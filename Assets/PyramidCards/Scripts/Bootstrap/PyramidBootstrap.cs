using UnityEngine;

namespace PyramidCards
{
    /// <summary>The only component you place by hand: new scene ▸ empty GameObject ▸ add PyramidBootstrap ▸ Play.
    /// Assign config assets in the Inspector to tune the game; leave any empty and a code-default is used,
    /// so it runs out of the box. This is the composition root — it builds the manager, view and services
    /// and wires them together, keeping every other class free of construction concerns.</summary>
    public class PyramidBootstrap : MonoBehaviour
    {
        [Header("Rules & levels (optional — defaults used if empty)")]
        [SerializeField] GameRulesConfig rules;
        [SerializeField] LevelSet levels;

        [Header("Look & feel (optional)")]
        [SerializeField] ThemePalette palette;
        [SerializeField] CardVisualTheme cardTheme;

        [Header("Audio & VFX (optional — empty = silent / no effects)")]
        [SerializeField] AudioLibrary audioLibrary;
        [SerializeField] VfxLibrary vfxLibrary;

        void Start()
        {
            // Fall back to freshly created defaults so the game is fully playable with zero assets assigned.
            if (rules == null) rules = ScriptableObject.CreateInstance<GameRulesConfig>();
            if (levels == null) levels = ScriptableObject.CreateInstance<LevelSet>();
            if (palette == null) palette = ScriptableObject.CreateInstance<ThemePalette>();
            if (cardTheme == null) cardTheme = ScriptableObject.CreateInstance<CardVisualTheme>();

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.Configure(rules, levels);

            var vfx = new GameObject("VfxService").AddComponent<VfxService>();
            var audioService = new GameObject("AudioService").AddComponent<AudioService>();

            var view = new GameObject("GameView").AddComponent<GameView>();
            view.Init(gm, palette, cardTheme, vfx);

            vfx.Bind(vfxLibrary, view.VfxLayer);
            audioService.Bind(gm, audioLibrary);

            gm.StartLevel(0);
        }
    }
}
