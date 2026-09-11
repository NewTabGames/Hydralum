using UnityEngine;

namespace MalumMenu;

// Submenu (opened from Config > Menu > "Pop-up Window Scales") that lets you scale every pop-up
// window individually. Scales are stored in CheatToggles.WindowScales and saved to the profile.
public class WindowScalesUI : MonoBehaviour
{
    public static int windowHeight = 430;
    public static int windowWidth = 360;
    public static Rect windowRect;

    private Vector2 _scroll = Vector2.zero;

    // Display order + labels for the per-window scale sliders (keys match CheatToggles.WindowScales).
    private static readonly (string key, string label)[] Windows =
    {
        ("Console", "Console"),
        ("RPCConsole", "RPC Console"),
        ("Doors", "Doors"),
        ("Tasks", "Tasks"),
        ("Protect", "Protect Players"),
        ("Roles", "Assign Roles"),
        ("ChatLog", "Chat Log"),
        ("Wardrobe", "Wardrobe Overlay"),
    };

    private void Start()
    {
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (!CheatToggles.showWindowScales || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        // This window is intentionally NOT scaled itself — it configures everything else.
        windowRect = GUI.Window((int)WindowId.WindowScalesUI, windowRect, (GUI.WindowFunction)DrawWindow, "Pop-up Window Scales");
    }

    private void DrawWindow(int windowID)
    {
        try
        {
            _scroll = GUILayout.BeginScrollView(_scroll, false, true);

            GUILayout.Label("Window Scale", GUIStylePreset.TabSubtitle);

            foreach (var (key, label) in Windows)
            {
                float cur = CheatToggles.GetWindowScale(key);
                GUILayout.Label($"{label}: {cur:F2}x");
                CheatToggles.WindowScales[key] = (float)System.Math.Round(
                    GUILayout.HorizontalSlider(cur, CheatToggles.MinWindowScale, CheatToggles.MaxWindowScale), 2);
            }

            GUILayout.Space(8);
            GUILayout.Label("Radar", GUIStylePreset.TabSubtitle);
            GUILayout.Label($"Size: {CheatToggles.radarSize}%");
            CheatToggles.radarSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.radarSize, 60f, 180f));
            GUILayout.Label($"Opacity: {CheatToggles.radarOpacity}%");
            CheatToggles.radarOpacity = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.radarOpacity, 30f, 100f));

            GUILayout.Space(8);
            GUILayout.Label("Replay Console", GUIStylePreset.TabSubtitle);
            GUILayout.Label($"Size: {CheatToggles.replaySize}%");
            CheatToggles.replaySize = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.replaySize, 60f, 180f));
            GUILayout.Label($"Opacity: {CheatToggles.replayOpacity}%");
            CheatToggles.replayOpacity = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.replayOpacity, 30f, 100f));

            GUILayout.Space(10);
            if (GUILayout.Button("Reset All to Default", GUIStylePreset.NormalButton))
            {
                foreach (var (key, _) in Windows) CheatToggles.WindowScales[key] = 1f;
                CheatToggles.radarSize = 100;
                CheatToggles.radarOpacity = 93;
                CheatToggles.replaySize = 100;
                CheatToggles.replayOpacity = 95;
            }

            GUILayout.EndScrollView();
        }
        catch { }

        GUI.DragWindow();
    }
}
