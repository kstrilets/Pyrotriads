using System;
using System.Collections.Generic;

namespace PyramidCards
{
    /// <summary>What to highlight/animate on a given render. null fields mean "nothing special".</summary>
    public class RenderOpts
    {
        public CellFlags[][] flags;
        public HashSet<(int, int)> clearing;
        public HashSet<(int, int)> entering;
    }

    /// <summary>A view-agnostic description of a modal. The logic layer fills in the callbacks;
    /// any view can present it however it likes.</summary>
    public class ModalRequest
    {
        public bool win;
        public string title;
        public string body;
        public string primaryLabel;
        public Action onPrimary;
        public string secondaryLabel;   // null/empty = single-button modal
        public Action onSecondary;
    }

    /// <summary>One resolved step of the match ▸ clear ▸ refill cascade. Emitted so audio, VFX,
    /// analytics or juice can react without the rules layer knowing they exist.</summary>
    public struct CascadeStep
    {
        public int chain;          // 1-based cascade depth
        public int mult;           // chain multiplier applied (chain capped)
        public int scoreGained;    // points added this step (already multiplied)
        public int gems;           // crystals dropped this step
        public int moveBonus;      // moves granted this step
        public bool hasTriad;      // any triad resolved this step
    }
}
