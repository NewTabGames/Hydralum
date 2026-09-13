using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

public class LobbiesTab : ITab
{
    public string name => "Lobbies";

    private Vector2 _scroll = Vector2.zero;

    public void Draw()
    {
        // Snapshot so Clear (which changes the row count) can be deferred to the end of the pass, keeping
        // the IMGUI control count stable within the frame.
        var entries = new List<LobbyEntry>(LobbyHistory.Entries);
        bool doClear = false;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Lobby History ({entries.Count})", GUIStylePreset.TabSubtitle);
        GUILayout.FlexibleSpace();
        if (entries.Count > 0 && GUILayout.Button("Clear", GUIStylePreset.NormalButton, GUILayout.Width(90)))
            doClear = true;
        GUILayout.EndHorizontal();

        GUILayout.Label("<size=11><color=#888888>Lobbies you've joined – code, host, map, players, region. Copy a code or rejoin.</color></size>");
        GUILayout.Space(6);

        if (entries.Count == 0)
        {
            GUILayout.Label("<color=#888888>No lobbies recorded yet. Join an online game and it'll show up here.</color>");
            return;
        }

        _scroll = GUILayout.BeginScrollView(_scroll, false, true);
        foreach (var e in entries)
        {
            DrawRow(e);
            GUILayout.Space(4);
        }
        GUILayout.EndScrollView();

        if (doClear) LobbyHistory.Clear();
    }

    private static void DrawRow(LobbyEntry e)
    {
        bool current = e.GameId != 0 && e.GameId == LobbyHistory.CurrentGameId;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        string when = LobbyHistory.FormatWhen(e.WhenTicks);
        string codeLine = current
            ? $"<b><color=#00FF88>{e.Code}</color></b>  <size=11><color=#888888>{when} · current</color></size>"
            : $"<b>{e.Code}</b>  <size=11><color=#888888>{when}</color></size>";
        GUILayout.Label(codeLine);
        GUILayout.Label($"<size=11><color=#AAAAAA>{e.Host} · {e.Map} · {e.Players} ppl · {e.Region} · {(e.Public ? "public" : "private")}</color></size>");
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Copy", GUIStylePreset.NormalButton, GUILayout.Width(80)))
            LobbyHistory.CopyCode(e);
        if (GUILayout.Button("Join", GUIStylePreset.NormalButton, GUILayout.Width(80)))
            LobbyHistory.Rejoin(e);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
}
