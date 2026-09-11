using UnityEngine;

namespace MalumMenu;

public class ConsoleTab : ITab
{
    public string name => "Console";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawRpcConsole();

        GUILayout.EndVertical();
    }

    private void DrawGeneral()
    {
        GUILayout.Label("Console", GUIStylePreset.TabSubtitle);

        CheatToggles.showConsole = GUILayout.Toggle(CheatToggles.showConsole, " Show Console");

        CheatToggles.logDeaths = GUILayout.Toggle(CheatToggles.logDeaths, " Log Deaths");

        CheatToggles.logShapeshifts = GUILayout.Toggle(CheatToggles.logShapeshifts, " Log Shapeshifts");

        CheatToggles.logVents = GUILayout.Toggle(CheatToggles.logVents, " Log Vents");

        CheatToggles.logMeetings = GUILayout.Toggle(CheatToggles.logMeetings, " Log Meetings");
    }

    private void DrawRpcConsole()
    {
        GUILayout.Label("RPC Console", GUIStylePreset.TabSubtitle);

        CheatToggles.showDebugConsole = GUILayout.Toggle(CheatToggles.showDebugConsole, " Show RPC Console");

        CheatToggles.logIncomingRpcs = GUILayout.Toggle(CheatToggles.logIncomingRpcs, " Log Incoming RPCs");

        CheatToggles.logOutgoingRpcs = GUILayout.Toggle(CheatToggles.logOutgoingRpcs, " Log Outgoing RPCs");
    }
}
