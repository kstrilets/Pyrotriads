using UnityEngine;

namespace PyramidCards
{
    /// <summary>Named visual effects. Extend this enum to add hooks; the mapping to prefabs stays in the asset.</summary>
    public enum GameVfx
    {
        CardCleared,
        ColorRun,
        NumberRun,
        ColorTriad,
        NumberTriad,
        CrystalGained
    }

    /// <summary>Maps each <see cref="GameVfx"/> to an optional prefab spawned at the effect position.
    /// Empty slots do nothing, so VFX can be added incrementally. The prefab is expected to live under the
    /// UI canvas (a particle system or an animated <c>Image</c>). Create via
    /// <b>Assets ▸ Create ▸ Pyrotriads ▸ VFX Library</b>.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/VFX Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public GameVfx vfx;
            public GameObject prefab;
            [Tooltip("Seconds before the spawned instance is auto-destroyed. 0 = leave it alone.")]
            public float lifetime;
        }

        public Entry[] entries;

        public GameObject Resolve(GameVfx vfx, out float lifetime)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].vfx == vfx && entries[i].prefab != null)
                    {
                        lifetime = entries[i].lifetime;
                        return entries[i].prefab;
                    }
                }
            }
            lifetime = 0f;
            return null;
        }
    }
}
