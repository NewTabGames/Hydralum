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
    }

    private void DrawTextbox()
    {
        GUILayout.Label("Textbox", GUIStylePreset.TabSubtitle);

        CheatToggles.unlockCharacters = GUILayout.Toggle(CheatToggles.unlockCharacters, " Unlock Extra Characters");

        CheatToggles.longerMessages = GUILayout.Toggle(CheatToggles.longerMessages, " Allow Longer Messages");

        CheatToggles.unlockClipboard = GUILayout.Toggle(CheatToggles.unlockClipboard, " Unlock Clipboard");

        CheatToggles.chatTimestamps = GUILayout.Toggle(CheatToggles.chatTimestamps, " Add Timestamps to Messages");

        GUILayout.Space(15);
        GUILayout.Label("Advanced Chat Tags", GUIStylePreset.TabSubtitle);
        CheatToggles.chatShowTasks = GUILayout.Toggle(CheatToggles.chatShowTasks, " Show Tasks (X/Y)");
        CheatToggles.chatShowVK = GUILayout.Toggle(CheatToggles.chatShowVK, " Show Votekick Counter");
        CheatToggles.chatShowFriendCode = GUILayout.Toggle(CheatToggles.chatShowFriendCode, " Show Friend Code");
    }
}
