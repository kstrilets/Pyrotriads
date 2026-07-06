using UnityEngine;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Presents a <see cref="ModalRequest"/>. Self-contained: it only knows how to show a title,
    /// body and one or two buttons, and calls back the request's actions — no game logic here.</summary>
    public class ModalView
    {
        readonly ThemePalette pal;
        readonly GameObject root;
        readonly Text title, body, primaryText, secondaryText;
        readonly Button primary, secondary;

        public ModalView(Transform parent, ThemePalette palette)
        {
            pal = palette;

            Image dim = UIFactory.Img(parent, new Color(0, 0, 0, 0.75f));
            UIFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            root = dim.gameObject;

            Image panel = UIFactory.Img(dim.transform, pal.panelBg);
            RectTransform pr = panel.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(440, 230);

            title = UIFactory.Txt(panel.transform, "", 24, pal.gold, TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Stretch(title.rectTransform);
            title.rectTransform.offsetMax = new Vector2(0, -18);

            body = UIFactory.Txt(panel.transform, "", 14, pal.inkDim, TextAnchor.UpperCenter);
            UIFactory.Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(24, 0);
            body.rectTransform.offsetMax = new Vector2(-24, -58);

            primary = UIFactory.Btn(panel.transform, "", pal.gold, pal.darkInk, null, 15);
            RectTransform mp = (RectTransform)primary.transform;
            mp.anchorMin = mp.anchorMax = new Vector2(0.5f, 0);
            mp.pivot = new Vector2(0.5f, 0);
            mp.sizeDelta = new Vector2(180, 40);
            mp.anchoredPosition = new Vector2(0, 56);
            primaryText = primary.GetComponentInChildren<Text>();

            secondary = UIFactory.Btn(panel.transform, "", new Color(0, 0, 0, 0), pal.inkDim, null, 12, FontStyle.Normal);
            RectTransform ms = (RectTransform)secondary.transform;
            ms.anchorMin = ms.anchorMax = new Vector2(0.5f, 0);
            ms.pivot = new Vector2(0.5f, 0);
            ms.sizeDelta = new Vector2(180, 26);
            ms.anchoredPosition = new Vector2(0, 24);
            secondaryText = secondary.GetComponentInChildren<Text>();

            root.SetActive(false);
        }

        public void Show(ModalRequest req)
        {
            title.text = req.title;
            title.color = req.win ? pal.gold : pal.danger;
            body.text = req.body;

            primaryText.text = req.primaryLabel;
            primary.onClick.RemoveAllListeners();
            if (req.onPrimary != null) primary.onClick.AddListener(() => req.onPrimary());

            bool hasSecondary = !string.IsNullOrEmpty(req.secondaryLabel);
            secondary.gameObject.SetActive(hasSecondary);
            if (hasSecondary)
            {
                secondaryText.text = req.secondaryLabel;
                secondary.onClick.RemoveAllListeners();
                if (req.onSecondary != null) secondary.onClick.AddListener(() => req.onSecondary());
            }

            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}
