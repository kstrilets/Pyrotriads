using UnityEngine;

namespace PyramidCards
{
    /// <summary>Spawns a VFX prefab at a screen position under a UI layer. Position-aware effects are
    /// triggered by the view (which knows where cards are), keeping this service a dumb spawner.
    /// With an empty <see cref="VfxLibrary"/> every call is a no-op.</summary>
    public class VfxService : MonoBehaviour
    {
        VfxLibrary library;
        RectTransform layer;   // where spawned effects are parented (an overlay canvas child)

        public void Bind(VfxLibrary lib, RectTransform vfxLayer)
        {
            library = lib;
            layer = vfxLayer;
        }

        /// <summary>Spawn the effect mapped to <paramref name="vfx"/> at a screen-space position.</summary>
        public void Play(GameVfx vfx, Vector3 screenPosition)
        {
            if (library == null || layer == null) return;

            GameObject prefab = library.Resolve(vfx, out float lifetime);
            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, layer);
            instance.transform.position = screenPosition;

            if (lifetime > 0f) Destroy(instance, lifetime);
        }
    }
}
