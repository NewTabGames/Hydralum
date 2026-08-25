using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

public class DebugUI : MonoBehaviour
{
    public static int windowHeight = 350;
    public static int windowWidth = 550;
    private Rect _windowRect;

    private GUIStyle _logStyle;
    private static Vector2 _scrollPosition = Vector2.zero;
    private static readonly List<string> _logEntries = new();
    private const int MaxLogEntries = 500;

    private static readonly object _logLock = new();

    private void Start()
    {
        // Instantiate 2D area of DebugUI on the left side of the screen
        _windowRect = new(
            30f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (!CheatToggles.showDebugConsole || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        _logStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            richText = true
        };

        UIHelpers.ApplyUIColor();

        _windowRect = GUI.Window((int)WindowId.DebugUI, _windowRect, (GUI.WindowFunction)DebugWindow, "RPC Console");
    }

    private void DebugWindow(int windowID)
    {
        try
        {
            GUILayout.BeginVertical(GUI.skin.box);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false);

            string[] logs;
            lock (_logLock)
            {
                logs = _logEntries.ToArray();
            }

            foreach (var log in logs)
            {
                GUILayout.Label(log, _logStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Log", GUILayout.Width(260)))
            {
                lock (_logLock)
                {
                    _logEntries.Clear();
                }
            }

            if (GUILayout.Button("Copy Log to Clipboard"))
            {
                lock (_logLock)
                {
                    GUIUtility.systemCopyBuffer = string.Join("\n", _logEntries.ToArray());
                }
            }

            GUILayout.EndHorizontal();
        }
        catch { }

        GUI.DragWindow();
    }

    public static void Log(string message)
    {
        lock (_logLock)
        {
            if (_logEntries.Count >= MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }

            _logEntries.Add(message);
        }

        // Scroll to the bottom
        _scrollPosition.y = float.MaxValue;
    }
}
