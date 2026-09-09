using UnityEngine;

namespace MalumMenu;

public class ChatTab : ITab
{
    public string name => "Chat";

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawTextbox();

        GUILayout.EndVertical();
    }

    private void DrawGeneral()
    {
        CheatToggles.enableChat = GUILayout.Toggle(CheatToggles.enableChat, " Enable Chat");

        CheatToggles.bypassUrlBlock = GUILayout.Toggle(CheatToggles.bypassUrlBlock, " Bypass URL Block");

        CheatToggles.lowerRateLimits = GUILayout.Toggle(CheatToggles.lowerRateLimits, " Lower Rate Limits");

        CheatToggles.chatDarkMode = GUILayout.Toggle(CheatToggles.chatDarkMode, " Dark Mode Chat");

        CheatToggles.showChatLog = GUILayout.Toggle(CheatToggles.showChatLog, " Show Chat Log");
    }

    private void DrawTextbox()
    {
        GUILayout.Label("Textbox", GUIStylePreset.TabSubtitle);

        CheatToggles.unlockCharacters = GUILayout.Toggle(CheatToggles.unlockCharacters, " Unlock Extra Characters");

        CheatToggles.unlockClipboard = GUILayout.Toggle(CheatToggles.unlockClipboard, " Unlock Clipboard");

        CheatToggles.chatTimestamps = GUILayout.Toggle(CheatToggles.chatTimestamps, " Add Timestamps to Messages");

        if (CheatToggles.chatTimestamps)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            var prevTsBg = GUI.backgroundColor;
            GUI.backgroundColor = CheatToggles.chatTimestamp24hr ? new Color(0.2f, 0.85f, 0.5f) : prevTsBg;
            if (GUILayout.Button("24hr", GUIStylePreset.NormalButton)) CheatToggles.chatTimestamp24hr = true;

            GUI.backgroundColor = !CheatToggles.chatTimestamp24hr ? new Color(0.2f, 0.85f, 0.5f) : prevTsBg;
            if (GUILayout.Button("12hr", GUIStylePreset.NormalButton)) CheatToggles.chatTimestamp24hr = false;

            GUI.backgroundColor = prevTsBg;
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(15);
        GUILayout.Label("Advanced Chat Tags", GUIStylePreset.TabSubtitle);
        CheatToggles.chatShowTasks = GUILayout.Toggle(CheatToggles.chatShowTasks, " Show Tasks (X/Y)");
        CheatToggles.chatShowVK = GUILayout.Toggle(CheatToggles.chatShowVK, " Show Votekick Counter");
        CheatToggles.chatShowFriendCode = GUILayout.Toggle(CheatToggles.chatShowFriendCode, " Show Friend Code");
    }
}
