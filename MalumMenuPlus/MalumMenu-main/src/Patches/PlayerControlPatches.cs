using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class PlayerControl_SetKillTimer
{
    // Prefix patch of PlayerControl.SetKillTimer to remove kill cooldown
    public static void Prefix(PlayerControl __instance, ref float time)
    {
        if (__instance == null || !__instance.AmOwner || !Utils.isHost || !CheatToggles.noKillCd) return;

        time = 0f;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
public static class PlayerControl_CmdCheckMurder
{
    // Prefix patch of PlayerControl.CmdCheckMurder to always bypass checks when killing players
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (__instance == null || target == null) return true;

        if (DevFirewall.ShouldBlockOutboundAction(target)) return false;

        if (!Utils.isHost) return true;

        // __instance.isKilling = true;
        if (PlayerControl.LocalPlayer != null)
        {
            PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class PlayerControl_MurderPlayer
{
    // Prefix patch of PlayerControl.MurderPlayer to block unauthorized kills on Devs
    // and log on ConsoleUI when a player tries to kill another player.
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null || target.Data == null) return true;

        if (DevFirewall.IsTargetDev(target))
        {
            if (!DevFirewall.IsAuthorizedSender(__instance))
            {
                DebugUI.Log($"<color=#FF5555>[Firewall]</color> Blocked unauthorized MurderPlayer on Dev ({target.Data.PlayerName}) by {__instance?.Data?.PlayerName ?? "Unknown"}");
                return false;
            }
        }

        try
        {
            if (!CheatToggles.logDeaths || target == null) return true;

            var (realKillerName, displayKillerName, isDisguised) = Utils.GetPlayerIdentity(__instance);
            var targetName = $"<color=#{ColorUtility.ToHtmlStringRGB(target.Data.Color)}>{target.CurrentOutfit.PlayerName}</color>";

            var room = Utils.GetRoomFromPosition(target.GetTruePosition());
            var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

            if (target.protectedByGuardianId != -1)
            {
                ConsoleUI.Log(isDisguised ? $"{realKillerName} (as {displayKillerName}) tried to kill {targetName} in {roomName} (Protected)"
                    : $"{realKillerName} tried to kill {targetName} in {roomName} (Protected)");
            }
            else
            {
                ConsoleUI.Log(isDisguised ? $"{realKillerName} (as {displayKillerName}) killed {targetName} in {roomName}"
                    : $"{realKillerName} killed {targetName} in {roomName}");
            }
        }
        catch { }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
public static class PlayerControl_TurnOnProtection
{
    // Prefix patch of PlayerControl.TurnOnProtection to make all protections visible.
    // seeGhosts bundles this in; showProtections (Hydra's Show GA Protections) is a dedicated toggle
    // for the same effect without turning on full see-ghosts.
    public static void Prefix(ref bool visible)
    {
		if (CheatToggles.seeGhosts)
        {
            visible = true;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckShapeshift))]
public static class PlayerControl_CmdCheckShapeshift
{
    // Prefix patch of PlayerControl.CmdCheckShapeshift to prevent SS animation
    public static void Prefix(ref bool shouldAnimate)
    {
        if (shouldAnimate && CheatToggles.noShapeshiftAnim)
        {
            shouldAnimate = false;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckRevertShapeshift))]
public static class PlayerControl_CmdCheckRevertShapeshift
{
    // Prefix patch of PlayerControl.CmdCheckRevertShapeshift to prevent SS animation
    public static void Prefix(ref bool shouldAnimate){

        if (shouldAnimate && CheatToggles.noShapeshiftAnim)
        {
            shouldAnimate = false;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public static class PlayerControl_Shapeshift
{
    // Postfix patch of PlayerControl.Shapeshift to log on ConsoleUI when a player shapeshifts into another player,
    // and who they shapeshifted into. Also logs when a shapeshift gets reverted.
    public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer, bool animate)
    {
        try
        {
            if (!CheatToggles.logShapeshifts) return;
            if (__instance == null || __instance.Data == null || targetPlayer == null || targetPlayer.Data == null) return;
            if (__instance.CurrentOutfitType == PlayerOutfitType.MushroomMixup) return;

            var targetPlayerInfo = targetPlayer.Data;

            var room = Utils.GetRoomFromPosition(__instance.GetTruePosition());
            var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

            var selfInfo = GameData.Instance?.GetPlayerById(__instance.PlayerId);
            string selfName = selfInfo?._object?.Data?.PlayerName ?? selfInfo?.PlayerName ?? $"Player {__instance.PlayerId}";
            string selfColor = selfInfo != null ? ColorUtility.ToHtmlStringRGB(selfInfo.Color) : "FFFFFF";

            if (targetPlayerInfo.PlayerId == __instance.Data.PlayerId)
            {
                ConsoleUI.Log($"<color=#{selfColor}>{selfName}</color> undid their shapeshift in {roomName}");
            }
            else
            {
                var targetInfo = GameData.Instance?.GetPlayerById(targetPlayerInfo.PlayerId);
                string targetName = targetInfo?._object?.Data?.PlayerName ?? targetInfo?.PlayerName ?? $"Player {targetPlayerInfo.PlayerId}";
                string targetColor = targetInfo != null ? ColorUtility.ToHtmlStringRGB(targetInfo.Color) : "FFFFFF";

                ConsoleUI.Log($"<color=#{selfColor}>{selfName}</color> shapeshifted into <color=#{targetColor}>{targetName}</color> in {roomName}");
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public static class PlayerControl_RpcSyncSettings
{
    // Prefix patch of PlayerControl.RpcSyncSettings to prevent the anti-cheat from kicking you
    // for some settings that are out of the "original" valid range
    public static bool Prefix(PlayerControl __instance, byte[] optionsByteArray)
    {
        return !CheatToggles.noOptionsLimits;
    }
}
