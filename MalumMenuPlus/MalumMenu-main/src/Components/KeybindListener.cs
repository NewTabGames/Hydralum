using UnityEngine;

namespace MalumMenu;

public class KeybindListener : MonoBehaviour
{
    // A key just assigned in the Keybind menu — ignored until it's released so the very press used to
    // bind it doesn't also trigger the feature.
    private static KeyCode _ignoreUntilRelease = KeyCode.None;
    public static void IgnoreKeyUntilRelease(KeyCode key) => _ignoreUntilRelease = key;

    public void Update()
    {
        if (MalumMenu.isPanicked) return;

        // Don't fire keybinds while the user is actively assigning one in the Keybind menu
        if (KeybindsUI.IsCapturing) return;

        // Keybinds aren't triggered from typing in the chat
        if (HudManager.InstanceExists && HudManager.Instance.Chat && HudManager.Instance.Chat.IsOpenOrOpening) return;

        // Once the just-bound key is released, stop ignoring it
        if (_ignoreUntilRelease != KeyCode.None && !Input.GetKey(_ignoreUntilRelease))
        {
            _ignoreUntilRelease = KeyCode.None;
        }

        // Check each keybind to see if the user pressed it and toggle the corresponding cheat
        foreach (var (name, key) in CheatToggles.Keybinds)
        {
            if (key == KeyCode.None) continue;
            if (key == _ignoreUntilRelease) continue;
            if (!Input.GetKeyDown(key)) continue;

            if (!CheatToggles.ToggleFields.TryGetValue(name, out var field)) continue;

            var current = (bool)field.GetValue(null);
            field.SetValue(null, !current);
        }
    }
}
