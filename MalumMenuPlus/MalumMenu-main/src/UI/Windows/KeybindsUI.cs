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

        var prevMatrix = GUI.matrix;
        float scale = CheatToggles.GetWindowScale("Keybinds");
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), windowRect.position);
        windowRect = GUI.Window((int)WindowId.KeybindsUI, windowRect, (GUI.WindowFunction)DrawWindow, "Keybind Settings");
        GUI.matrix = prevMatrix;
    }

    private void DrawWindow(int windowID)
    {
        try
        {
            // Capture the next key press for the row being rebound. Escape cancels; a bare modifier keeps
            // listening so combos like Ctrl+F are recorded when the real key finally lands.
            if (_listeningFor != null && Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                var kc = Event.current.keyCode;
                if (kc == KeyCode.Escape)
                {
                    _listeningFor = null;
                }
                else if (!IsModifierKey(kc))
                {
                    var mods = CheatToggles.KeyModifier.None;
                    if (Event.current.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) mods |= CheatToggles.KeyModifier.Ctrl;
                    if (Event.current.alt || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) mods |= CheatToggles.KeyModifier.Alt;
                    if (Event.current.shift || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) mods |= CheatToggles.KeyModifier.Shift;

                    CheatToggles.Keybinds[_listeningFor] = kc;
                    CheatToggles.KeybindMods[_listeningFor] = mods;
                    KeybindListener.IgnoreKeyUntilRelease(kc);
                    _listeningFor = null;
                }
                // else: only a modifier is held so far — wait for the actual key
            }

            GUILayout.Label("Feature Hotkeys", GUIStylePreset.TabSubtitle);
            GUILayout.Label("Hold Ctrl / Alt / Shift while pressing a key to bind a combo.", GUIStylePreset.Hint);
            GUILayout.Space(4);

            foreach (var (field, label) in Binds)
            {
                CheatToggles.Keybinds.TryGetValue(field, out var key);
                CheatToggles.KeybindMods.TryGetValue(field, out var mods);

                GUILayout.BeginHorizontal();

                GUILayout.Label(label, GUILayout.Width(225f));

                bool listening = _listeningFor == field;
                string btnText = listening ? "<color=yellow>Press keys...</color>" : $"<b>{FormatBind(key, mods)}</b>";
                if (GUILayout.Button(btnText, GUIStylePreset.NormalButton, GUILayout.Width(140f), GUILayout.Height(22f)))
                {
                    _listeningFor = listening ? null : field;
                }

                GUI.enabled = key != KeyCode.None;
                if (GUILayout.Button("Clear", GUIStylePreset.NormalButton, GUILayout.Width(55f), GUILayout.Height(22f)))
                {
                    CheatToggles.Keybinds[field] = KeyCode.None;
                    CheatToggles.KeybindMods[field] = CheatToggles.KeyModifier.None;
                    if (_listeningFor == field) _listeningFor = null;
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }
        catch { }

        GUI.DragWindow();
    }

    // "None", "F", or a combo like "Ctrl+Shift+F" for display on the bind button.
    private static string FormatBind(KeyCode key, CheatToggles.KeyModifier mods)
    {
        if (key == KeyCode.None) return "None";
        return CheatToggles.ModifierPrefix(mods) + key;
    }

    // Modifier keys are never bound as the main key; holding one just adds to the combo.
    private static bool IsModifierKey(KeyCode k) =>
        k is KeyCode.LeftControl or KeyCode.RightControl
          or KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.AltGr
          or KeyCode.LeftShift or KeyCode.RightShift
          or KeyCode.LeftCommand or KeyCode.RightCommand
          or KeyCode.LeftWindows or KeyCode.RightWindows;
}
