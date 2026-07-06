using UnityEngine;

namespace PyramidCards
{
    /// <summary>Optional art + layout for cards. Every sprite slot is nullable: leave it empty and the
    /// card falls back to the flat-colour look. Drop sprites in here (no code change) to skin the game.
    /// Create via <b>Assets ▸ Create ▸ Pyrotriads ▸ Card Visual Theme</b>.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/Card Visual Theme", fileName = "CardVisualTheme")]
    public class CardVisualTheme : ScriptableObject
    {
        [Header("Layout")]
        [Min(1f)] public float cardWidth = 74f;
        [Min(1f)] public float cardHeight = 96f;
        [Min(0f)] public float gap = 14f;
        [Tooltip("Inset of the face panel inside the card rim.")]
        public float facePanelInset = 6f;

        [Header("Card art (optional — empty = flat colour)")]
        [Tooltip("Rim / background behind every card. Tinted by suit colour when set.")]
        public Sprite cardRim;
        [Tooltip("Panel behind the number on face-up cards.")]
        public Sprite facePanel;
        [Tooltip("Per-suit pip shown on face-down cards. Tinted by suit-light when set.")]
        public Sprite[] suitPips;
        [Tooltip("Optional per-suit icon drawn on face-up cards.")]
        public Sprite[] suitIcons;
        [Tooltip("Background of the 'gilded x2/x3' tag.")]
        public Sprite gildTagBackground;
        [Tooltip("Sprite for empty holes in the pyramid.")]
        public Sprite holeSprite;

        [Header("Typography (optional — empty = built-in font)")]
        public Font font;

        public Sprite Pip(int suit) { return Pick(suitPips, suit); }
        public Sprite Icon(int suit) { return Pick(suitIcons, suit); }

        static Sprite Pick(Sprite[] arr, int i)
        {
            if (arr != null && i >= 0 && i < arr.Length) return arr[i];
            return null;
        }
    }
}
