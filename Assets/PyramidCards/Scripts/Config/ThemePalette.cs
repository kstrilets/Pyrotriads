using UnityEngine;

namespace PyramidCards
{
    /// <summary>All colours in one asset so a tech-artist can re-skin the whole game without code.
    /// Create via <b>Assets ▸ Create ▸ Pyrotriads ▸ Theme Palette</b>. Defaults mirror the original look.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/Theme Palette", fileName = "ThemePalette")]
    public class ThemePalette : ScriptableObject
    {
        [Header("Suit palettes (index = suit)")]
        public Color[] suitColors =
        {
            new Color32(0xd9, 0x43, 0x3f, 255), new Color32(0x41, 0x78, 0xd6, 255),
            new Color32(0x3a, 0xa7, 0x65, 255), new Color32(0xe0, 0xa8, 0x3c, 255)
        };
        public Color[] suitLight =
        {
            new Color32(0xef, 0x6a, 0x66, 255), new Color32(0x6f, 0x9a, 0xea, 255),
            new Color32(0x62, 0xc4, 0x89, 255), new Color32(0xf0, 0xc4, 0x68, 255)
        };
        public Color[] suitDeep =
        {
            new Color32(0x7d, 0x20, 0x1f, 255), new Color32(0x1e, 0x3a, 0x73, 255),
            new Color32(0x1c, 0x54, 0x36, 255), new Color32(0x8a, 0x5f, 0x15, 255)
        };

        [Header("Surfaces")]
        public Color bg = new Color32(0x10, 0x16, 0x1b, 255);
        public Color felt = new Color32(0x13, 0x32, 0x2b, 255);
        public Color panelBg = new Color32(0x16, 0x1f, 0x25, 255);

        [Header("Text & accents")]
        public Color ink = new Color32(0xe9, 0xe4, 0xd6, 255);
        public Color inkDim = new Color32(0x9a, 0xa3, 0x9a, 255);
        public Color gold = new Color32(0xd9, 0xb2, 0x5a, 255);
        public Color triadGlow = new Color32(0xb7, 0x85, 0xf0, 255);
        public Color gemBlue = new Color32(0x8f, 0xc6, 0xff, 255);
        public Color cream = new Color32(0xf5, 0xf0, 0xe2, 255);
        public Color danger = new Color32(0xd9, 0x43, 0x3f, 255);

        [Header("Dark ink used on gold buttons")]
        public Color darkInk = new Color32(0x1a, 0x13, 0x0a, 255);

        // ---- safe accessors (never throw if a suit array is short) ----
        public Color SuitColor(int suit) { return Pick(suitColors, suit, Color.magenta); }
        public Color SuitLight(int suit) { return Pick(suitLight, suit, Color.magenta); }
        public Color SuitDeep(int suit) { return Pick(suitDeep, suit, Color.black); }

        static Color Pick(Color[] arr, int i, Color fallback)
        {
            if (arr != null && i >= 0 && i < arr.Length) return arr[i];
            return fallback;
        }
    }
}
