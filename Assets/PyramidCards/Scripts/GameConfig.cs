using UnityEngine;

namespace PyramidCards
{
    public struct LevelConfig
    {
        public int target;
        public int moves;
        public float faceDown;
        public LevelConfig(int target, int moves, float faceDown)
        {
            this.target = target; this.moves = moves; this.faceDown = faceDown;
        }
    }

    /// <summary>Every tuning knob in one place. Values mirror the HTML prototype.</summary>
    public static class GameConfig
    {
        public const int Suits = 4;
        public const int Nums = 11;                       // deck = 4 x 11 = 44 unique cards
        public static readonly int[] RowSizes = { 1, 2, 3, 4, 5 }; // 15 dealt, 29 in the draw pile

        // colour runs (face-down, same suit, adjacent), indexed by run length (capped at 5)
        public static readonly int[] ColorPts = { 0, 0, 2, 5, 9, 14 };
        // number runs score sum-of-values + this length bonus
        public static readonly int[] NumberLenBonus = { 0, 0, 0, 3, 7, 12 };

        public const int TriadNumMult = 6;                // number triad = value * 6 + 6
        public const int TriadNumBonus = 6;
        public const int TriadColorMoves = 2;             // colour triad: no points, +2 moves
        public const int ChainCap = 4;                    // cascade multiplier tops out here

        public const float ClearSeconds = 0.42f;
        public const float FillSeconds = 0.34f;

        public static readonly string[] SuitNames = { "Ruby", "Sapphire", "Emerald", "Citrine" };

        static Color C(int r, int g, int b) { return new Color32((byte)r, (byte)g, (byte)b, 255); }

        public static readonly Color[] SuitColors = { C(0xd9,0x43,0x3f), C(0x41,0x78,0xd6), C(0x3a,0xa7,0x65), C(0xe0,0xa8,0x3c) };
        public static readonly Color[] SuitLight  = { C(0xef,0x6a,0x66), C(0x6f,0x9a,0xea), C(0x62,0xc4,0x89), C(0xf0,0xc4,0x68) };
        public static readonly Color[] SuitDeep   = { C(0x7d,0x20,0x1f), C(0x1e,0x3a,0x73), C(0x1c,0x54,0x36), C(0x8a,0x5f,0x15) };

        public static readonly Color Bg        = C(0x10,0x16,0x1b);
        public static readonly Color Felt      = C(0x13,0x32,0x2b);
        public static readonly Color PanelBg   = C(0x16,0x1f,0x25);
        public static readonly Color Ink       = C(0xe9,0xe4,0xd6);
        public static readonly Color InkDim    = C(0x9a,0xa3,0x9a);
        public static readonly Color Gold      = C(0xd9,0xb2,0x5a);
        public static readonly Color TriadGlow = C(0xb7,0x85,0xf0);
        public static readonly Color GemBlue   = C(0x8f,0xc6,0xff);
        public static readonly Color Cream     = C(0xf5,0xf0,0xe2);
        public static readonly Color Danger    = C(0xd9,0x43,0x3f);

        static readonly LevelConfig[] Levels =
        {
            new LevelConfig(155, 12, 0.50f),
            new LevelConfig(160, 12, 0.50f),
            new LevelConfig(170, 12, 0.50f),
            new LevelConfig(175, 12, 0.50f),
            new LevelConfig(185, 12, 0.50f),
        };

        public static LevelConfig GetLevel(int i)
        {
            if (i < Levels.Length) return Levels[i];
            int over = i - Levels.Length + 1;
            return new LevelConfig(185 + over * 10, 12, 0.50f);
        }
    }
}
