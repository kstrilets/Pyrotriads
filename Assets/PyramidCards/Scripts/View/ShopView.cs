using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>The between-level Workshop screen. Reads offers/crystals/mods from <see cref="GameManager"/>
    /// and drives purchases through it; owns none of that state.</summary>
    public class ShopView
    {
        readonly GameManager gm;
        readonly ThemePalette pal;

        readonly GameObject root;
        readonly Text balance, ownedText, goText;
        readonly RectTransform offersRow;

        public ShopView(Transform parent, GameManager gameManager, ThemePalette palette)
        {
            gm = gameManager;
            pal = palette;

            Image dim = UIFactory.Img(parent, new Color(0, 0, 0, 0.8f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            root = dim.gameObject;

            Image panel = UIFactory.Img(dim.transform, pal.panelBg);
            RectTransform pr = panel.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(580, 430);

            Text title = UIFactory.Txt(panel.transform, "THE WORKSHOP", 22, pal.ink, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(24, 0);
            title.rectTransform.offsetMax = new Vector2(0, -18);

            balance = UIFactory.Txt(panel.transform, "◆ 0", 20, pal.gemBlue, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Stretch(balance.rectTransform);
            balance.rectTransform.offsetMax = new Vector2(-24, -18);

            Text sub = UIFactory.Txt(panel.transform,
                "Spend crystals to gild a number — every card of that number scores more, for the rest of the run.",
                13, pal.inkDim, TextAnchor.UpperLeft);
            UIFactory.Stretch(sub.rectTransform);
            sub.rectTransform.offsetMin = new Vector2(24, 0);
            sub.rectTransform.offsetMax = new Vector2(-24, -52);

            offersRow = UIFactory.NewRT("Offers", panel.transform);
            offersRow.anchorMin = offersRow.anchorMax = new Vector2(0.5f, 0.5f);
            offersRow.sizeDelta = new Vector2(540, 170);
            offersRow.anchoredPosition = new Vector2(0, 18);

            ownedText = UIFactory.Txt(panel.transform, "", 12, pal.gold, TextAnchor.MiddleCenter);
            RectTransform ot = ownedText.rectTransform;
            ot.anchorMin = ot.anchorMax = new Vector2(0.5f, 0);
            ot.pivot = new Vector2(0.5f, 0);
            ot.sizeDelta = new Vector2(540, 22);
            ot.anchoredPosition = new Vector2(0, 74);

            Button go = UIFactory.Btn(panel.transform, "Begin level >", pal.gold, pal.darkInk, () => gm.BeginPending(), 15);
            RectTransform sg = (RectTransform)go.transform;
            sg.anchorMin = sg.anchorMax = new Vector2(0.5f, 0);
            sg.pivot = new Vector2(0.5f, 0);
            sg.sizeDelta = new Vector2(520, 44);
            sg.anchoredPosition = new Vector2(0, 20);
            goText = go.GetComponentInChildren<Text>();

            root.SetActive(false);
        }

        public void Show()
        {
            goText.text = "Begin level " + (gm.PendingLevel + 1) + " >";
            Refresh();
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        public void Refresh()
        {
            balance.text = "◆ " + gm.crystals;

            for (int i = offersRow.childCount - 1; i >= 0; i--)
                Object.Destroy(offersRow.GetChild(i).gameObject);

            var offers = gm.Offers;
            int count = offers.Count;
            for (int i = 0; i < count; i++)
            {
                ShopOffer o = offers[i];
                int index = i;

                Image box = UIFactory.Img(offersRow, pal.bg);
                RectTransform bx = box.rectTransform;
                bx.anchorMin = bx.anchorMax = new Vector2(0.5f, 0.5f);
                bx.sizeDelta = new Vector2(168, 168);
                bx.anchoredPosition = new Vector2((i - (count - 1) / 2f) * 182f, 0);

                Text big = UIFactory.Txt(box.transform, o.num.ToString(), 40, pal.ink, TextAnchor.UpperCenter, FontStyle.Bold);
                UIFactory.Stretch(big.rectTransform);
                big.rectTransform.offsetMax = new Vector2(0, -14);

                Text eff = UIFactory.Txt(box.transform,
                    "every " + o.num + " scores x" + o.mult, 12, pal.inkDim, TextAnchor.MiddleCenter);
                UIFactory.Stretch(eff.rectTransform);
                eff.rectTransform.offsetMin = new Vector2(4, 30);
                eff.rectTransform.offsetMax = new Vector2(-4, -62);

                if (o.bought)
                {
                    Text done = UIFactory.Txt(box.transform, "✓ gilded", 13,
                        pal.SuitColor(2), TextAnchor.LowerCenter, FontStyle.Bold);
                    UIFactory.Stretch(done.rectTransform);
                    done.rectTransform.offsetMin = new Vector2(0, 18);
                }
                else
                {
                    bool afford = gm.crystals >= o.cost;
                    Button buy = UIFactory.Btn(box.transform, o.cost + " ◆",
                        afford ? pal.gold : (Color)new Color32(0x2a, 0x31, 0x38, 255),
                        afford ? pal.darkInk : pal.inkDim,
                        () => { if (gm.TryBuy(index)) Refresh(); }, 13);
                    buy.interactable = afford;
                    RectTransform brt = (RectTransform)buy.transform;
                    brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0);
                    brt.pivot = new Vector2(0.5f, 0);
                    brt.sizeDelta = new Vector2(90, 30);
                    brt.anchoredPosition = new Vector2(0, 12);
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
