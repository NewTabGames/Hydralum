using System;
using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

public class GlobalChatTab : ITab
{
    public string name => "Hydralum Chat";

    private static string _inputText = "";
    private static bool _isInputFocused = false;
    private static Vector2 _chatScroll = new Vector2(0, 10000f);
    private static float _blinkTimer = 0f;
    private static bool _showCursor = true;
    private static int _lastMessageCount = 0;

    public void Draw()
    {
        ChatManager.MarkAsRead();

        // Cursor blink timer
        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= 0.5f)
        {
            _blinkTimer = 0f;
            _showCursor = !_showCursor;
        }

        // Keyboard input processing for IL2CPP safety
        if (Event.current != null && Event.current.type == EventType.KeyDown && _isInputFocused)
        {
            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
            {
                if (!string.IsNullOrWhiteSpace(_inputText))
                {
                    string toSend = _inputText;
                    _inputText = "";
                    _ = ChatManager.SendMessageAsync(toSend);
                }
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Backspace)
            {
                if (_inputText.Length > 0)
                {
                    _inputText = _inputText.Substring(0, _inputText.Length - 1);
                }
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.V && (Event.current.control || Event.current.command || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                string clip = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clip))
                {
                    _inputText += clip;
                    if (_inputText.Length > 140) _inputText = _inputText.Substring(0, 140);
                }
                Event.current.Use();
            }
            else if (Event.current.character != '\0' && !char.IsControl(Event.current.character))
            {
                if (_inputText.Length < 140)
                {
                    _inputText += Event.current.character;
                }
                Event.current.Use();
            }
        }

        // Header info
        GUILayout.BeginHorizontal();
        int online = PresenceTracker.GetOnlineCount();
        GUILayout.Label($"<color=#00FF88>● {online} online</color>", GUIStylePreset.Hint, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(65), GUILayout.Height(20)))
        {
            ChatManager.Refresh();
        }
        GUILayout.EndHorizontal();

        // Status message
        if (!string.IsNullOrEmpty(ChatManager.StatusMessage))
        {
            GUILayout.Label($"<color=#FFAA00>{ChatManager.StatusMessage}</color>", GUIStylePreset.Hint);
        }

        GUILayout.Space(4);

        // Message list container
        var messages = ChatManager.GetMessages();
        if (messages.Count > _lastMessageCount)
        {
            _chatScroll.y = 100000f; // auto scroll to bottom on new messages
            _lastMessageCount = messages.Count;
        }

        _chatScroll = GUILayout.BeginScrollView(_chatScroll, false, true, GUILayout.Height(MenuUI.windowHeight * 0.58f));

        if (messages.Count == 0)
        {
            GUILayout.Label("<color=#888888>No messages yet. Say hello to other Hydralum users!</color>", GUIStylePreset.Hint);
        }
        else
        {
            foreach (var msg in messages)
            {
                if (msg == null) continue;

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                
                string senderColor = msg.name == PresenceTracker.LocalPlayerName ? "#00FF88" : "#00D8FF";
                GUILayout.Label($"<b><color={senderColor}>{msg.name}</color></b>", GUILayout.ExpandWidth(true));
                GUILayout.Label($"<color=#888888>{msg.time}</color>", GUIStylePreset.Hint, GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUIStyle wordWrapStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    richText = true
                };
                GUILayout.Label($"<color=#EEEEEE>{msg.text}</color>", wordWrapStyle);
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6);

        // Input field box (clickable focus)
        GUILayout.BeginHorizontal();
        string displayText = _inputText;
        if (string.IsNullOrEmpty(displayText) && !_isInputFocused)
        {
            displayText = "<color=#777777>Click here to type message (max 140 chars)...</color>";
        }
        else if (_isInputFocused && _showCursor)
        {
            displayText += "|";
        }

        GUIStyle inputStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };

        if (GUILayout.Button(displayText, inputStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
        {
            _isInputFocused = true;
        }

        if (GUILayout.Button("Send", GUIStylePreset.NormalButton, GUILayout.Width(60), GUILayout.Height(26)))
        {
            if (!string.IsNullOrWhiteSpace(_inputText))
            {
                string toSend = _inputText;
                _inputText = "";
                _ = ChatManager.SendMessageAsync(toSend);
            }
        }

        if (GUILayout.Button("Clear", GUIStylePreset.NormalButton, GUILayout.Width(50), GUILayout.Height(26)))
        {
            _inputText = "";
        }

        GUILayout.EndHorizontal();

        // Quick actions
        if (_isInputFocused)
        {
            if (GUILayout.Button("Unfocus Keyboard Input", GUIStylePreset.NormalButton, GUILayout.Height(20)))
            {
                _isInputFocused = false;
            }
        }
    }
}
