using UnityEngine;

namespace MalumMenu;

public class ConsoleTab : ITab
{
    public string name => "Console";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        // Left column: console toggle + per-player event loggers.
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(12);

        DrawPlayers();

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Right column: meeting + game loggers and the RPC console.
        GUILayout.BeginVertical();

        DrawMeetings();

        GUILayout.Space(12);

        DrawGame();

        GUILayout.Space(12);

        DrawRpcConsole();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawGeneral()
    {
        GUILayout.Label("Console", GUIStylePreset.TabSubtitle);

        CheatToggles.showConsole = GUILayout.Toggle(CheatToggles.showConsole, " Show Console");
    }

    private void DrawPlayers()
    {
        GUILayout.Label("Players", GUIStylePreset.TabSubtitle);

        CheatToggles.logDeaths = GUILayout.Toggle(CheatToggles.logDeaths, " Log Deaths");

        CheatToggles.logShapeshifts = GUILayout.Toggle(CheatToggles.logShapeshifts, " Log Shapeshifts");

        CheatToggles.logVents = GUILayout.Toggle(CheatToggles.logVents, " Log Vents");

        CheatToggles.logTasks = GUILayout.Toggle(CheatToggles.logTasks, " Log Tasks");

        CheatToggles.logJoins = GUILayout.Toggle(CheatToggles.logJoins, " Log Joins");

        CheatToggles.logDisconnects = GUILayout.Toggle(CheatToggles.logDisconnects, " Log Disconnects");

        CheatToggles.logGuardianProtect = GUILayout.Toggle(CheatToggles.logGuardianProtect, " Log Guardian Protect");
    }

    private void DrawMeetings()
    {
        GUILayout.Label("Meetings", GUIStylePreset.TabSubtitle);

        CheatToggles.logMeetings = GUILayout.Toggle(CheatToggles.logMeetings, " Log Meetings");

        CheatToggles.logVotes = GUILayout.Toggle(CheatToggles.logVotes, " Log Votes");

        CheatToggles.logVotekicks = GUILayout.Toggle(CheatToggles.logVotekicks, " Log Votekicks");

        CheatToggles.logVerdict = GUILayout.Toggle(CheatToggles.logVerdict, " Log Judge Verdict");
    }

    private void DrawGame()
    {
        GUILayout.Label("Game", GUIStylePreset.TabSubtitle);

        CheatToggles.logGameState = GUILayout.Toggle(CheatToggles.logGameState, " Log Game State");

        CheatToggles.logSabotages = GUILayout.Toggle(CheatToggles.logSabotages, " Log Sabotages");

        CheatToggles.logChat = GUILayout.Toggle(CheatToggles.logChat, " Log Chat");
    }

    private void DrawRpcConsole()
    {
        GUILayout.Label("RPC Console", GUIStylePreset.TabSubtitle);

        CheatToggles.showDebugConsole = GUILayout.Toggle(CheatToggles.showDebugConsole, " Show RPC Console");

        CheatToggles.logIncomingRpcs = GUILayout.Toggle(CheatToggles.logIncomingRpcs, " Log Incoming RPCs");

        CheatToggles.logOutgoingRpcs = GUILayout.Toggle(CheatToggles.logOutgoingRpcs, " Log Outgoing RPCs");
    }
}
