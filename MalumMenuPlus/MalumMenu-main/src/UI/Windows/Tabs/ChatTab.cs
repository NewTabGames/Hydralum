using UnityEngine;

namespace MalumMenu;

public class ChatTab : ITab
{
    public string name => "Chat";

    // Chat Sender input state. GUILayout.TextField crashes under IL2CPP, so (as elsewhere) we capture typed
    // characters ourselves. The buffer is ChatSender.Message directly so the ticking spam sees live edits.
    private static bool _isTyping = false;

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawChatSender();

        GUILayout.Space(15);

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
            GUILayout.Label("<size=11><color=#888888>24hr / 12hr format is in Config → Account → Time Format</color></size>");

        GUILayout.Space(15);
        GUILayout.Label("Advanced Chat Tags", GUIStylePreset.TabSubtitle);
        CheatToggles.chatShowTasks = GUILayout.Toggle(CheatToggles.chatShowTasks, " Show Tasks (X/Y)");
        CheatToggles.chatShowVK = GUILayout.Toggle(CheatToggles.chatShowVK, " Show Votekick Counter");
        CheatToggles.chatShowFriendCode = GUILayout.Toggle(CheatToggles.chatShowFriendCode, " Show Friend Code");
    }

    private void DrawChatSender()
    {
        GUILayout.Label("Chat Sender", GUIStylePreset.TabSubtitle);

        if (_isTyping) CaptureChatInput();

        // Message field (click to type).
        string label = _isTyping
            ? $"<color=yellow>{(string.IsNullOrEmpty(ChatSender.Message) ? "Type a message..." : ChatSender.Message)}_</color>"
            : (string.IsNullOrEmpty(ChatSender.Message) ? "Message: <i>click to type</i>" : $"Message: <b>{ChatSender.Message}</b>");
        if (GUILayout.Button(label, GUIStylePreset.NormalButton, GUILayout.Height(26)))
            _isTyping = !_isTyping;

        // Interval is always visible so you can dial it in BEFORE (and while) spamming. The slider is
        // logarithmic: a linear one over the 0.1ms..60s range would bunch every sub-second value into a
        // sliver at the far left. Mapping the slider linearly in log-space gives even feel per order of
        // magnitude, so the low intervals are actually selectable.
        GUILayout.Label($"Interval: <b>{FormatInterval(ChatSender.Interval)}</b> <size=11><color=#888888>(min {FormatInterval(ChatSender.MinInterval)} / max {FormatInterval(ChatSender.MaxInterval)})</color></size>");
        float minL = Mathf.Log(ChatSender.MinInterval);
        float maxL = Mathf.Log(ChatSender.MaxInterval);
        float cur = Mathf.Clamp(ChatSender.Interval, ChatSender.MinInterval, ChatSender.MaxInterval);
        float t = (Mathf.Log(cur) - minL) / (maxL - minL);
        t = GUILayout.HorizontalSlider(t, 0f, 1f);
        ChatSender.Interval = Mathf.Exp(minL + t * (maxL - minL));

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Send Once", GUIStylePreset.NormalButton, GUILayout.Height(26)))
            ChatSender.SendOnce();

        var prev = GUI.backgroundColor;
        if (ChatSender.Spam) GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f); // red-ish while running
        if (GUILayout.Button(ChatSender.Spam ? "■ Stop Spam" : "▶ Start Spam", GUIStylePreset.NormalButton, GUILayout.Height(26)))
            ChatSender.Spam = !ChatSender.Spam;
        GUI.backgroundColor = prev;
        GUILayout.EndHorizontal();

        // Status line.
        if (ChatSender.Spam)
        {
            if (string.IsNullOrWhiteSpace(ChatSender.Message))
                GUILayout.Label("<color=#FF6B6B>Type a message to spam.</color>");
            else
                GUILayout.Label($"<color=#00d0ff>🔁 Spamming every {FormatInterval(ChatSender.Interval)}</color>");
        }
    }

    // Readable interval: seconds (one decimal) at/above 1s, milliseconds below it.
    private static string FormatInterval(float v)
    {
        if (v >= 1f) return $"{v:0.#}s";
        return $"{v * 1000f:0.#}ms";
    }

    // Manual key capture (IL2CPP-safe): appends printable chars to ChatSender.Message. Enter/Escape stop
    // editing (Enter does not auto-send - use the Send Chat button), Backspace deletes.
    private void CaptureChatInput()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
        {
            _isTyping = false;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            if (!string.IsNullOrEmpty(ChatSender.Message))
                ChatSender.Message = ChatSender.Message.Substring(0, ChatSender.Message.Length - 1);
            e.Use();
            return;
        }

        char c = e.character;
        if (c != '\0' && !char.IsControl(c) && ChatSender.Message.Length < ChatSender.MaxLength)
        {
            ChatSender.Message += c;
            e.Use();
        }
    }
}
