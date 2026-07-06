using UnityEngine;

namespace PyramidCards
{
    /// <summary>The only component you need to place by hand.
    /// New scene -> empty GameObject -> add PyramidBootstrap -> Play.</summary>
    public class PyramidBootstrap : MonoBehaviour
    {
        void Start()
        {
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();

            var uiGO = new GameObject("GameUI");
            var ui = uiGO.AddComponent<GameUI>();

            gm.ui = ui;
            ui.Init(gm);
            gm.StartLevel(0);
        }
    }
}
