using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;

namespace MalumMenu;

// Visual patches adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.

[HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
public static class ShhhBehaviour_PlayAnimation
{
    // Skip the "Shhh" emblem animation at the start of a round. PlayAnimation returns a coroutine
    // that the game starts, so we return an empty (already-finished) one instead of null to be safe.
    public static bool Prefix(ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (!CheatToggles.skipShhh) return true;

        try { HudManager.Instance.shhhEmblem.gameObject.SetActive(false); } catch { }

        __result = EmptyRoutine().WrapToIl2Cpp();
        return false;
    }

    private static IEnumerator EmptyRoutine()
    {
        yield break;
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
public static class GameData_ShowNotification
{
    // GameData.ShowNotification only has proper wording for ExitGame/Kicked/Banned; everything else
    // falls back to a generic error. Replace the common ones with specific messages.
    public static bool Prefix(string playerName, DisconnectReasons reason)
    {
        if (!CheatToggles.accurateDisconnects) return true;

        switch (reason)
        {
            // Already worded correctly by the game itself.
            case DisconnectReasons.ExitGame:
            case DisconnectReasons.Kicked:
            case DisconnectReasons.Banned:
            case DisconnectReasons.Error:
                return true;

            case DisconnectReasons.Hacking:
                HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was banned by the Among Us anticheat for hacking.");
                return false;

            case DisconnectReasons.DuplicateConnectionDetected:
                HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to duplicate login.");
                return false;

            case DisconnectReasons.ClientTimeout:
                HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to timeout.");
                return false;

            default:
                HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected due to {reason}.");
                return false;
        }
    }
}

// Show GA Protections (CheatToggles.showProtections) is folded into the existing
// PlayerControlPatches.PlayerControl_TurnOnProtection prefix (which already handles seeGhosts) rather
// than duplicated here - the two would just set the same visible flag.
