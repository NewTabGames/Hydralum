using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

// Extra Console event loggers (adapted from the KrushMenu reference). Each is gated by its own Console-tab
// toggle and writes to the existing ConsoleUI.Log timeline, with player colors embedded inline to match
// Malum's other console loggers. These sit alongside Malum's existing loggers (Deaths/Shapeshifts/Vents/
// Meetings/Tasks/Game State) rather than replacing them.
internal static class ConsoleLoggers
{
    private static string Fmt(PlayerControl pc)
    {
        try
        {
            // Strip any rich-text the player injected into their name (e.g. <size=200>) so it can't blow up
            // the console layout; we add only our own color wrapper.
            string name = Regex.Replace(pc.Data.PlayerName ?? "?", "<.*?>", "");
            return $"<color=#{ColorUtility.ToHtmlStringRGB(pc.Data.Color)}>{name}</color>";
        }
        catch { return "?"; }
    }

    private static string FmtInfo(NetworkedPlayerInfo info)
    {
        try
        {
            string name = Regex.Replace(info.PlayerName ?? "?", "<.*?>", "");
            return $"<color=#{ColorUtility.ToHtmlStringRGB(info.Color)}>{name}</color>";
        }
        catch { return "?"; }
    }

    private static PlayerControl ById(byte id)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
            if (pc != null && pc.PlayerId == id) return pc;
        return null;
    }

    private static PlayerControl ByOwnerId(int ownerId)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
            if (pc != null && pc.OwnerId == ownerId) return pc;
        return null;
    }

    // ---- Players ----

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    private static class LogJoins
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!CheatToggles.logJoins || __instance == null || __instance.AmOwner || __instance.Data == null) return;
            try { ConsoleUI.Log($"{Fmt(__instance)} joined the lobby"); } catch { }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    private static class LogDisconnects
    {
        public static void Prefix(ClientData data, DisconnectReasons reason)
        {
            if (!CheatToggles.logDisconnects || data == null) return;
            try
            {
                string name = (data.Character != null && data.Character.Data != null) ? Fmt(data.Character) : (data.PlayerName ?? "?");
                ConsoleUI.Log($"{name} disconnected <color=#888888>({reason})</color>");
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    private static class LogGuardianProtect
    {
        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            if (!CheatToggles.logGuardianProtect || __instance == null || target == null) return;
            try { ConsoleUI.Log($"{Fmt(__instance)} protected {Fmt(target)}"); } catch { }
        }
    }

    // ---- Meetings ----

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    private static class LogVotes
    {
        private static readonly HashSet<byte> _logged = new();
        private static int _meetingId = -1;

        public static void Postfix(MeetingHud __instance)
        {
            if (!CheatToggles.logVotes || __instance == null || __instance.playerStates == null) return;
            try
            {
                // Reset the per-meeting dedupe set whenever a new meeting UI appears.
                int id = __instance.GetInstanceID();
                if (id != _meetingId) { _meetingId = id; _logged.Clear(); }

                foreach (var area in __instance.playerStates)
                {
                    if (area == null || _logged.Contains(area.PlayerId)) continue;
                    if (area.VotedForId == PlayerVoteArea.HasNotVoted
                        || area.VotedForId == PlayerVoteArea.MissedVote
                        || area.VotedForId == PlayerVoteArea.DeadVote) continue;

                    _logged.Add(area.PlayerId);

                    var voter = ById(area.PlayerId);
                    if (voter == null) continue;

                    if (area.VotedForId == PlayerVoteArea.SkippedVote)
                    {
                        ConsoleUI.Log($"{Fmt(voter)} voted to skip");
                        continue;
                    }

                    var target = ById(area.VotedForId);
                    ConsoleUI.Log($"{Fmt(voter)} voted for {(target != null ? Fmt(target) : "?")}");
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    private static class LogVotekicks
    {
        public static void Postfix(int srcClient, int clientId)
        {
            if (!CheatToggles.logVotekicks) return;
            try
            {
                var src = ByOwnerId(srcClient);
                var tgt = ByOwnerId(clientId);
                if (src != null)
                    ConsoleUI.Log($"<color=#FF8800>{Fmt(src)} votekicked {(tgt != null ? Fmt(tgt) : "?")}</color>");
            }
            catch { }
        }
    }

    // Judge Verdict: the overrule isn't a normal vote - it comes through VotingComplete with wasOverruled.
    // We record which Judge is overruling in ConsumeOverruleVotesUsage (runs as the overrule is committed on
    // the host / judge's client), then log the verdict when the meeting resolves. Attribution falls back to
    // "A Judge" on clients that never saw the consume call.
    private static PlayerControl _lastOverruleJudge;

    [HarmonyPatch(typeof(JudgeRole), nameof(JudgeRole.ConsumeOverruleVotesUsage))]
    private static class TrackOverruleJudge
    {
        public static void Prefix(JudgeRole __instance)
        {
            try { if (__instance != null) _lastOverruleJudge = __instance.Player; } catch { }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    private static class LogJudgeVerdict
    {
        public static void Postfix(NetworkedPlayerInfo exiled, bool wasOverruled)
        {
            if (!CheatToggles.logVerdict || !wasOverruled) { _lastOverruleJudge = null; return; }

            try
            {
                string judge = (_lastOverruleJudge != null && _lastOverruleJudge.Data != null)
                    ? Fmt(_lastOverruleJudge) : "A Judge";
                string verdict = exiled != null ? $"{FmtInfo(exiled)} ejected" : "the vote forced to skip";
                ConsoleUI.Log($"<color=#5599FF>[Overrule]</color> {judge} overruled → {verdict}");
            }
            catch { }
            finally { _lastOverruleJudge = null; }
        }
    }

    // ---- Game ----

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem), typeof(SystemTypes), typeof(byte))]
    private static class LogSabotages
    {
        public static void Postfix(SystemTypes systemType, byte amount)
        {
            // The high bit (0x80) marks a sabotage being started (vs a normal/repair system update).
            if (!CheatToggles.logSabotages || (amount & 0x80) == 0) return;
            try { ConsoleUI.Log($"Sabotage: <color=#FF4444>{systemType}</color>"); } catch { }
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    private static class LogChat
    {
        public static void Postfix(PlayerControl sourcePlayer, string chatText)
        {
            if (!CheatToggles.logChat || sourcePlayer == null || sourcePlayer.Data == null) return;
            try
            {
                // The game/chat-timestamp feature appends a right-aligned timestamp as a whole
                // <align="right">[time]</align> block. Remove that block ENTIRELY (tags + the timestamp
                // text inside) first - otherwise the timestamp leaks onto its own line - then strip any
                // other rich text players may have injected (<size>/<color>) and trim.
                string clean = Regex.Replace(chatText ?? "", "<align[^>]*>.*?</align>", "",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                clean = Regex.Replace(clean, "<.*?>", "").Trim();
                if (clean.Length == 0) return;

                string dead = sourcePlayer.Data.IsDead ? "<color=#FF9090>[DEAD]</color> " : "";
                ConsoleUI.Log($"{dead}{Fmt(sourcePlayer)}: {clean}");
            }
            catch { }
        }
    }
}
