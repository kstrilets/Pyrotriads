using UnityEngine;

namespace PyramidCards
{
    [System.Serializable]
    public struct LevelDefinition
    {
        [Tooltip("Score needed to clear the level.")]
        public int target;
        [Tooltip("Move budget for the level.")]
        public int moves;
        [Range(0f, 1f), Tooltip("Chance each dealt card starts face-down (colour side up).")]
        public float faceDown;

        public LevelDefinition(int target, int moves, float faceDown)
        {
            this.target = target; this.moves = moves; this.faceDown = faceDown;
        }
    }

    /// <summary>The ordered list of levels, plus an endless overflow rule for levels past the list.
    /// Create via <b>Assets ▸ Create ▸ Pyrotriads ▸ Level Set</b>.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/Level Set", fileName = "LevelSet")]
    public class LevelSet : ScriptableObject
    {
        public LevelDefinition[] levels =
        {
            new LevelDefinition(155, 12, 0.50f),
            new LevelDefinition(160, 12, 0.50f),
            new LevelDefinition(170, 12, 0.50f),
            new LevelDefinition(175, 12, 0.50f),
            new LevelDefinition(185, 12, 0.50f),
        };

        [Header("Endless overflow (levels beyond the list above)")]
        [Tooltip("Target increase added per level once the defined list is exhausted.")]
        public int overflowTargetStep = 10;

        public LevelDefinition Get(int i)
        {
            if (levels != null && i < levels.Length && i >= 0) return levels[i];

            LevelDefinition last = (levels != null && levels.Length > 0)
                ? levels[levels.Length - 1]
                : new LevelDefinition(185, 12, 0.50f);

            int definedCount = (levels != null) ? levels.Length : 0;
            int over = i - definedCount + 1;
            return new LevelDefinition(last.target + over * overflowTargetStep, last.moves, last.faceDown);
        }
    }
}
