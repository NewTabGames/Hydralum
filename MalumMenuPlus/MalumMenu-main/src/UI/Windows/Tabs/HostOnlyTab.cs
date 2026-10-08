using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

namespace MalumMenu;

public class HostOnlyTab : ITab
{
    public string name => "Host";

    public void Draw()
    {
        var players = MalumRoleAssign.CollectPlayers();

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawMurder();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        DrawGameState();

        GUILayout.Space(15);

        DrawMeetings();

        GUILayout.Space(15);

        // Role Assign controls sit in the right column, under Meetings.
        DrawRoleAssignControls(players);

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // The player list spans the full width below both columns (the menu wraps tab content in its
        // own scroll view, so it can grow without clipping).
        GUILayout.Space(15);

        DrawRoleAssignPlayerList(players);
    }

    private void DrawGeneral()
    {
        GUILayout.Label("General", GUIStylePreset.TabSubtitle);

        CheatToggles.killVanished = GUILayout.Toggle(CheatToggles.killVanished, " Kill While Vanished");

        CheatToggles.killAnyone = GUILayout.Toggle(CheatToggles.killAnyone, " Kill Anyone");

        CheatToggles.noKillCd = GUILayout.Toggle(CheatToggles.noKillCd, " No Kill Cooldown");

        CheatToggles.banMidGame = GUILayout.Toggle(CheatToggles.banMidGame, " Ban Mid-Game");

        CheatToggles.disableCloseDoors = GUILayout.Toggle(CheatToggles.disableCloseDoors, " Disable Close Doors");

        CheatToggles.disableSecurityCameras = GUILayout.Toggle(CheatToggles.disableSecurityCameras, " Disable Security Cameras");

        CheatToggles.showProtectMenu = GUILayout.Toggle(CheatToggles.showProtectMenu, " Show Protect Menu");

        CheatToggles.showRolesMenu = GUILayout.Toggle(CheatToggles.showRolesMenu, " Show Roles Menu");

        CheatToggles.assignRolesNextRound = GUILayout.Toggle(CheatToggles.assignRolesNextRound, " Assign Roles Next Round");

        CheatToggles.noOptionsLimits = GUILayout.Toggle(CheatToggles.noOptionsLimits, " No Options Limits");

        CheatToggles.discoParty = GUILayout.Toggle(CheatToggles.discoParty, " Disco Party");
    }

    private void DrawMurder()
    {
        GUILayout.Label("Murder", GUIStylePreset.TabSubtitle);

        CheatToggles.killPlayer = GUILayout.Toggle(CheatToggles.killPlayer, " Kill Player");

        CheatToggles.telekillPlayer = GUILayout.Toggle(CheatToggles.telekillPlayer, " Telekill Player");

        CheatToggles.killAllCrew = GUILayout.Toggle(CheatToggles.killAllCrew, " Kill All Crewmates");

        CheatToggles.killAllImps = GUILayout.Toggle(CheatToggles.killAllImps, " Kill All Impostors");

        CheatToggles.killAll = GUILayout.Toggle(CheatToggles.killAll, " Kill Everyone");
    }

    private void DrawGameState()
    {
        GUILayout.Label("Game State", GUIStylePreset.TabSubtitle);

        CheatToggles.forceStartGame = GUILayout.Toggle(CheatToggles.forceStartGame, " Force Start Game");

        CheatToggles.noGameEnd = GUILayout.Toggle(CheatToggles.noGameEnd, " No Game End");
    }

    private void DrawMeetings()
    {
        GUILayout.Label("Meetings", GUIStylePreset.TabSubtitle);

        CheatToggles.disableMeetings = GUILayout.Toggle(CheatToggles.disableMeetings, " Disable Meetings");

        CheatToggles.spamReportBodies = GUILayout.Toggle(CheatToggles.spamReportBodies, " Spam Report Bodies");

        CheatToggles.skipMeeting = GUILayout.Toggle(CheatToggles.skipMeeting, " Skip Meeting");

        CheatToggles.voteImmune = GUILayout.Toggle(CheatToggles.voteImmune, " Vote Immune");

        CheatToggles.ejectPlayer = GUILayout.Toggle(CheatToggles.ejectPlayer, " Eject Player");
    }

    // ---- Role Assign ------------------------------------------------------------------------------
    // Pick each player's role before the match; everyone left on Random is filled in normally. The
    // assignment runs in RoleManager.SelectRoles as host (see MalumRoleAssign / RoleAssignPatches).

    private void DrawRoleAssignControls(List<PlayerControl> players)
    {
        GUILayout.Label("Role Assign", GUIStylePreset.TabSubtitle);

        CheatToggles.roleAssignerEnabled = GUILayout.Toggle(CheatToggles.roleAssignerEnabled, " Enable Role Assigner");

        if (!Utils.isHost && !Utils.isFreePlay)
            GUILayout.Label("<color=#ffcc55>You must be the host for this to take effect.</color>", GUIStylePreset.Hint);

        GUILayout.Space(6f);

        DrawRoleAssignSummary(players);

        GUILayout.Space(4f);

        if (GUILayout.Button("Clear All (Random)", GUIStylePreset.NormalButton, GUILayout.Width(160f), GUILayout.Height(24f)))
            MalumRoleAssign.ClearAll();
    }

    private void DrawRoleAssignSummary(List<PlayerControl> players)
    {
        int maxImps = MalumRoleAssign.GetMaxImpostorAmount(players.Count);
        int chosenImps = MalumRoleAssign.CountChosenImpostors();
        int chosen = MalumRoleAssign.CountChosen();

        GUILayout.Label($"<color=#888888>Players: {players.Count}   Assigned: {chosen}</color>");

        string impColor = chosenImps > maxImps ? "#ff6666" : "#88ff88";
        GUILayout.Label($"Chosen impostors: <color={impColor}>{chosenImps}</color> / max {maxImps}");
        if (chosenImps > maxImps)
            GUILayout.Label("<color=#ffcc55>More impostors chosen than the cap - they'll all still be forced.</color>", GUIStylePreset.Hint);
    }

    private void DrawRoleAssignPlayerList(List<PlayerControl> players)
    {
        if (players.Count == 0)
        {
            GUILayout.Label("Join a lobby to assign roles.");
            return;
        }

        int hostId = AmongUsClient.Instance != null ? AmongUsClient.Instance.HostId : -1;

        foreach (var player in players)
        {
            var data = player.Data;
            if (data == null) continue;

            byte playerId = data.PlayerId;
            var chosen = MalumRoleAssign.GetChosen(playerId);

            var colorHex = ColorUtility.ToHtmlStringRGB(data.Color);
            string hostTag = player.OwnerId == hostId ? " <color=#ffcc00>(Host)</color>" : "";
            string youTag = player.AmOwner ? " <color=#00d0ff>(You)</color>" : "";

            GUILayout.BeginHorizontal();

            GUILayout.Label($"<color=#{colorHex}>{data.PlayerName}</color>{youTag}{hostTag}",
                GUILayout.Width(MenuUI.windowWidth * 0.40f));

            if (GUILayout.Button("<", GUIStylePreset.NormalButton, GUILayout.Width(28f), GUILayout.Height(24f)))
                MalumRoleAssign.CycleRole(playerId, -1);

            GUILayout.Label(RoleLabel(chosen), GUIStylePreset.Hint, GUILayout.Width(130f));

            if (GUILayout.Button(">", GUIStylePreset.NormalButton, GUILayout.Width(28f), GUILayout.Height(24f)))
                MalumRoleAssign.CycleRole(playerId, 1);

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }
    }

    // Colored label for the chosen role: red for impostor-side, cyan for special crew, gray for
    // plain Crewmate / Random.
    private static string RoleLabel(RoleTypes? chosen)
    {
        if (chosen == null) return "<color=#888888><b>Random</b></color>";

        var role = chosen.Value;
        string name = Utils.GetRoleDisplayName(role);

        bool impostor = role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter
            || role == RoleTypes.Phantom || role == RoleTypes.Viper;

        string color = impostor ? "#ff6666" : (role == RoleTypes.Crewmate ? "#dddddd" : "#55ddff");
        return $"<color={color}><b>{name}</b></color>";
    }
}
