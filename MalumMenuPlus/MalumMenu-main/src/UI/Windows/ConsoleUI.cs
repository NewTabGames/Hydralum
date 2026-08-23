using Il2CppSystem;
using UnityEngine;
using System.Collections.Generic;

namespace MalumMenu;

public class ConsoleUI : MonoBehaviour
{
    public static int windowHeight = 350;
    public static int windowWidth = 550;
    public static Rect windowRect;

    private GUIStyle _logStyle;
    private static Vector2 _scrollPosition = Vector2.zero;
    private static List<string> _logEntries = new();
    private const int MaxLogEntries = 300;

    private void Start()
    {
        // Instantiate 2D area of ConsoleUI
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        if (!CheatToggles.showConsole || !(MenuUI.isGUIActive || MalumMenu.menuKeepSubwindowsOpen.Value) || MalumMenu.isPanicked) return;

        _logStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 16
        };

        UIHelpers.ApplyUIColor();

        windowRect = GUI.Window((int)WindowId.ConsoleUI, windowRect, (GUI.WindowFunction)ConsoleWindow, "Console");
    }

    private void ConsoleWindow(int windowID)
    {
        try
        {
            GUILayout.BeginVertical(GUI.skin.box);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false);

            var logs = _logEntries.ToArray();
            foreach (var log in logs)
            {
                GUILayout.Label(log, _logStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Log", GUILayout.Width(260)))
            {
                _logEntries.Clear();
            }

            if (GUILayout.Button("Copy Log to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = String.Join("\n", _logEntries.ToArray());
            }

            GUILayout.EndHorizontal();
        }
        catch { }

        GUI.DragWindow();
    }

    public static void Log(string message)
    {
        // Timestamp every entry so the console reads like a replay timeline
        var entry = $"<color=#8A8A8A>[{System.DateTime.Now:h:mm:ss tt}]</color> {message}";

        if (_logEntries.Count >= MaxLogEntries) // Limit the number of logs to keep memory usage in check
        {
            _logEntries.RemoveAt(0); // Remove the oldest log entry
        }

        _logEntries.Add(entry);

        // Scroll to the bottom
        _scrollPosition.y = float.MaxValue;
    }
}
