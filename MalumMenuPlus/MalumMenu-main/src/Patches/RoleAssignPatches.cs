using AmongUs.GameOptions;
using HarmonyLib;

namespace MalumMenu;

// Host role assigner. The game hands out roles through LogicRoleSelectionNormal.AssignRolesFromList
// (RoleManager.SelectRoles -> AssignRolesForTeam -> AssignRolesFromList). We prefix it to force the
// host's pre-chosen roles and let the vanilla logic fill in the rest. See MalumRoleAssign.

[HarmonyPatch(typeof(LogicRoleSelectionNormal), nameof(LogicRoleSelectionNormal.AssignRolesFromList))]
public static class LogicRoleSelectionNormal_AssignRolesFromList_RoleAssign
{
    // teamMax is omitted on purpose - Harmony matches parameters by name, so we only declare the ones
    // we touch (mirrors the Assign Roles Next Round patch in HostPatches).
    public static void Prefix(
        Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players,
        Il2CppSystem.Collections.Generic.List<RoleTypes> roleList,
        ref int rolesAssigned)
    {
        if (!CheatToggles.roleAssignerEnabled) return;
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

        try
        {
            int applied = MalumRoleAssign.ApplyPreChosen(players, roleList, ref rolesAssigned);
            if (applied > 0)
                MalumMenu.Log?.LogInfo($"[RoleAssign] Forced {applied} pre-chosen role(s) this call.");
        }
        catch (System.Exception ex)
        {
            MalumMenu.Log?.LogError($"[RoleAssign] ApplyPreChosen failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class RoleAssignReset_OnJoin
{
    // PlayerIds are handed out per lobby, so clear chosen roles when we join a new one to avoid
    // applying a stale assignment to whoever now holds that id. (This fires on lobby join, not on
    // match start, so picks made in the lobby survive until the game begins.)
    public static void Postfix()
    {
        MalumRoleAssign.ClearOnLobbyJoin();
        MalumMenu.Log?.LogInfo("[RoleAssign] Cleared picks (joined lobby).");
    }
}
