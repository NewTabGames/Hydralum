using UnityEngine;

namespace MalumMenu;

public class DebugTab : ITab
{
    public string name => "Debug";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.EndVertical();
    }

    private void DrawGeneral()
    {
        CheatToggles.showDebugConsole = GUILayout.Toggle(CheatToggles.showDebugConsole, " Show Debug Console");

        CheatToggles.logIncomingRpcs = GUILayout.Toggle(CheatToggles.logIncomingRpcs, " Log Incoming RPCs");

        CheatToggles.logOutgoingRpcs = GUILayout.Toggle(CheatToggles.logOutgoingRpcs, " Log Outgoing RPCs");
    }
}
