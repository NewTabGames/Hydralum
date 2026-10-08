using System.Collections.Generic;
using AmongUs.GameOptions;

namespace MalumMenu;

// Host role assigner. The host picks a role for specific players in the lobby; when the match starts
// those players are forced into their roles and the game assigns everyone else as usual.
//
// Implementation: we hook LogicRoleSelectionNormal.AssignRolesFromList - the method the game actually
// uses to hand out roles (RoleManager.SelectRoles -> AssignRolesForTeam -> AssignRolesFromList), and
// the same hook Malum's "Assign Roles Next Round" uses. For each call we pull the pre-chosen players
// out of the list and RpcSetRole them ourselves, letting the vanilla logic fill in everyone we leave
// behind (which also keeps the intro cutscene working).
public static class MalumRoleAssign
{
    // PlayerId -> chosen role. Absence means "Random" (leave to the game). Cleared when joining a new
    // lobby, since PlayerIds are handed out per lobby.
    private static readonly Dictionary<byte, RoleTypes> _assigned = new();

    // Roles the host can pick per player. Ghost/death roles (Guardian Angel, Spirit Guide, the ...Ghost
    // roles) aren't here because the game never hands those out at match start - they're assigned on
    // death - so use the Change Role tab for those.
    public static readonly RoleTypes[] SelectableRoles =
    {
        RoleTypes.Crewmate,
        RoleTypes.Scientist,
        RoleTypes.Engineer,
        RoleTypes.Noisemaker,
        RoleTypes.Tracker,
        RoleTypes.Detective,
        RoleTypes.Judge,
        RoleTypes.Impostor,
        RoleTypes.Shapeshifter,
        RoleTypes.Phantom,
        RoleTypes.Viper
    };

    public static bool IsImpostorRole(RoleTypes role)
        => role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter
        || role == RoleTypes.Phantom || role == RoleTypes.Viper;

    // ---- UI helpers -----------------------------------------------------------------------------

    // Returns the chosen role for a player, or null for "Random".
    public static RoleTypes? GetChosen(byte playerId)
        => _assigned.TryGetValue(playerId, out var role) ? role : null;

    // Cycle a player's chosen role forward (dir > 0) or backward (dir < 0) through
    // [Random, SelectableRoles...]. Index 0 is the "Random" sentinel.
    public static void CycleRole(byte playerId, int dir)
    {
        int count = SelectableRoles.Length + 1; // +1 for Random at index 0
        int current = 0;
        if (_assigned.TryGetValue(playerId, out var role))
        {
            current = System.Array.IndexOf(SelectableRoles, role) + 1;
            if (current < 1) current = 0;
        }

        int next = ((current + dir) % count + count) % count;
        if (next == 0) _assigned.Remove(playerId);            // back to Random
        else _assigned[playerId] = SelectableRoles[next - 1];
    }

    public static void ClearAll() => _assigned.Clear();

    public static int CountChosen() => _assigned.Count;

    public static int CountChosenImpostors()
    {
        int n = 0;
        foreach (var role in _assigned.Values) if (IsImpostorRole(role)) n++;
        return n;
    }

    // The game's own impostor cap for a given player count (3 at 9+, 2 at 7+, else 1), just for the
    // UI summary.
    public static int GetMaxImpostorAmount(int playerAmount)
    {
        var options = GameOptionsManager.Instance != null ? GameOptionsManager.Instance.CurrentGameOptions : null;
        int numImps = options != null ? options.NumImpostors : 1;
        if (playerAmount >= 9) return System.Math.Min(numImps, 3);
        if (playerAmount >= 7) return System.Math.Min(numImps, 2);
        return 1;
    }

    public static List<PlayerControl> CollectPlayers()
    {
        var list = new List<PlayerControl>();
        var all = PlayerControl.AllPlayerControls;
        if (all == null) return list;
        foreach (var p in all)
            if (p != null && p.Data != null && !p.Data.Disconnected) list.Add(p);
        return list;
    }

    public static void ClearOnLobbyJoin() => _assigned.Clear();

    // ---- Assignment hook ------------------------------------------------------------------------

    // Called from the AssignRolesFromList prefix. Pulls pre-chosen players out of this call's list,
    // forces their role, and lets vanilla assign whoever we leave behind. Each player appears in
    // exactly one AssignRolesFromList call, so there's no double assignment. Returns how many we
    // forced this call (for logging). The lists are the game's own Il2Cpp lists - removing from them
    // is what stops vanilla reassigning those players.
    public static int ApplyPreChosen(
        Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players,
        Il2CppSystem.Collections.Generic.List<RoleTypes> roleList,
        ref int rolesAssigned)
    {
        if (_assigned.Count == 0 || players == null) return 0;

        int applied = 0;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            var info = players[i];
            if (info == null) continue;
            if (!_assigned.TryGetValue(info.PlayerId, out var role)) continue;

            var pc = info.Object;
            if (pc == null) continue;

            // Spend one matching role from this list when present, so vanilla's head count stays right.
            if (roleList != null)
            {
                Il2CppSystem.Predicate<RoleTypes> isRole = (Il2CppSystem.Predicate<RoleTypes>)(r => r == role);
                int roleIndex = roleList.FindIndex(isRole);
                if (roleIndex != -1) roleList.RemoveAt(roleIndex);
            }

            players.RemoveAt(i);

            // canOverride MUST be false: that marks it an initial assignment so the game's
            // "all players assigned -> play the intro cutscene" check counts it. Passing true treats
            // it as a mid-game role swap, the intro never fires, and the lobby black-screens.
            try { pc.RpcSetRole(role, false); } catch { }

            rolesAssigned++;
            applied++;
        }

        return applied;
    }
}
