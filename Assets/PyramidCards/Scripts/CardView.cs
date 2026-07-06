using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Visual card. Click (no drag) = flip; drag onto another card = swap.
    /// Views are rebuilt each render, so state lives in GameManager, never here.</summary>
    public class CardView : MonoBehaviour,
        IPointerDownHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int r, c;

        GameManager gm;
        GameUI ui;
        CanvasGroup cg;
        bool dragged;

        Transform homeParent;
        int homeIndex;
        Vector2 homePos;

        public static CardView Create(GameUI ui, GameManager gm, RectTransform parent, Vector2 pos,
            CardData card, CellFlags flags, int r, int c, bool clearing, bool entering)
        {
            RectTransform rt = UIFactory.NewRT("Card", parent);
            rt.sizeDelta = new Vector2(GameUI.CardW, GameUI.CardH);
            rt.anchoredPosition = pos;

            Image root = rt.gameObject.AddComponent<Image>();
            root.color = card.up ? GameConfig.SuitColors[card.suit] : GameConfig.SuitColors[card.suit];

            if (card.up)
            {
                // cream panel inset in the suit-colour rim
                Image panel = UIFactory.Img(rt, GameConfig.Cream);
                UIFactory.StretchWithInset(panel.rectTransform, 6f);

                Text num = UIFactory.Txt(panel.transform, card.num.ToString(), 30,
                    GameConfig.SuitDeep[card.suit], TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(num.rectTransform);

                Text tl = UIFactory.Txt(panel.transform, card.num.ToString(), 11,
                    GameConfig.SuitColors[card.suit], TextAnchor.UpperLeft, FontStyle.Bold);
                UIFactory.Stretch(tl.rectTransform);
                tl.rectTransform.offsetMin = new Vector2(4, 0);
                tl.rectTransform.offsetMax = new Vector2(0, -2);

                Text br = UIFactory.Txt(panel.transform, card.num.ToString(), 11,
                    GameConfig.SuitColors[card.suit], TextAnchor.LowerRight, FontStyle.Bold);
                UIFactory.Stretch(br.rectTransform);
                br.rectTransform.offsetMin = new Vector2(0, 2);
                br.rectTransform.offsetMax = new Vector2(-4, 0);

                // gild tag: shows the bought score multiplier right on the card
                int mod = gm.GetMod(card.num);
                if (mod > 1)
                {
                    Image tag = UIFactory.Img(rt, GameConfig.Gold);
                    RectTransform tr = tag.rectTransform;
                    tr.anchorMin = tr.anchorMax = new Vector2(1, 1);
                    tr.pivot = new Vector2(1, 1);
                    tr.sizeDelta = new Vector2(28, 16);
                    tr.anchoredPosition = new Vector2(-2, -2);
                    Text tt = UIFactory.Txt(tag.transform, "x" + mod, 11,
                        new Color32(0x1a, 0x13, 0x0a, 255), TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIFactory.Stretch(tt.rectTransform);
                }
            }
            else
            {
                // face-down: solid colour with a soft pip
                Image pip = UIFactory.Img(rt, GameConfig.SuitLight[card.suit]);
                RectTransform pr = pip.rectTransform;
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.sizeDelta = new Vector2(26, 26);
                pr.anchoredPosition = Vector2.zero;
            }

            // combo glow (priority: number triad > colour triad > number run > colour run)
            if (flags != null)
            {
                Color? glow = null;
                if (flags.triad) glow = GameConfig.TriadGlow;
                else if (flags.triadColor) glow = Color.white;
                else if (flags.number) glow = GameConfig.Gold;
                else if (flags.color) glow = GameConfig.SuitLight[card.suit];
                if (glow.HasValue)
                {
                    Outline o = rt.gameObject.AddComponent<Outline>();
                    o.effectColor = glow.Value;
                    o.effectDistance = new Vector2(4, 4);
                }
            }

            CardView view = rt.gameObject.AddComponent<CardView>();
            view.gm = gm; view.ui = ui; view.r = r; view.c = c;
            view.cg = rt.gameObject.AddComponent<CanvasGroup>();

            if (clearing) view.StartCoroutine(view.AnimClear());
            else if (entering) view.StartCoroutine(view.AnimPop());

            return view;
        }

        public static void CreateHole(RectTransform parent, Vector2 pos)
        {
            RectTransform rt = UIFactory.NewRT("Hole", parent);
            rt.sizeDelta = new Vector2(GameUI.CardW, GameUI.CardH);
            rt.anchoredPosition = pos;
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.22f);
            img.raycastTarget = false;
        }

        // ===== input =====

        public void OnPointerDown(PointerEventData e) { dragged = false; }

        public void OnPointerClick(PointerEventData e)
        {
            if (dragged) return;
            gm.Flip(r, c);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (gm.busy || gm.moves <= 0) return;
            dragged = true;
            homeParent = transform.parent;
            homeIndex = transform.GetSiblingIndex();
            homePos = ((RectTransform)transform).anchoredPosition;
            transform.SetParent(ui.DragLayer, true);
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
                gm.Swap(r, c, targetView.r, targetView.c);   // triggers a full re-render
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
            float d = GameConfig.ClearSeconds, t = 0f;
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
