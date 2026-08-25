using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace MalumMenu;

// Host-authoritative powers adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
// These are Harmony patches that read their CheatToggles directly, mirroring the No Game End
// patches in LogicGameFlowPatches. They only have a real effect while you are the host.

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
public static class ShipStatus_CloseDoorsOfType
{
    // As host, the game calls CloseDoorsOfType to shut doors (sabotage or the Close Doors button).
    // Blocking it stops any doors from being closed for the whole lobby.
    public static bool Prefix()
    {
        return !CheatToggles.disableCloseDoors;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public static class PlayerControl_ReportDeadBody
{
    // When a player reports a body their client sends a ReportDeadBody RPC to the host, which runs
    // this method to start the meeting. Ignoring it as host blocks every meeting in the lobby.
    public static bool Prefix()
    {
        return !CheatToggles.disableMeetings;
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
public static class InnerNetClient_CanBan
{
    // The game normally forbids banning mid-game. Forcing CanBan to AmHost lets you ban during a
    // match, but only as the real host - it never grants ban rights to a non-host.
    public static void Postfix(InnerNetClient __instance, ref bool __result)
    {
        if (CheatToggles.banMidGame)
        {
            __result = __instance.AmHost;
        }
    }
}

[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
public static class SecurityCameraSystemType_UpdateSystem
{
    // When another player opens/closes the security cameras (as host we receive their system
    // update), blind their comms so they can't actually watch. operation == 1 means they started
    // watching; any other value means they stopped, so we clear the comms state for them again.
    public static void Postfix(PlayerControl player, MessageReader msgReader)
    {
        if (!CheatToggles.disableSecurityCameras || !Utils.isHost) return;
        if (player == null || player.OwnerId == AmongUsClient.Instance.HostId) return;

        try
        {
            msgReader.Position--;
            byte operation = msgReader.ReadByte();

            MalumHost.SendCommsStateTo(player.OwnerId, operation == 1);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(LogicRoleSelectionNormal), nameof(LogicRoleSelectionNormal.AssignRolesFromList))]
public static class LogicRoleSelectionNormal_AssignRolesFromList
{
    // Assign ourselves a chosen role at the start of the next round (adapted from Hydra's
    // AlwaysImposter). The 2026.6.5 signature gained a teamMax parameter; Harmony matches the
    // parameters we declare by name, so we simply omit teamMax.
    public static void Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players,
        Il2CppSystem.Collections.Generic.List<RoleTypes> roleList, ref int rolesAssigned)
    {
        if (!CheatToggles.assignRolesNextRound || !AmongUsClient.Instance.AmHost) return;

        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

        try
        {
            var assignedRole = MalumHost.NextRoundRole;

            // AssignRolesFromList runs several times with different player lists. Only act on the call
            // whose list contains our own NetworkedPlayerInfo, then remove ourselves so the game doesn't
            // assign us a second role.
            Il2CppSystem.Predicate<NetworkedPlayerInfo> isSelf =
                (Il2CppSystem.Predicate<NetworkedPlayerInfo>)(p => p == PlayerControl.LocalPlayer.Data);
            int playerIndex = players.FindIndex(isSelf);
            if (playerIndex == -1) return;

            players.RemoveAt(playerIndex);

            // If the role we want is in this list, remove one instance so the head count stays correct.
            Il2CppSystem.Predicate<RoleTypes> isRole =
                (Il2CppSystem.Predicate<RoleTypes>)(r => r == assignedRole);
            int roleIndex = roleList.FindIndex(isRole);
            if (roleIndex != -1) roleList.RemoveAt(roleIndex);

            // Ghost-role edge case: if we'd be the last player assigned and the role is a ghost role, the
            // intro cutscene never plays and the lobby black-screens. Assign a normal role first.
            if (RoleManager.IsGhostRole(assignedRole) && players.Count == 0)
            {
                PlayerControl.LocalPlayer.RpcSetRole(
                    RoleManager.IsImpostorRole(assignedRole) ? RoleTypes.Impostor : RoleTypes.Crewmate, true);
            }

            PlayerControl.LocalPlayer.RpcSetRole(assignedRole, true);
            rolesAssigned++;
        }
        catch { }
    }
}
