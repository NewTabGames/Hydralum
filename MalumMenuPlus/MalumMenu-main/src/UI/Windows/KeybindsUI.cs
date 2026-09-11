using UnityEngine;

namespace MalumMenu;

// Submenu (opened from Config > Menu > "Keybind Settings") for assigning hotkeys to specific
// features. Keys are stored in CheatToggles.Keybinds (applied by KeybindListener) and are saved to
// the profile alongside their toggles, so they persist with the config like everything else.
public class KeybindsUI : MonoBehaviour
{
    public static int windowHeight = 380;
    public static int windowWidth = 460;
    public static Rect windowRect;

    // The toggle field currently listening for a key press (null = not binding).
    private static string _listeningFor;

    // True while waiting for the user to press a key to bind — KeybindListener pauses hotkeys then.
    public static bool IsCapturing => _listeningFor != null;

    // Feature -> label. The field name must match a bool in CheatToggles (KeybindListener toggles it).
    private static readonly (string field, string label)[] Binds =
    {
        ("replay", "Replay Console"),
        ("showConsole", "Console"),
        ("showDoorsMenu", "Doors Menu"),
        ("showTasksMenu", "Task Menu"),
        ("sabotageAllNoDoors", "Sabotage All"),
        ("fixSabotage", "Fix Sabotages"),
        ("autoCompleteTasks", "Auto Complete Tasks"),
        ("completeMyTasks", "Complete All Tasks"),
        ("autoCompleteNoAlwaysUpdates", "Disable Auto Task on Taskbar Updates"),
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
        if (!CheatToggles.showKeybindSettings || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        windowRect = GUI.Window((int)WindowId.KeybindsUI, windowRect, (GUI.WindowFunction)DrawWindow, "Keybind Settings");
    }

    private void DrawWindow(int windowID)
    {
        try
        {
            // Capture the next key press for the row being rebound (Escape cancels).
            if (_listeningFor != null && Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                if (Event.current.keyCode != KeyCode.Escape)
                {
                    CheatToggles.Keybinds[_listeningFor] = Event.current.keyCode;
                    KeybindListener.IgnoreKeyUntilRelease(Event.current.keyCode);
                }
                _listeningFor = null;
            }

            GUILayout.Label("Feature Hotkeys", GUIStylePreset.TabSubtitle);
            GUILayout.Space(4);

            foreach (var (field, label) in Binds)
            {
                CheatToggles.Keybinds.TryGetValue(field, out var key);

                GUILayout.BeginHorizontal();

                GUILayout.Label(label, GUILayout.Width(250f));

                bool listening = _listeningFor == field;
                string btnText = listening ? "<color=yellow>Press a key...</color>" : $"<b>{(key == KeyCode.None ? "None" : key.ToString())}</b>";
                if (GUILayout.Button(btnText, GUIStylePreset.NormalButton, GUILayout.Width(110f), GUILayout.Height(22f)))
                {
                    _listeningFor = listening ? null : field;
                }

                GUI.enabled = key != KeyCode.None;
                if (GUILayout.Button("Clear", GUIStylePreset.NormalButton, GUILayout.Width(55f), GUILayout.Height(22f)))
                {
                    CheatToggles.Keybinds[field] = KeyCode.None;
                    if (_listeningFor == field) _listeningFor = null;
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }
        catch { }

        GUI.DragWindow();
    }
}
