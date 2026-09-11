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

            // Require the bound modifier combo to be held exactly (so e.g. "F" doesn't fire on Ctrl+F, and
            // vice-versa), letting combos coexist with plain-key binds without clashing.
            CheatToggles.KeybindMods.TryGetValue(name, out var mods);
            if (!ModifiersHeldExactly(key, mods)) continue;

            if (!CheatToggles.ToggleFields.TryGetValue(name, out var field)) continue;

            var current = (bool)field.GetValue(null);
            field.SetValue(null, !current);
        }
    }

    // True only when the currently-held Ctrl/Alt/Shift state matches the bind's modifier set exactly.
    private static bool ModifiersHeldExactly(KeyCode key, CheatToggles.KeyModifier mods)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // If the bound key IS a modifier (a legacy plain bind like LeftAlt), that modifier is unavoidably
        // held when the key fires - so don't count it, otherwise the bind could never match.
        if (key is KeyCode.LeftControl or KeyCode.RightControl) ctrl = mods.HasFlag(CheatToggles.KeyModifier.Ctrl);
        if (key is KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.AltGr) alt = mods.HasFlag(CheatToggles.KeyModifier.Alt);
        if (key is KeyCode.LeftShift or KeyCode.RightShift) shift = mods.HasFlag(CheatToggles.KeyModifier.Shift);

        return ctrl == mods.HasFlag(CheatToggles.KeyModifier.Ctrl)
            && alt == mods.HasFlag(CheatToggles.KeyModifier.Alt)
            && shift == mods.HasFlag(CheatToggles.KeyModifier.Shift);
    }
}
