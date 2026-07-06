using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Everything a card needs to build and behave, passed as one object so the signature stays small
    /// and new dependencies (art, VFX, sizing) don't ripple through call sites.</summary>
    public class CardBuildContext
    {
        public GameManager gm;
        public ThemePalette palette;
        public CardVisualTheme theme;
        public RectTransform dragLayer;
        public Action<GameVfx, Vector3> vfx;   // spawn a screen-space effect; may be null
    }

    /// <summary>Visual card. Click (no drag) = flip; drag onto another card = swap. Views are rebuilt each
    /// render, so state lives in <see cref="GameManager"/>, never here. Reads all colour/sprite/timing from
    /// config via <see cref="CardBuildContext"/>.</summary>
    public class CardView : MonoBehaviour,
        IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int r, c;

        CardBuildContext ctx;
        CanvasGroup cg;
        bool dragged;
        GameVfx? clearVfx;

        Transform homeParent;
        int homeIndex;
        Vector2 homePos;

        public static CardView Create(CardBuildContext ctx, RectTransform parent, Vector2 pos,
            CardData card, CellFlags flags, int r, int c, bool clearing, bool entering)
        {
            ThemePalette pal = ctx.palette;
            CardVisualTheme theme = ctx.theme;

            RectTransform rt = UIFactory.NewRT("Card", parent);
            rt.sizeDelta = new Vector2(theme.cardWidth, theme.cardHeight);
            rt.anchoredPosition = pos;

            Image root = rt.gameObject.AddComponent<Image>();
            root.color = pal.SuitColor(card.suit);
            if (theme.cardRim != null) { root.sprite = theme.cardRim; root.type = Image.Type.Sliced; }

            if (card.up)
            {
                // cream panel inset in the suit-colour rim
                Image panel = UIFactory.Img(rt, pal.cream, theme.facePanel);
                UIFactory.StretchWithInset(panel.rectTransform, theme.facePanelInset);

                // optional suit icon washed behind the number
                Sprite icon = theme.Icon(card.suit);
                if (icon != null)
                {
                    Image ic = UIFactory.Img(panel.transform, new Color(1f, 1f, 1f, 0.22f), icon);
                    UIFactory.StretchWithInset(ic.rectTransform, 10f);
                    ic.preserveAspect = true;
                }

                Text num = UIFactory.Txt(panel.transform, card.num.ToString(), 30,
                    pal.SuitDeep(card.suit), TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(num.rectTransform);

                Text tl = UIFactory.Txt(panel.transform, card.num.ToString(), 11,
                    pal.SuitColor(card.suit), TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Stretch(tl.rectTransform);
                tl.rectTransform.offsetMin = new Vector2(4, 0);
                tl.rectTransform.offsetMax = new Vector2(0, -2);

                Text br = UIFactory.Txt(panel.transform, card.num.ToString(), 11,
                    pal.SuitColor(card.suit), TextAnchor.LowerRight, FontStyle.Bold);
                UIFactory.Stretch(br.rectTransform);
                br.rectTransform.offsetMin = new Vector2(0, 2);
                br.rectTransform.offsetMax = new Vector2(-4, 0);

                // gild tag: shows the bought score multiplier right on the card
                int mod = ctx.gm.GetMod(card.num);
                if (mod > 1)
                {
                    Image tag = UIFactory.Img(rt, pal.gold, theme.gildTagBackground);
                    RectTransform tr = tag.rectTransform;
                    tr.anchorMin = tr.anchorMax = new Vector2(1, 1);
                    tr.pivot = new Vector2(1, 1);
                    tr.sizeDelta = new Vector2(28, 16);
                    tr.anchoredPosition = new Vector2(-2, -2);
                    Text tt = UIFactory.Txt(tag.transform, "x" + mod, 11,
                        pal.darkInk, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIFactory.Stretch(tt.rectTransform);
                }
            }
            else
            {
                // face-down: solid colour with a soft pip (or the themed pip sprite)
                Image pip = UIFactory.Img(rt, pal.SuitLight(card.suit), theme.Pip(card.suit));
                RectTransform pr = pip.rectTransform;
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.sizeDelta = new Vector2(26, 26);
                pr.anchoredPosition = Vector2.zero;
                pip.preserveAspect = true;
            }

            // combo glow (priority: number triad > colour triad > number run > colour run)
            if (flags != null)
            {
                Color? glow = null;
                if (flags.triad) glow = pal.triadGlow;
                else if (flags.triadColor) glow = Color.white;
                else if (flags.number) glow = pal.gold;
                else if (flags.color) glow = pal.SuitLight(card.suit);
                if (glow.HasValue)
                {
                    Outline o = rt.gameObject.AddComponent<Outline>();
                    o.effectColor = glow.Value;
                    o.effectDistance = new Vector2(4, 4);
                }
            }

            CardView view = rt.gameObject.AddComponent<CardView>();
            view.ctx = ctx; view.r = r; view.c = c;
            view.cg = rt.gameObject.AddComponent<CanvasGroup>();
            view.clearVfx = clearing ? PickVfx(flags) : (GameVfx?)null;

            if (clearing) view.StartCoroutine(view.AnimClear());
            else if (entering) view.StartCoroutine(view.AnimPop());

            return view;
        }

        public static void CreateHole(CardVisualTheme theme, RectTransform parent, Vector2 pos)
        {
            RectTransform rt = UIFactory.NewRT("Hole", parent);
            rt.sizeDelta = new Vector2(theme.cardWidth, theme.cardHeight);
            rt.anchoredPosition = pos;
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.22f);
            if (theme.holeSprite != null) { img.sprite = theme.holeSprite; img.type = Image.Type.Sliced; }
            img.raycastTarget = false;
        }

        static GameVfx PickVfx(CellFlags f)
        {
            if (f == null) return GameVfx.CardCleared;
            if (f.triad) return GameVfx.NumberTriad;
            if (f.triadColor) return GameVfx.ColorTriad;
            if (f.number) return GameVfx.NumberRun;
            if (f.color) return GameVfx.ColorRun;
            return GameVfx.CardCleared;
        }

        // ===== input =====

        public void OnPointerDown(PointerEventData e) { dragged = false; }

        public void OnPointerClick(PointerEventData e)
        {
            if (dragged) return;
            ctx.gm.Flip(r, c);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (ctx.gm.busy || ctx.gm.moves <= 0) return;
            dragged = true;
            homeParent = transform.parent;
            homeIndex = transform.GetSiblingIndex();
            homePos = ((RectTransform)transform).anchoredPosition;
            transform.SetParent(ctx.dragLayer, true);
            cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragged) return;
            transform.position = e.position;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragged) return;
            cg.blocksRaycasts = true;

            CardView targetView = null;
            if (e.pointerEnter != null)
                targetView = e.pointerEnter.GetComponentInParent<CardView>();

            if (targetView != null && targetView != this)
            {
                ctx.gm.Swap(r, c, targetView.r, targetView.c);   // triggers a full re-render
            }
            else
            {
                transform.SetParent(homeParent, false);
                transform.SetSiblingIndex(homeIndex);
                ((RectTransform)transform).anchoredPosition = homePos;
            }
        }

        // ===== animation =====

        IEnumerator AnimPop()
        {
            float d = 0.3f, t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.SmoothStep(0.55f, 1f, t / d);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        IEnumerator AnimClear()
        {
            if (clearVfx.HasValue && ctx.vfx != null)
                ctx.vfx(clearVfx.Value, transform.position);

            float d = ctx.gm.ClearSeconds, t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = t / d;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
                cg.alpha = 1f - k;
                yield return null;
            }
        }
    }
}
