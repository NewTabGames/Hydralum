using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;

namespace MalumMenu;

// Records chat messages (when Record Chat is on) so they can be reviewed in the Chat Log window and
// exported to a .txt file under BepInEx/config/TextLogs. Each field shown/exported is toggleable, so
// the log can be customized the same way the chat ESP tags are.
public static class ChatLogRecorder
{
    public class Entry
    {
        public DateTime Time;
        public string Name = "";
        public string ColorHex = "ffffff";
        public string RoleName = "";
        public string RoleColorHex = "ffffff";
        public int Level;
        public string Platform = "Unknown";
        public string FriendCode = "";
        public int TasksComplete;
        public int TasksTotal;
        public bool HasTasks;
        public int Votekick;
        public bool IsDead;
        public string Text = "";
    }

    // Keep at most this many messages in memory so a very long session can't grow without bound.
    private const int MaxEntries = 5000;

    private static readonly List<Entry> _entries = new();

    public static int Count => _entries.Count;
    public static IReadOnlyList<Entry> Entries => _entries;

    public static string TextLogsDir => Path.Combine(Paths.ConfigPath, "TextLogs");

    public static void EnsureFolder()
    {
        try { if (!Directory.Exists(TextLogsDir)) Directory.CreateDirectory(TextLogsDir); } catch { }
    }

    private static string StripTags(string s) => string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, "<.*?>", string.Empty);

    // Called from ChatController.AddChat for every message shown. Snapshots the sender's info at send
    // time so the exported log stays accurate even if the player later dies/leaves/changes role.
    public static void Capture(PlayerControl sourcePlayer, string rawText)
    {
        if (!CheatToggles.recordChat) return;

        try
        {
            var data = sourcePlayer?.Data;
            if (data == null) return;

            var e = new Entry
            {
                Time = DateTime.Now,
                Name = StripTags(data.PlayerName ?? ""),
                ColorHex = ColorUtility.ToHtmlStringRGB(data.Color),
                Level = (int)(data.PlayerLevel + 1),
                FriendCode = data.FriendCode ?? "",
                IsDead = data.IsDead || data.Disconnected,
                Text = StripTags(rawText ?? "")
            };

            try
            {
                if (data.Role != null)
                {
                    e.RoleName = Utils.GetRoleName(data);
                    e.RoleColorHex = ColorUtility.ToHtmlStringRGB(data.Role.TeamColor);
                }
            }
            catch { }

            try
            {
                if (Utils.isLocalGame)
                {
                    e.Platform = "Local";
                }
                else
                {
                    var client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromPlayerInfo(data) : null;
                    e.Platform = client != null ? Utils.PlatformTypeToString(client.PlatformData.Platform) : "Unknown";
                }
            }
            catch { e.Platform = "Unknown"; }

            try
            {
                var tasks = data.Tasks;
                if (tasks != null)
                {
                    e.HasTasks = true;
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        e.TasksTotal++;
                        if (tasks[i] != null && tasks[i].Complete) e.TasksComplete++;
                    }
                }
            }
            catch { }

            try
            {
                if (VoteBanSystem.Instance != null && VoteBanSystem.Instance.Votes != null
                    && VoteBanSystem.Instance.Votes.ContainsKey(data.ClientId))
                {
                    e.Votekick = VoteBanSystem.Instance.Votes[data.ClientId].Count;
                }
            }
            catch { }

            _entries.Add(e);

            if (_entries.Count > MaxEntries) _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }
        catch { }
    }

    public static void Clear() => _entries.Clear();

    // Builds one line for an entry, honoring the "include" toggles. richText=true colours the name/role
    // for the on-screen preview; false produces plain text for the exported .txt file.
    public static string FormatEntry(Entry e, bool richText)
    {
        if (e == null) return "";

        var sb = new StringBuilder();

        if (CheatToggles.logIncludeTimestamp)
            sb.Append('[').Append(e.Time.ToString(CheatToggles.chatTimestamp24hr ? "HH:mm:ss" : "h:mm:ss tt")).Append("] ");

        if (CheatToggles.logIncludeDeadTag && e.IsDead)
            sb.Append(richText ? "<color=#ff5555>[DEAD]</color> " : "[DEAD] ");

        if (richText && CheatToggles.logIncludeColor)
            sb.Append($"<color=#{e.ColorHex}>{e.Name}</color>");
        else
            sb.Append(e.Name);

        if (CheatToggles.logIncludeRole && !string.IsNullOrEmpty(e.RoleName))
            sb.Append(richText ? $" <color=#{e.RoleColorHex}>({e.RoleName})</color>" : $" ({e.RoleName})");

        if (CheatToggles.logIncludeLevel)
            sb.Append($" Lv:{e.Level}");

        if (CheatToggles.logIncludePlatform)
            sb.Append($" {e.Platform}");

        if (CheatToggles.logIncludeTasks && e.HasTasks)
            sb.Append($" Tasks:{e.TasksComplete}/{e.TasksTotal}");

        if (CheatToggles.logIncludeVotekick)
            sb.Append($" VK:{e.Votekick}/3");

        if (CheatToggles.logIncludeFriendCode && !string.IsNullOrEmpty(e.FriendCode))
            sb.Append($" {e.FriendCode}");

        sb.Append(": ").Append(e.Text);
        return sb.ToString();
    }

    // Writes the whole log to a timestamped .txt in TextLogs. Returns the file path, or null if there
    // was nothing to write / an error occurred.
    public static string ExportToFile()
    {
        try
        {
            if (_entries.Count == 0) return null;

            EnsureFolder();

            string path = Path.Combine(TextLogsDir, $"ChatLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            var sb = new StringBuilder();
            sb.AppendLine($"# MalumMenu Chat Log - exported {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"# {_entries.Count} message(s)");
            sb.AppendLine();

            foreach (var e in _entries) sb.AppendLine(FormatEntry(e, false));

            File.WriteAllText(path, sb.ToString());
            return path;
        }
        catch { return null; }
    }

    public static void OpenFolder()
    {
        try
        {
            EnsureFolder();
            Process.Start("explorer.exe", TextLogsDir);
        }
        catch { }
    }
}
