using UnityEngine;

namespace MalumMenu;

// Subwindow toggled by "Show Chat Log" in the Chat tab. Lets you turn recording on/off, choose which
// fields the log includes, preview captured messages, and export them to a .txt in BepInEx/config/TextLogs.
public class ChatLogUI : MonoBehaviour
{
    public static int windowHeight = 420;
    public static int windowWidth = 560;
    public static Rect windowRect;

    private Vector2 _scrollPosition = Vector2.zero;
    private string _statusMsg = "";
    private float _statusUntil;

    private void Start()
    {
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (!CheatToggles.showChatLog || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        var prevMatrix = GUI.matrix;
        float scale = CheatToggles.GetWindowScale("ChatLog");
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), windowRect.position);
        windowRect = GUI.Window((int)WindowId.ChatLogUI, windowRect, (GUI.WindowFunction)ChatLogWindow, "Chat Log");
        GUI.matrix = prevMatrix;
    }

    private void SetStatus(string msg)
    {
        _statusMsg = msg;
        _statusUntil = Time.unscaledTime + 4f;
    }

    private void ChatLogWindow(int windowID)
    {
        try
        {
            GUILayout.BeginVertical();

            // Recording control + message count
            GUILayout.BeginHorizontal();
            CheatToggles.recordChat = GUILayout.Toggle(CheatToggles.recordChat, " Record Chat");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<color=#888888>{ChatLogRecorder.Count} message(s)</color>");
            GUILayout.EndHorizontal();

            GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(1f), GUILayout.ExpandWidth(true));

            // Field toggles (customize what the log/export shows) — two columns
            GUILayout.Label("Include in log", GUIStylePreset.TabSubtitle);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.45f));
            CheatToggles.logIncludeTimestamp = GUILayout.Toggle(CheatToggles.logIncludeTimestamp, " Timestamp");
            CheatToggles.logIncludeColor = GUILayout.Toggle(CheatToggles.logIncludeColor, " Name Color");
            CheatToggles.logIncludeDeadTag = GUILayout.Toggle(CheatToggles.logIncludeDeadTag, " Dead Tag");
            CheatToggles.logIncludeRole = GUILayout.Toggle(CheatToggles.logIncludeRole, " Role");
            CheatToggles.logIncludeLevel = GUILayout.Toggle(CheatToggles.logIncludeLevel, " Level");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            CheatToggles.logIncludePlatform = GUILayout.Toggle(CheatToggles.logIncludePlatform, " Platform");
            CheatToggles.logIncludeFriendCode = GUILayout.Toggle(CheatToggles.logIncludeFriendCode, " Friend Code");
            CheatToggles.logIncludeTasks = GUILayout.Toggle(CheatToggles.logIncludeTasks, " Tasks (X/Y)");
            CheatToggles.logIncludeVotekick = GUILayout.Toggle(CheatToggles.logIncludeVotekick, " Votekick Count");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(1f), GUILayout.ExpandWidth(true));

            // Live preview of captured messages
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true, GUILayout.ExpandHeight(true));

            var entries = ChatLogRecorder.Entries;
            if (entries.Count == 0)
            {
                GUILayout.Label(CheatToggles.recordChat
                    ? "<color=#888888>Recording... messages will appear here.</color>"
                    : "<color=#888888>Enable Record Chat to start capturing messages.</color>");
            }
            else
            {
                // Show the most recent messages last (cap the drawn rows so the GUI stays responsive)
                int start = entries.Count > 300 ? entries.Count - 300 : 0;
                for (int i = start; i < entries.Count; i++)
                {
                    GUILayout.Label(ChatLogRecorder.FormatEntry(entries[i], true));
                }
            }

            GUILayout.EndScrollView();

            // Actions
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Export to .txt", GUIStylePreset.NormalButton))
            {
                var path = ChatLogRecorder.ExportToFile();
                SetStatus(path != null ? $"<color=#00FF00>Saved: {System.IO.Path.GetFileName(path)}</color>" : "<color=#FFAA00>Nothing to export</color>");
            }

            if (GUILayout.Button("Open Text Logs", GUIStylePreset.NormalButton))
            {
                ChatLogRecorder.OpenFolder();
            }

            if (GUILayout.Button("Clear Log", GUIStylePreset.NormalButton))
            {
                ChatLogRecorder.Clear();
                SetStatus("<color=#888888>Log cleared</color>");
            }

            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMsg) && Time.unscaledTime < _statusUntil)
            {
                GUILayout.Label(_statusMsg);
            }

            GUILayout.EndVertical();
        }
        catch { }

        GUI.DragWindow();
    }
}
