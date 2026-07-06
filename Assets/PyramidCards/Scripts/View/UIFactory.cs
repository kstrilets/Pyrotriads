using System;
using UnityEngine;
using UnityEngine.UI;

namespace PyramidCards
{
    /// <summary>Small helpers for building uGUI in code. The view is still built at runtime, but every
    /// visual now reads colour/sprite/font from config, so a themed skin needs no code changes.</summary>
    public static class UIFactory
    {
        static Font _builtin;

        /// <summary>Optional project font (from <see cref="CardVisualTheme"/>); falls back to the built-in one.</summary>
        public static Font FontOverride;

        public static Font UIFont
        {
            get
            {
                if (FontOverride != null) return FontOverride;
                if (_builtin == null) _builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _builtin;
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

        public static Image Img(Transform parent, Color color, Sprite sprite = null)
        {
            RectTransform rt = NewRT("Image", parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;   // respects 9-slice borders when the sprite has them
            }
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
            Action onClick, int fontSize = 15, FontStyle style = FontStyle.Bold, Sprite sprite = null)
        {
            Image img = Img(parent, bg, sprite);
            img.raycastTarget = true;
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Text t = Txt(img.transform, label, fontSize, fg, TextAnchor.MiddleCenter, style);
            Stretch(t.rectTransform);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }
    }
}
