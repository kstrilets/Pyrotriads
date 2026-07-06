using System.Collections.Generic;

namespace PyramidCards
{
    public enum ComboType { Triad, TriadColor, Number, Color }

    public class Combo
    {
        public ComboType type;
        public int points;
        public int moveBonus;         // colour triads pay moves instead of points
        public bool gem;              // any triad drops a crystal
        public int suit = -1;
        public int mult = 1;          // chain multiplier, filled in when logged
        public string label;
        public List<(int r, int c)> cells = new List<(int, int)>();
    }

    public class CellFlags
    {
        public bool color, number, triad, triadColor;
    }

    public class EvalResult
    {
        public int total;
        public List<Combo> combos = new List<Combo>();
        public CellFlags[][] flags;
    }

    /// <summary>Everything the <see cref="Evaluator"/> needs to score a board, as plain data.
    /// Produced from <c>GameRulesConfig</c> so scoring never touches a ScriptableObject or UnityEngine.</summary>
    public class ScoringRules
    {
        public int suits = 4;
        public int nums = 11;

        // colour runs (face-down, same suit, adjacent), indexed by run length (capped at 5)
        public int[] colorPts = { 0, 0, 2, 5, 9, 14 };
        // number runs score sum-of-values + this length bonus
        public int[] numberLenBonus = { 0, 0, 0, 3, 7, 12 };

        public int triadNumMult = 6;     // number triad = value * mult + bonus
        public int triadNumBonus = 6;
        public int triadColorMoves = 2;  // colour triad: no points, +moves

        public string[] suitNames = { "Ruby", "Sapphire", "Emerald", "Citrine" };
    }
}
