using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Small helpers for building uGUI in code (no prefabs or scenes needed).</summary>
    public static class UIFactory
    {
        static Font _font;
        public static Font UIFont
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform NewRT(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void StretchWithInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Img(Transform parent, Color color)
        {
            RectTransform rt = NewRT("Image", parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Txt(Transform parent, string s, int size, Color color,
            TextAnchor align, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = NewRT("Text", parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = UIFont;
            t.text = s;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string label, Color bg, Color fg,
            Action onClick, int fontSize = 15, FontStyle style = FontStyle.Bold)
        {
            Image img = Img(parent, bg);
            img.raycastTarget = true;
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Text t = Txt(img.transform, label, fontSize, fg, TextAnchor.MiddleCenter, style);
            Stretch(t.rectTransform);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }
    }

    /// <summary>Builds the entire interface at runtime and re-renders it from GameManager state.</summary>
    public class GameUI : MonoBehaviour
    {
        public const float CardW = 74f, CardH = 96f, Gap = 14f;

        public RectTransform DragLayer { get; private set; }

        GameManager gm;
        RectTransform boardRoot;

        Text lvlBadge, scoreV, targetV, crystalV, deckV, movesV, progTxt, logText, hintText, ownedText, shopBal;
        RectTransform progressFill;
        GameObject nextBtnGO, modalRoot, shopRoot;
        Text modalTitle, modalBody, modalPrimaryText, modalSecondaryText;
        Button modalPrimary, modalSecondary;
        RectTransform offersRow;
        Button shopGo;
        Text shopGoText;

        public void Init(GameManager manager)
        {
            gm = manager;

            // canvas + event system
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
                // ENABLE_INPUT_SYSTEM is defined when the Input System package is active
                // (including "Both"); otherwise fall back to the legacy module.
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            Transform root = canvasGO.transform;

            // background + felt
            Image bg = UIFactory.Img(root, GameConfig.Bg);
            UIFactory.Stretch(bg.rectTransform);
            bg.raycastTarget = false;

            Image felt = UIFactory.Img(root, GameConfig.Felt);
            RectTransform feltRT = felt.rectTransform;
            feltRT.anchorMin = feltRT.anchorMax = new Vector2(0.5f, 0.5f);
            feltRT.sizeDelta = new Vector2(5 * CardW + 4 * Gap + 60, 5 * CardH + 4 * Gap + 50);
            feltRT.anchoredPosition = new Vector2(0, -14);
            felt.raycastTarget = false;

            // header
            lvlBadge = UIFactory.Txt(root, "LEVEL 1  ·  PYRAMID", 22, GameConfig.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform lb = lvlBadge.rectTransform;
            lb.anchorMin = lb.anchorMax = new Vector2(0, 1);
            lb.pivot = new Vector2(0, 1);
            lb.sizeDelta = new Vector2(420, 30);
            lb.anchoredPosition = new Vector2(20, -16);

            // stats row (top-right)
            string[] keys = { "SCORE", "TARGET", "CRYSTALS", "DECK", "MOVES" };
            Text[] vals = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                Image box = UIFactory.Img(root, GameConfig.PanelBg);
                RectTransform bx = box.rectTransform;
                bx.anchorMin = bx.anchorMax = new Vector2(1, 1);
                bx.pivot = new Vector2(1, 1);
                bx.sizeDelta = new Vector2(96, 52);
                bx.anchoredPosition = new Vector2(-14 - (4 - i) * 102, -12);
                box.raycastTarget = false;

                Text k = UIFactory.Txt(box.transform, keys[i], 10, GameConfig.InkDim, TextAnchor.UpperCenter);
                UIFactory.Stretch(k.rectTransform);
                k.rectTransform.offsetMax = new Vector2(0, -5);

                vals[i] = UIFactory.Txt(box.transform, "0", 22, GameConfig.Ink, TextAnchor.LowerCenter, FontStyle.Bold);
                UIFactory.Stretch(vals[i].rectTransform);
                vals[i].rectTransform.offsetMin = new Vector2(0, 4);
            }
            scoreV = vals[0]; scoreV.color = GameConfig.Gold;
            targetV = vals[1];
            crystalV = vals[2]; crystalV.color = GameConfig.GemBlue;
            deckV = vals[3];
            movesV = vals[4];

            // progress bar
            Image barBg = UIFactory.Img(root, new Color(0, 0, 0, 0.5f));
            RectTransform bb = barBg.rectTransform;
            bb.anchorMin = bb.anchorMax = new Vector2(0.5f, 1);
            bb.pivot = new Vector2(0.5f, 1);
            bb.sizeDelta = new Vector2(640, 10);
            bb.anchoredPosition = new Vector2(0, -78);
            barBg.raycastTarget = false;

            Image barFill = UIFactory.Img(barBg.transform, GameConfig.Gold);
            progressFill = barFill.rectTransform;
            progressFill.anchorMin = new Vector2(0, 0);
            progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
            barFill.raycastTarget = false;

            progTxt = UIFactory.Txt(root, "0 / 0", 12, GameConfig.InkDim, TextAnchor.UpperCenter);
            RectTransform pt = progTxt.rectTransform;
            pt.anchorMin = pt.anchorMax = new Vector2(0.5f, 1);
            pt.pivot = new Vector2(0.5f, 1);
            pt.sizeDelta = new Vector2(640, 18);
            pt.anchoredPosition = new Vector2(0, -92);

            // board container (manual card positioning)
            boardRoot = UIFactory.NewRT("Board", root);
            boardRoot.anchorMin = boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.sizeDelta = new Vector2(5 * CardW + 4 * Gap, 5 * CardH + 4 * Gap);
            boardRoot.anchoredPosition = new Vector2(0, -14);

            // cleared-this-turn log (left column)
            Text logTitle = UIFactory.Txt(root, "CLEARED THIS TURN", 11, GameConfig.InkDim, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform lt = logTitle.rectTransform;
            lt.anchorMin = lt.anchorMax = new Vector2(0, 0.5f);
            lt.pivot = new Vector2(0, 0.5f);
            lt.sizeDelta = new Vector2(280, 20);
            lt.anchoredPosition = new Vector2(20, 150);

            logText = UIFactory.Txt(root, "", 13, GameConfig.Ink, TextAnchor.UpperLeft);
            RectTransform lg = logText.rectTransform;
            lg.anchorMin = lg.anchorMax = new Vector2(0, 0.5f);
            lg.pivot = new Vector2(0, 1);
            lg.sizeDelta = new Vector2(280, 300);
            lg.anchoredPosition = new Vector2(20, 138);

            // buttons + hint (bottom)
            Button newBoard = UIFactory.Btn(root, "New board", GameConfig.PanelBg, GameConfig.InkDim, () => gm.Restart(), 14);
            RectTransform nb = (RectTransform)newBoard.transform;
            nb.anchorMin = nb.anchorMax = new Vector2(0.5f, 0);
            nb.pivot = new Vector2(1f, 0);
            nb.sizeDelta = new Vector2(130, 38);
            nb.anchoredPosition = new Vector2(-8, 58);

            Button next = UIFactory.Btn(root, "Next level >", GameConfig.Gold, new Color32(0x1a, 0x13, 0x0a, 255), () => gm.Advance(), 14);
            nextBtnGO = next.gameObject;
            RectTransform nx = (RectTransform)next.transform;
            nx.anchorMin = nx.anchorMax = new Vector2(0.5f, 0);
            nx.pivot = new Vector2(0f, 0);
            nx.sizeDelta = new Vector2(130, 38);
            nx.anchoredPosition = new Vector2(8, 58);
            nextBtnGO.SetActive(false);

            hintText = UIFactory.Txt(root,
                "Click a card to flip (colour <-> number) · drag one onto another to swap · matches clear and refill — chain them",
                12, GameConfig.InkDim, TextAnchor.MiddleCenter);
            RectTransform ht = hintText.rectTransform;
            ht.anchorMin = ht.anchorMax = new Vector2(0.5f, 0);
            ht.pivot = new Vector2(0.5f, 0);
            ht.sizeDelta = new Vector2(900, 24);
            ht.anchoredPosition = new Vector2(0, 26);

            BuildModal(root);
            BuildShop(root);

            // drag layer must be topmost so dragged cards render above everything
            DragLayer = UIFactory.NewRT("DragLayer", root);
            UIFactory.Stretch(DragLayer);
            DragLayer.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        // ===== render =====

        public void Render(RenderOpts opts)
        {
            var clearing = (opts != null && opts.clearing != null) ? opts.clearing : new HashSet<(int, int)>();
            var entering = (opts != null && opts.entering != null) ? opts.entering : new HashSet<(int, int)>();
            CellFlags[][] flags = opts != null ? opts.flags : null;

            // HUD
            lvlBadge.text = "LEVEL " + (gm.level + 1) + "  ·  PYRAMID";
            scoreV.text = gm.score.ToString();
            targetV.text = gm.target.ToString();
            crystalV.text = "\u25C6 " + gm.crystals;
            deckV.text = gm.pile.Count.ToString();
            movesV.text = gm.moves.ToString();
            movesV.color = gm.moves <= 1 ? GameConfig.Danger : GameConfig.Ink;

            float pct = Mathf.Clamp01(gm.target > 0 ? (float)gm.score / gm.target : 0f);
            progressFill.anchorMax = new Vector2(pct, 1);
            progTxt.text = gm.score + " / " + gm.target + (gm.score >= gm.target ? "   target reached" : "");

            // board
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
                Destroy(boardRoot.GetChild(i).gameObject);

            float top = (5 * CardH + 4 * Gap) / 2f - CardH / 2f;
            for (int r = 0; r < gm.grid.Length; r++)
            {
                int len = gm.grid[r].Length;
                float y = top - r * (CardH + Gap);
                for (int c = 0; c < len; c++)
                {
                    float x = (c - (len - 1) / 2f) * (CardW + Gap);
                    var pos = new Vector2(x, y);
                    CardData card = gm.grid[r][c];
                    if (card == null)
                    {
                        CardView.CreateHole(boardRoot, pos);
                    }
                    else
                    {
                        CardView.Create(this, gm, boardRoot, pos, card,
                            flags != null ? flags[r][c] : null, r, c,
                            clearing.Contains((r, c)), entering.Contains((r, c)));
                    }
                }
            }

            // log
            if (gm.lastCleared.Count == 0)
            {
                logText.text = "Make a match to clear cards —\nthe deck refills the gaps.";
                logText.color = GameConfig.InkDim;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (Combo e in gm.lastCleared)
                {
                    string reward = e.moveBonus > 0 ? "+" + e.moveBonus + " moves" : "+" + e.points;
                    string chainTag = (e.mult > 1 && e.moveBonus == 0) ? "  x" + e.mult : "";
                    string gemTag = e.gem ? "  \u25C6" : "";
                    sb.AppendLine("\u2022 " + e.label + "   " + reward + chainTag + gemTag);
                }
                logText.text = sb.ToString();
                logText.color = GameConfig.Ink;
            }
        }

        public void SetNextVisible(bool visible)
        {
            nextBtnGO.SetActive(visible);
        }

        public void FlashMoves()
        {
            movesV.color = GameConfig.Danger;
        }

        // ===== modal =====

        void BuildModal(Transform root)
        {
            Image dim = UIFactory.Img(root, new Color(0, 0, 0, 0.75f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            modalRoot = dim.gameObject;

            Image panel = UIFactory.Img(dim.transform, GameConfig.PanelBg);
            RectTransform pr = panel.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(440, 230);

            modalTitle = UIFactory.Txt(panel.transform, "", 24, GameConfig.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Stretch(modalTitle.rectTransform);
            modalTitle.rectTransform.offsetMax = new Vector2(0, -18);

            modalBody = UIFactory.Txt(panel.transform, "", 14, GameConfig.InkDim, TextAnchor.UpperCenter);
            UIFactory.Stretch(modalBody.rectTransform);
            modalBody.rectTransform.offsetMin = new Vector2(24, 0);
            modalBody.rectTransform.offsetMax = new Vector2(-24, -58);

            modalPrimary = UIFactory.Btn(panel.transform, "", GameConfig.Gold, new Color32(0x1a, 0x13, 0x0a, 255), null, 15);
            RectTransform mp = (RectTransform)modalPrimary.transform;
            mp.anchorMin = mp.anchorMax = new Vector2(0.5f, 0);
            mp.pivot = new Vector2(0.5f, 0);
            mp.sizeDelta = new Vector2(180, 40);
            mp.anchoredPosition = new Vector2(0, 56);
            modalPrimaryText = modalPrimary.GetComponentInChildren<Text>();

            modalSecondary = UIFactory.Btn(panel.transform, "", new Color(0, 0, 0, 0), GameConfig.InkDim, null, 12, FontStyle.Normal);
            RectTransform ms = (RectTransform)modalSecondary.transform;
            ms.anchorMin = ms.anchorMax = new Vector2(0.5f, 0);
            ms.pivot = new Vector2(0.5f, 0);
            ms.sizeDelta = new Vector2(180, 26);
            ms.anchoredPosition = new Vector2(0, 24);
            modalSecondaryText = modalSecondary.GetComponentInChildren<Text>();

            modalRoot.SetActive(false);
        }

        public void ShowModal(bool win, string title, string body,
            string primaryLabel, Action primary, string secondaryLabel, Action secondary)
        {
            modalTitle.text = title;
            modalTitle.color = win ? GameConfig.Gold : GameConfig.Danger;
            modalBody.text = body;

            modalPrimaryText.text = primaryLabel;
            modalPrimary.onClick.RemoveAllListeners();
            modalPrimary.onClick.AddListener(() => primary());

            bool hasSecondary = !string.IsNullOrEmpty(secondaryLabel);
            modalSecondary.gameObject.SetActive(hasSecondary);
            if (hasSecondary)
            {
                modalSecondaryText.text = secondaryLabel;
                modalSecondary.onClick.RemoveAllListeners();
                modalSecondary.onClick.AddListener(() => secondary());
            }
            modalRoot.SetActive(true);
        }

        public void HideModal()
        {
            modalRoot.SetActive(false);
        }

        // ===== the Workshop =====

        void BuildShop(Transform root)
        {
            Image dim = UIFactory.Img(root, new Color(0, 0, 0, 0.8f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            shopRoot = dim.gameObject;

            Image panel = UIFactory.Img(dim.transform, GameConfig.PanelBg);
            RectTransform pr = panel.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(580, 430);

            Text title = UIFactory.Txt(panel.transform, "THE WORKSHOP", 22, GameConfig.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(24, 0);
            title.rectTransform.offsetMax = new Vector2(0, -18);

            shopBal = UIFactory.Txt(panel.transform, "\u25C6 0", 20, GameConfig.GemBlue, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Stretch(shopBal.rectTransform);
            shopBal.rectTransform.offsetMax = new Vector2(-24, -18);

            Text sub = UIFactory.Txt(panel.transform,
                "Spend crystals to gild a number — every card of that number scores more, for the rest of the run.",
                13, GameConfig.InkDim, TextAnchor.UpperLeft);
            UIFactory.Stretch(sub.rectTransform);
            sub.rectTransform.offsetMin = new Vector2(24, 0);
            sub.rectTransform.offsetMax = new Vector2(-24, -52);

            offersRow = UIFactory.NewRT("Offers", panel.transform);
            offersRow.anchorMin = offersRow.anchorMax = new Vector2(0.5f, 0.5f);
            offersRow.sizeDelta = new Vector2(540, 170);
            offersRow.anchoredPosition = new Vector2(0, 18);

            ownedText = UIFactory.Txt(panel.transform, "", 12, GameConfig.Gold, TextAnchor.MiddleCenter);
            RectTransform ot = ownedText.rectTransform;
            ot.anchorMin = ot.anchorMax = new Vector2(0.5f, 0);
            ot.pivot = new Vector2(0.5f, 0);
            ot.sizeDelta = new Vector2(540, 22);
            ot.anchoredPosition = new Vector2(0, 74);

            shopGo = UIFactory.Btn(panel.transform, "Begin level >", GameConfig.Gold, new Color32(0x1a, 0x13, 0x0a, 255), () => gm.BeginPending(), 15);
            RectTransform sg = (RectTransform)shopGo.transform;
            sg.anchorMin = sg.anchorMax = new Vector2(0.5f, 0);
            sg.pivot = new Vector2(0.5f, 0);
            sg.sizeDelta = new Vector2(520, 44);
            sg.anchoredPosition = new Vector2(0, 20);
            shopGoText = shopGo.GetComponentInChildren<Text>();

            shopRoot.SetActive(false);
        }

        public void ShowShop()
        {
            shopGoText.text = "Begin level " + (gm.pendingLevel + 1) + " >";
            RefreshShop();
            shopRoot.SetActive(true);
        }

        public void HideShop()
        {
            shopRoot.SetActive(false);
        }

        void RefreshShop()
        {
            shopBal.text = "\u25C6 " + gm.crystals;

            for (int i = offersRow.childCount - 1; i >= 0; i--)
                Destroy(offersRow.GetChild(i).gameObject);

            int count = gm.offers.Count;
            for (int i = 0; i < count; i++)
            {
                ShopOffer o = gm.offers[i];
                int index = i;

                Image box = UIFactory.Img(offersRow, GameConfig.Bg);
                RectTransform bx = box.rectTransform;
                bx.anchorMin = bx.anchorMax = new Vector2(0.5f, 0.5f);
                bx.sizeDelta = new Vector2(168, 168);
                bx.anchoredPosition = new Vector2((i - (count - 1) / 2f) * 182f, 0);

                Text big = UIFactory.Txt(box.transform, o.num.ToString(), 40, GameConfig.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
                UIFactory.Stretch(big.rectTransform);
                big.rectTransform.offsetMax = new Vector2(0, -14);

                Text eff = UIFactory.Txt(box.transform,
                    "every " + o.num + " scores x" + o.mult, 12, GameConfig.InkDim, TextAnchor.MiddleCenter);
                UIFactory.Stretch(eff.rectTransform);
                eff.rectTransform.offsetMin = new Vector2(4, 30);
                eff.rectTransform.offsetMax = new Vector2(-4, -62);

                if (o.bought)
                {
                    Text done = UIFactory.Txt(box.transform, "\u2713 gilded", 13,
                        new Color32(0x3a, 0xa7, 0x65, 255), TextAnchor.LowerCenter, FontStyle.Bold);
                    UIFactory.Stretch(done.rectTransform);
                    done.rectTransform.offsetMin = new Vector2(0, 18);
                }
                else
                {
                    bool afford = gm.crystals >= o.cost;
                    Button buy = UIFactory.Btn(box.transform, o.cost + " \u25C6",
                        afford ? GameConfig.Gold : new Color32(0x2a, 0x31, 0x38, 255),
                        afford ? (Color)new Color32(0x1a, 0x13, 0x0a, 255) : (Color)GameConfig.InkDim,
                        () => { if (gm.TryBuy(index)) RefreshShop(); }, 13);
                    buy.interactable = afford;
                    RectTransform br = (RectTransform)buy.transform;
                    br.anchorMin = br.anchorMax = new Vector2(0.5f, 0);
                    br.pivot = new Vector2(0.5f, 0);
                    br.sizeDelta = new Vector2(90, 30);
                    br.anchoredPosition = new Vector2(0, 12);
                }
            }

            // active modifications summary
            var owned = new List<int>(gm.mods.Keys);
            owned.Sort();
            if (owned.Count == 0)
            {
                ownedText.text = "";
            }
            else
            {
                var sb = new StringBuilder("Active: ");
                for (int i = 0; i < owned.Count; i++)
                {
                    if (i > 0) sb.Append("   ");
                    sb.Append(owned[i]).Append("s x").Append(gm.mods[owned[i]]);
                }
                ownedText.text = sb.ToString();
            }
        }
    }
}
