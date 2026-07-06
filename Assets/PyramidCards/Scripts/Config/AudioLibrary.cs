using UnityEngine;

namespace PyramidCards
{
    /// <summary>Named game sounds. Extend this enum to add new hooks; the mapping to clips stays in the asset.</summary>
    public enum GameSfx
    {
        Flip,
        Swap,
        InvalidMove,
        ClearStep,
        Triad,
        LevelWon,
        LevelLost,
        Purchase
    }

    /// <summary>Maps each <see cref="GameSfx"/> to an optional clip. Empty slots simply stay silent, so audio
    /// can be added incrementally. Create via <b>Assets ▸ Create ▸ Pyrotriads ▸ Audio Library</b>.</summary>
    [CreateAssetMenu(menuName = "Pyrotriads/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public GameSfx sfx;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Range(0f, 1f)] public float masterVolume = 1f;
        public Entry[] entries;

        [Header("Optional looping music")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        /// <summary>Returns the clip for a sound (or null), and its per-clip volume already scaled by master.</summary>
        public AudioClip Resolve(GameSfx sfx, out float volume)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].sfx == sfx && entries[i].clip != null)
                    {
                        float v = entries[i].volume <= 0f ? 1f : entries[i].volume;
                        volume = v * masterVolume;
                        return entries[i].clip;
                    }
                }
            }
            volume = 0f;
            return null;
        }
    }
}
