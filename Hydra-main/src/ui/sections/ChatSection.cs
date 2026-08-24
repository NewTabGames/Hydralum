using System;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui.sections
{
    internal class ChatSection : ISection
    {
        private static string _inputText = "";
        private static bool _isInputFocused = false;
        private static Vector2 _chatScroll = new Vector2(0, 10000f);
        private static float _blinkTimer = 0f;
        private static bool _showCursor = true;
        private static int _lastMessageCount = 0;

        public ChatSection() : base("Hydralum Chat") { }

        public override void Render()
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
            if (_isInputFocused)
            {
                string typed = Input.inputString;
                if (!string.IsNullOrEmpty(typed))
                {
                    foreach (char c in typed)
                    {
                        if (c == '\b') // Backspace
                        {
                            if (_inputText.Length > 0)
                            {
                                _inputText = _inputText.Substring(0, _inputText.Length - 1);
                            }
                        }
                        else if (c == '\n' || c == '\r') // Enter
                        {
                            if (!string.IsNullOrWhiteSpace(_inputText))
                            {
                                string toSend = _inputText;
                                _inputText = "";
                                _ = ChatManager.SendMessageAsync(toSend);
                            }
                        }
                        else if (!char.IsControl(c))
                        {
                            if (_inputText.Length < 140)
                            {
                                _inputText += c;
                            }
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!string.IsNullOrWhiteSpace(_inputText))
                    {
                        string toSend = _inputText;
                        _inputText = "";
                        _ = ChatManager.SendMessageAsync(toSend);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (_inputText.Length > 0 && string.IsNullOrEmpty(typed))
                    {
                        _inputText = _inputText.Substring(0, _inputText.Length - 1);
                    }
                }

                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
                {
                    string clip = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(clip))
                    {
                        _inputText += clip;
                        if (_inputText.Length > 140) _inputText = _inputText.Substring(0, 140);
                    }
                }
            }

            // Header info
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>Hydralum Chat</b>", GUILayout.ExpandWidth(true));
            int online = PresenceTracker.GetOnlineCount();
            GUILayout.Label($"<color=#00FF88>● {online} online</color>", GUILayout.Width(90));
            if (GUILayout.Button("Refresh", GUILayout.Width(65 * MainUI.scale), GUILayout.Height(20 * MainUI.scale)))
            {
                ChatManager.Refresh();
            }
            GUILayout.EndHorizontal();

            // Status message
            if (!string.IsNullOrEmpty(ChatManager.StatusMessage))
            {
                GUILayout.Label($"<color=#FFAA00>{ChatManager.StatusMessage}</color>");
            }

            GUILayout.Space(4);

            // Message list container
            var messages = ChatManager.GetMessages();
            if (messages.Count > _lastMessageCount)
            {
                _chatScroll.y = 100000f; // auto scroll to bottom on new messages
                _lastMessageCount = messages.Count;
            }

            _chatScroll = GUILayout.BeginScrollView(_chatScroll, false, true, GUILayout.Height(MainUI.FeaturePaneSize.y - 120 * MainUI.scale));

            if (messages.Count == 0)
            {
                GUILayout.Label("<color=#888888>No messages yet. Say hello to other Hydralum users!</color>");
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
                    GUILayout.Label($"<color=#888888>{msg.time}</color>", GUILayout.Width(80));
                    GUILayout.EndHorizontal();

                    GUIStyle wordWrapStyle = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true,
                        richText = true,
                        fontSize = (int)(11 * MainUI.scale)
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
                richText = true,
                fontSize = (int)(11 * MainUI.scale)
            };

            if (GUILayout.Button(displayText, inputStyle, GUILayout.Height(26 * MainUI.scale), GUILayout.ExpandWidth(true)))
            {
                _isInputFocused = true;
            }

            if (GUILayout.Button("Send", GUILayout.Width(60 * MainUI.scale), GUILayout.Height(26 * MainUI.scale)))
            {
                if (!string.IsNullOrWhiteSpace(_inputText))
                {
                    string toSend = _inputText;
                    _inputText = "";
                    _ = ChatManager.SendMessageAsync(toSend);
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50 * MainUI.scale), GUILayout.Height(26 * MainUI.scale)))
            {
                _inputText = "";
            }

            GUILayout.EndHorizontal();

            // Quick actions
            if (_isInputFocused)
            {
                if (GUILayout.Button("Unfocus Keyboard Input", GUILayout.Height(20 * MainUI.scale)))
                {
                    _isInputFocused = false;
                }
            }
        }
    }
}
