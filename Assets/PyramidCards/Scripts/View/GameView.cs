using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Builds the interface at runtime and re-renders it from <see cref="GameManager"/> state.
    /// It never calls into the rules — it subscribes to the manager's events and only reads public state,
    /// so logic and presentation are fully decoupled.</summary>
    public class GameView : MonoBehaviour
    {
        GameManager gm;
        ThemePalette pal;
        CardVisualTheme theme;

        float cardW, cardH, gap;

        public RectTransform DragLayer { get; private set; }
        public RectTransform VfxLayer { get; private set; }

        RectTransform boardRoot;
        Text lvlBadge, scoreV, targetV, crystalV, deckV, movesV, progTxt, logText;
        RectTransform progressFill;
        GameObject nextBtnGO;

        ModalView modal;
        ShopView shop;
        CardBuildContext cardCtx;

        public void Init(GameManager manager, ThemePalette palette, CardVisualTheme cardTheme, VfxService vfxService)
        {
            gm = manager;
            pal = palette;
            theme = cardTheme;
            cardW = theme.cardWidth; cardH = theme.cardHeight; gap = theme.gap;

            UIFactory.FontOverride = theme.font;   // null => built-in font

            Transform root = BuildCanvas();
            BuildBackground(root);
            BuildHeader(root);
            BuildStats(root);
            BuildProgress(root);
            BuildBoard(root);
            BuildLog(root);
            BuildButtons(root);

            modal = new ModalView(root, pal);
            shop = new ShopView(root, gm, pal);

            // VFX renders above the board but below dialogs; drag layer stays on top of everything.
            VfxLayer = UIFactory.NewRT("VfxLayer", root);
            UIFactory.Stretch(VfxLayer);
            VfxLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            DragLayer = UIFactory.NewRT("DragLayer", root);
            UIFactory.Stretch(DragLayer);
            DragLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // The bootstrap binds the VfxService to its library and this VfxLayer after Init.
            Action<GameVfx, Vector3> vfxHook = vfxService != null
                ? (Action<GameVfx, Vector3>)((v, pos) => vfxService.Play(v, pos))
                : null;

            cardCtx = new CardBuildContext
            {
                gm = gm, palette = pal, theme = theme,
                dragLayer = DragLayer, vfx = vfxHook
            };

            Subscribe();
        }

        // ===== event wiring =====

        void Subscribe()
        {
            gm.RenderRequested += OnRender;
            gm.MoveBlocked += FlashMoves;
            gm.NextAvailabilityChanged += SetNextVisible;
            gm.ModalRequested += modal.Show;
            gm.ModalDismissed += modal.Hide;
            gm.ShopOpened += OnShopOpened;
            gm.ShopChanged += shop.Refresh;
            gm.ShopClosed += shop.Hide;
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.RenderRequested -= OnRender;
            gm.MoveBlocked -= FlashMoves;
            gm.NextAvailabilityChanged -= SetNextVisible;
            gm.ModalRequested -= modal.Show;
            gm.ModalDismissed -= modal.Hide;
            gm.ShopOpened -= OnShopOpened;
            gm.ShopChanged -= shop.Refresh;
            gm.ShopClosed -= shop.Hide;
        }

        void OnShopOpened() { shop.Show(); }

        // ===== canvas & static chrome =====

        Transform BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform, false);
                // Use the module that matches the project's active input backend.
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            return canvasGO.transform;
        }

        void BuildBackground(Transform root)
        {
            Image bg = UIFactory.Img(root, pal.bg);
            UIFactory.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            Image felt = UIFactory.Img(root, pal.felt);
            RectTransform feltRT = felt.rectTransform;
            feltRT.anchorMin = feltRT.anchorMax = new Vector2(0.5f, 0.5f);
            feltRT.sizeDelta = new Vector2(5 * cardW + 4 * gap + 60, 5 * cardH + 4 * gap + 50);
            feltRT.anchoredPosition = new Vector2(0, -14);
            felt.raycastTarget = false;
        }

        void BuildHeader(Transform root)
        {
            lvlBadge = UIFactory.Txt(root, "LEVEL 1  ·  PYRAMID", 22, pal.gold, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform lb = lvlBadge.rectTransform;
            lb.anchorMin = lb.anchorMax = new Vector2(0, 1);
            lb.pivot = new Vector2(0, 1);
            lb.sizeDelta = new Vector2(420, 30);
            lb.anchoredPosition = new Vector2(20, -16);
        }

        void BuildStats(Transform root)
        {
            string[] keys = { "SCORE", "TARGET", "CRYSTALS", "DECK", "MOVES" };
            Text[] vals = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                Image box = UIFactory.Img(root, pal.panelBg);
                RectTransform bx = box.rectTransform;
                bx.anchorMin = bx.anchorMax = new Vector2(1, 1);
                bx.pivot = new Vector2(1, 1);
                bx.sizeDelta = new Vector2(96, 52);
                bx.anchoredPosition = new Vector2(-14 - (4 - i) * 102, -12);
                box.raycastTarget = false;

                Text k = UIFactory.Txt(box.transform, keys[i], 10, pal.inkDim, TextAnchor.UpperCenter);
                UIFactory.Stretch(k.rectTransform);
                k.rectTransform.offsetMax = new Vector2(0, -5);

                vals[i] = UIFactory.Txt(box.transform, "0", 22, pal.ink, TextAnchor.LowerCenter, FontStyle.Bold);
                UIFactory.Stretch(vals[i].rectTransform);
                vals[i].rectTransform.offsetMin = new Vector2(0, 4);
            }
            scoreV = vals[0]; scoreV.color = pal.gold;
            targetV = vals[1];
            crystalV = vals[2]; crystalV.color = pal.gemBlue;
            deckV = vals[3];
            movesV = vals[4];
        }

        void BuildProgress(Transform root)
        {
            Image barBg = UIFactory.Img(root, new Color(0, 0, 0, 0.5f));
            RectTransform bb = barBg.rectTransform;
            bb.anchorMin = bb.anchorMax = new Vector2(0.5f, 1);
            bb.pivot = new Vector2(0.5f, 1);
            bb.sizeDelta = new Vector2(640, 10);
            bb.anchoredPosition = new Vector2(0, -78);
            barBg.raycastTarget = false;

            Image barFill = UIFactory.Img(barBg.transform, pal.gold);
            progressFill = barFill.rectTransform;
            progressFill.anchorMin = new Vector2(0, 0);
            progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
            barFill.raycastTarget = false;

            progTxt = UIFactory.Txt(root, "0 / 0", 12, pal.inkDim, TextAnchor.UpperCenter);
            RectTransform pt = progTxt.rectTransform;
            pt.anchorMin = pt.anchorMax = new Vector2(0.5f, 1);
            pt.pivot = new Vector2(0.5f, 1);
            pt.sizeDelta = new Vector2(640, 18);
            pt.anchoredPosition = new Vector2(0, -92);
        }

        void BuildBoard(Transform root)
        {
            boardRoot = UIFactory.NewRT("Board", root);
            boardRoot.anchorMin = boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.sizeDelta = new Vector2(5 * cardW + 4 * gap, 5 * cardH + 4 * gap);
            boardRoot.anchoredPosition = new Vector2(0, -14);
        }

        void BuildLog(Transform root)
        {
            Text logTitle = UIFactory.Txt(root, "CLEARED THIS TURN", 11, pal.inkDim, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform lt = logTitle.rectTransform;
            lt.anchorMin = lt.anchorMax = new Vector2(0, 0.5f);
            lt.pivot = new Vector2(0, 0.5f);
            lt.sizeDelta = new Vector2(280, 20);
            lt.anchoredPosition = new Vector2(20, 150);

            logText = UIFactory.Txt(root, "", 13, pal.ink, TextAnchor.UpperLeft);
            RectTransform lg = logText.rectTransform;
            lg.anchorMin = lg.anchorMax = new Vector2(0, 0.5f);
            lg.pivot = new Vector2(0, 1);
            lg.sizeDelta = new Vector2(280, 300);
            lg.anchoredPosition = new Vector2(20, 138);
        }

        void BuildButtons(Transform root)
        {
            Button newBoard = UIFactory.Btn(root, "New board", pal.panelBg, pal.inkDim, () => gm.Restart(), 14);
            RectTransform nb = (RectTransform)newBoard.transform;
            nb.anchorMin = nb.anchorMax = new Vector2(0.5f, 0);
            nb.pivot = new Vector2(1f, 0);
            nb.sizeDelta = new Vector2(130, 38);
            nb.anchoredPosition = new Vector2(-8, 58);

            Button next = UIFactory.Btn(root, "Next level >", pal.gold, pal.darkInk, () => gm.Advance(), 14);
            nextBtnGO = next.gameObject;
            RectTransform nx = (RectTransform)next.transform;
            nx.anchorMin = nx.anchorMax = new Vector2(0.5f, 0);
            nx.pivot = new Vector2(0f, 0);
            nx.sizeDelta = new Vector2(130, 38);
            nx.anchoredPosition = new Vector2(8, 58);
            nextBtnGO.SetActive(false);

            Text hint = UIFactory.Txt(root,
                "Click a card to flip (colour <-> number) · drag one onto another to swap · matches clear and refill — chain them",
                12, pal.inkDim, TextAnchor.MiddleCenter);
            RectTransform ht = hint.rectTransform;
            ht.anchorMin = ht.anchorMax = new Vector2(0.5f, 0);
            ht.pivot = new Vector2(0.5f, 0);
            ht.sizeDelta = new Vector2(900, 24);
            ht.anchoredPosition = new Vector2(0, 26);
        }

        // ===== render =====

        void OnRender(RenderOpts opts)
        {
            RenderHud();
            RenderBoard(opts);
            RenderLog();
        }

        void RenderHud()
        {
            lvlBadge.text = "LEVEL " + (gm.level + 1) + "  ·  PYRAMID";
            scoreV.text = gm.score.ToString();
            targetV.text = gm.target.ToString();
            crystalV.text = "◆ " + gm.crystals;
            deckV.text = gm.pile.Count.ToString();
            movesV.text = gm.moves.ToString();
            movesV.color = gm.moves <= 1 ? pal.danger : pal.ink;

            float pct = Mathf.Clamp01(gm.target > 0 ? (float)gm.score / gm.target : 0f);
            progressFill.anchorMax = new Vector2(pct, 1);
            progTxt.text = gm.score + " / " + gm.target + (gm.score >= gm.target ? "   target reached" : "");
        }

        void RenderBoard(RenderOpts opts)
        {
            var clearing = (opts != null && opts.clearing != null) ? opts.clearing : null;
            var entering = (opts != null && opts.entering != null) ? opts.entering : null;
            CellFlags[][] flags = opts != null ? opts.flags : null;

            for (int i = boardRoot.childCount - 1; i >= 0; i--)
                Destroy(boardRoot.GetChild(i).gameObject);

            float top = (5 * cardH + 4 * gap) / 2f - cardH / 2f;
            for (int r = 0; r < gm.grid.Length; r++)
            {
                int len = gm.grid[r].Length;
                float y = top - r * (cardH + gap);
                for (int c = 0; c < len; c++)
                {
                    float x = (c - (len - 1) / 2f) * (cardW + gap);
                    var pos = new Vector2(x, y);
                    CardData card = gm.grid[r][c];
                    if (card == null)
                    {
                        CardView.CreateHole(theme, boardRoot, pos);
                    }
                    else
                    {
                        CardView.Create(cardCtx, boardRoot, pos, card,
                            flags != null ? flags[r][c] : null, r, c,
                            clearing != null && clearing.Contains((r, c)),
                            entering != null && entering.Contains((r, c)));
                    }
                }
            }
        }

        void RenderLog()
        {
            if (gm.lastCleared.Count == 0)
            {
                logText.text = "Make a match to clear cards —\nthe deck refills the gaps.";
                logText.color = pal.inkDim;
                return;
            }

            var sb = new StringBuilder();
            foreach (Combo e in gm.lastCleared)
            {
                string reward = e.moveBonus > 0 ? "+" + e.moveBonus + " moves" : "+" + e.points;
                string chainTag = (e.mult > 1 && e.moveBonus == 0) ? "  x" + e.mult : "";
                string gemTag = e.gem ? "  ◆" : "";
                sb.AppendLine("• " + e.label + "   " + reward + chainTag + gemTag);
            }
            logText.text = sb.ToString();
            logText.color = pal.ink;
        }

        void SetNextVisible(bool visible) { nextBtnGO.SetActive(visible); }

        void FlashMoves() { movesV.color = pal.danger; }
    }
}
