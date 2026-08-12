using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// Role patches adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
public static class SabotageButton_DoClick
{
    // Normally the sabotage button is blocked while you are inside a vent. When Sabotage In Vents is
    // on and you are an impostor in a vent, open the sabotage map directly instead of running the
    // blocked default. Any other case runs the normal button behaviour.
    public static bool Prefix()
    {
        var player = PlayerControl.LocalPlayer;

        if (CheatToggles.sabotageInVents && player != null && player.Data != null
            && player.inVent && RoleManager.IsImpostorRole(player.Data.RoleType))
        {
            HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Sabotage });
            return false;
        }

        return true;
    }
}
