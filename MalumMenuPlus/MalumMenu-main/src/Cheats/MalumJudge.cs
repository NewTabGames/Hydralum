using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace MalumMenu;

// Judge cheats (Roles > Judge), reimplemented cleanly from the KrushMenu reference:
//   Infinite Overrules - never consume the overrule use and refill it each meeting tick, so the Judge can
//     overrule every meeting without the one-use limit.
//   Judge Immune (toggle lives in CheatToggles) - cancel any overrule verdict that targets the local player.
//   Force Overrule - force an overrule verdict onto any player: as the Judge via TryOverrule, otherwise by
//     forging one (make another living player appear to have overruled, via CmdQueueOverruleVotes) or, as
//     host, by broadcasting the verdict directly.
public static class MalumJudge
{
    private static int _lastScapegoatId = -1;

    private static JudgeRole GetLocalJudge()
    {
        var role = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null
            ? PlayerControl.LocalPlayer.Data.Role : null;
        return role != null ? role.TryCast<JudgeRole>() : null;
    }

    // Living, connected players that can be force-overruled (only meaningful during a meeting).
    public static List<PlayerControl> GetOverruleTargets()
    {
        var list = new List<PlayerControl>();
        if (MeetingHud.Instance == null || PlayerControl.AllPlayerControls == null) return list;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.Disconnected || p.Data.IsDead) continue;
            list.Add(p);
        }
        return list;
    }

    public static void ForceOverrule(PlayerControl target)
    {
        if (target == null || target.Data == null) return;
        if (MeetingHud.Instance == null) { ConsoleUI.Log("Judge: no active meeting to overrule in."); return; }

        try
        {
            // If we're the Judge and still have an overrule available, use the real ability.
            var judge = GetLocalJudge();
            if (judge != null)
            {
                bool blocked = false;
                try { blocked = judge.HasAlreadyOverruledThisMeeting || !judge.HasAnOverruleUse; } catch { }
                if (!blocked && judge.TryOverrule(target.PlayerId))
                {
                    ConsoleUI.Log($"Judge: overruled {target.Data.PlayerName}.");
                    return;
                }
            }

            // Not the Judge (or no use left): forge an overrule from another player.
            if (ForgeOverrule(target)) return;

            // Last resort: as host, broadcast the verdict directly.
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0), target.Data, false, true, 0);
                ConsoleUI.Log($"Judge: force-overruled {target.Data.PlayerName} (host).");
                return;
            }

            ConsoleUI.Log("Judge: couldn't force an overrule (no eligible player to attribute it to).");
        }
        catch { }
    }

    // Make another living player appear to have cast the overrule, via the client->host CmdQueueOverruleVotes
    // path - so a non-Judge can trigger a Judge verdict. Rotates the "scapegoat" so it isn't always the same.
    private static bool ForgeOverrule(PlayerControl target)
    {
        try
        {
            PlayerControl scapegoat = PickScapegoat(target, avoidLast: true) ?? PickScapegoat(target, avoidLast: false);
            if (scapegoat == null) return false;

            _lastScapegoatId = scapegoat.PlayerId;
            ushort nonce = (ushort)Random.Range(0, 65535);
            MeetingHud.Instance.CmdQueueOverruleVotes(scapegoat.PlayerId, target.PlayerId, nonce);

            ConsoleUI.Log($"Judge: forged an overrule on {target.Data.PlayerName} via {scapegoat.Data.PlayerName}.");
            return true;
        }
        catch { return false; }
    }

    private static PlayerControl PickScapegoat(PlayerControl target, bool avoidLast)
    {
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.AmOwner || p.Data == null || p.Data.Disconnected || p.Data.IsDead) continue;
            if (p.PlayerId == target.PlayerId) continue;
            if (avoidLast && p.PlayerId == _lastScapegoatId) continue;
            return p;
        }
        return null;
    }
}

// --- Patches ---

// Infinite Overrules: don't consume the overrule use.
[HarmonyPatch(typeof(JudgeRole), nameof(JudgeRole.ConsumeOverruleVotesUsage))]
public static class JudgeRole_ConsumeOverrule
{
    public static bool Prefix() => !CheatToggles.judgeInfiniteOverrules;
}

// Infinite Overrules: refill the local Judge's overrule each meeting tick so it's always available.
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class JudgeRole_RefillOverrule
{
    public static void Postfix()
    {
        if (!CheatToggles.judgeInfiniteOverrules) return;
        try
        {
            var role = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null
                ? PlayerControl.LocalPlayer.Data.Role : null;
            var judge = role != null ? role.TryCast<JudgeRole>() : null;
            if (judge != null)
            {
                judge.HasAnOverruleUse = true;
                judge.HasAlreadyOverruledThisMeeting = false;
            }
        }
        catch { }
    }
}

// Judge Immune: cancel any overrule verdict that would exile the local player. This is client-side (it edits
// the VotingComplete the local client applies), mirroring the KrushMenu reference.
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
public static class JudgeImmune_VotingComplete
{
    public static void Prefix(ref NetworkedPlayerInfo exiled, ref bool wasOverruled)
    {
        if (!CheatToggles.judgeImmune) return;
        if (wasOverruled && exiled != null && PlayerControl.LocalPlayer != null
            && exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            exiled = null;
            wasOverruled = false;
        }
    }
}
