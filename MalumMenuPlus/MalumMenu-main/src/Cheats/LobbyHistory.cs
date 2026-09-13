using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

public sealed class LobbyEntry
{
    public int GameId;
    public string Code = "?";
    public string Host = "?";
    public string Map = "?";
    public int Players;
    public string Region = "?";
    public bool Public;
    public long WhenTicks; // raw join time; formatted at display so it follows the 24hr/12hr setting live
}

// Records the online lobbies you join (code, host, map, peak players, region, privacy, time) so you can
// copy a code or rejoin later. Persisted to disk. Deduped by code - rejoining a lobby moves it to the top
// and refreshes its details instead of piling up duplicate rows.
public static class LobbyHistory
{
    private const int Max = 40;

    private static readonly List<LobbyEntry> _entries = new();
    private static int _cur;
    private static float _next;
    private static bool _loaded;
    private static bool _dirty;

    private static string FilePath => Path.Combine(Paths.ConfigPath, "MalumLobbyHistory.txt");

    // The GameId of the lobby we're currently in (0 when not in an online game). Used to badge the row.
    public static int CurrentGameId => _cur;

    public static IReadOnlyList<LobbyEntry> Entries { get { Load(); return _entries; } }

    public static void Clear()
    {
        Load();
        _entries.Clear();
        _cur = 0;
        Save();
    }

    // Polled every frame from AmongUsClient.Update (self-throttled). Adds a row when you join a new online
    // lobby and keeps the newest row's details fresh while you're in it.
    public static void Tick()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 1.5f;

        var au = AmongUsClient.Instance;
        if (au == null) return;

        int gid = au.GameId;
        if (gid == 0 || au.NetworkMode != NetworkModes.OnlineGame ||
            (LobbyBehaviour.Instance == null && ShipStatus.Instance == null))
        {
            _cur = 0;
            return;
        }

        Load();

        if (gid != _cur)
        {
            _cur = gid;
            AddOrPromote(gid);
        }

        if (_entries.Count > 0 && _entries[0].GameId == gid)
            Fill(_entries[0], au);

        if (_dirty)
        {
            Save();
            _dirty = false;
        }
    }

    private static void AddOrPromote(int gid)
    {
        string code = "?";
        try { code = GameCode.IntToGameName(gid); } catch { }
        long ticks = DateTime.Now.Ticks;

        // Dedupe by lobby: if we've seen this one, move it to the top and refresh its time.
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].GameId != gid) continue;
            var existing = _entries[i];
            existing.WhenTicks = ticks;
            _entries.RemoveAt(i);
            _entries.Insert(0, existing);
            _dirty = true;
            return;
        }

        _entries.Insert(0, new LobbyEntry { GameId = gid, Code = code, WhenTicks = ticks });
        while (_entries.Count > Max) _entries.RemoveAt(_entries.Count - 1);
        _dirty = true;
    }

    private static void Fill(LobbyEntry e, AmongUsClient au)
    {
        try
        {
            if (au.allClients != null && au.allClients.Count != e.Players)
            {
                e.Players = au.allClients.Count; // live count - updates as players join/leave
                _dirty = true;
            }
        }
        catch { }

        try { if (e.Public != au.IsGamePublic) { e.Public = au.IsGamePublic; _dirty = true; } } catch { }

        try
        {
            if (ServerManager.Instance != null && ServerManager.Instance.CurrentRegion != null)
            {
                string region = ServerManager.Instance.CurrentRegion.Name;
                if (!string.IsNullOrEmpty(region) && region != e.Region) { e.Region = region; _dirty = true; }
            }
        }
        catch { }

        try
        {
            byte mapId = Utils.GetCurrentMapID();
            if (mapId != byte.MaxValue)
            {
                string map = MapDisplayName(mapId);
                if (map != e.Map) { e.Map = map; _dirty = true; }
            }
        }
        catch { }

        try
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null || pc.OwnerId != au.HostId) continue;
                string nm = Regex.Replace(pc.Data.PlayerName ?? "", "<.*?>", "").Replace("|", "").Trim();
                if (nm.Length > 0 && nm != e.Host) { e.Host = nm; _dirty = true; }
                break;
            }
        }
        catch { }
    }

    private static string MapDisplayName(byte mapId)
    {
        return (MapNames)mapId switch
        {
            MapNames.Skeld => "The Skeld",
            MapNames.MiraHQ => "MIRA HQ",
            MapNames.Polus => "Polus",
            MapNames.Dleks => "dlekS ehT",
            MapNames.Airship => "The Airship",
            MapNames.Fungle => "The Fungle",
            _ => ((MapNames)mapId).ToString(),
        };
    }

    // Formats a stored join time using the current 24hr / 12hr setting (Config -> Account -> Time Format).
    public static string FormatWhen(long ticks)
    {
        if (ticks <= 0) return "";
        try { return new DateTime(ticks).ToString(CheatToggles.chatTimestamp24hr ? "dd.MM HH:mm" : "dd.MM h:mm tt"); }
        catch { return ""; }
    }

    public static void CopyCode(LobbyEntry e)
    {
        if (e == null) return;
        try { GUIUtility.systemCopyBuffer = e.Code; } catch { }
    }

    public static void Rejoin(LobbyEntry e)
    {
        if (e == null || e.GameId == 0) return;
        try
        {
            var au = AmongUsClient.Instance;
            if (au == null) return;
            au.GameId = e.GameId;
            var routine = au.CoJoinOnlineGameFromCode(e.GameId);
            if (routine != null) au.StartCoroutine(routine);
        }
        catch { }
    }

    private static void Load()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var p = line.Split('|');
                if (p.Length < 8) continue;
                int.TryParse(p[0], out int id);
                long.TryParse(p[2], out long ticks);
                int.TryParse(p[5], out int players);
                _entries.Add(new LobbyEntry
                {
                    GameId = id, Code = p[1], WhenTicks = ticks, Host = p[3], Map = p[4],
                    Players = players, Region = p[6], Public = p[7] == "1",
                });
                if (_entries.Count >= Max) break;
            }
        }
        catch { }
    }

    private static void Save()
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var e in _entries)
                sb.Append(e.GameId).Append('|').Append(e.Code).Append('|').Append(e.WhenTicks).Append('|')
                  .Append(e.Host).Append('|').Append(e.Map).Append('|').Append(e.Players).Append('|')
                  .Append(e.Region).Append('|').Append(e.Public ? '1' : '0').Append('\n');
            File.WriteAllText(FilePath, sb.ToString());
        }
        catch { }
    }
}
