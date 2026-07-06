using UnityEngine;

namespace PyramidCards
{
    /// <summary>Every rules/scoring knob as a designer-tunable asset.
    /// Create via <b>Assets ▸ Create ▸ Pyrotriads ▸ Game Rules</b>. Defaults mirror the original prototype,
    /// so the game plays identically until a designer overrides a value.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/Game Rules", fileName = "GameRules")]
    public class GameRulesConfig : ScriptableObject
    {
        [Header("Deck")]
        [Min(1)] public int suits = 4;
        [Min(1)] public int nums = 11;                              // deck = suits x nums unique cards
        public int[] rowSizes = { 1, 2, 3, 4, 5 };                 // pyramid shape (top to bottom)

        [Header("Scoring — colour runs (index = run length, capped at last entry)")]
        public int[] colorPts = { 0, 0, 2, 5, 9, 14 };

        [Header("Scoring — number runs (sum of face values + this length bonus)")]
        public int[] numberLenBonus = { 0, 0, 0, 3, 7, 12 };

        [Header("Triads")]
        public int triadNumMult = 6;                               // number triad = value * mult + bonus
        public int triadNumBonus = 6;
        public int triadColorMoves = 2;                            // colour triad: no points, +moves

        [Header("Cascade")]
        [Min(1)] public int chainCap = 4;                          // cascade multiplier tops out here
        [Min(0f)] public float clearSeconds = 0.42f;
        [Min(0f)] public float fillSeconds = 0.34f;

        [Header("Naming")]
        public string[] suitNames = { "Ruby", "Sapphire", "Emerald", "Citrine" };

        /// <summary>Project the tunable values onto the plain object the pure logic layer consumes.</summary>
        public ScoringRules ToScoringRules()
        {
            return new ScoringRules
            {
                suits = suits,
                nums = nums,
                colorPts = colorPts,
                numberLenBonus = numberLenBonus,
                triadNumMult = triadNumMult,
                triadNumBonus = triadNumBonus,
                triadColorMoves = triadColorMoves,
                suitNames = suitNames
            };
        }
    }
}
