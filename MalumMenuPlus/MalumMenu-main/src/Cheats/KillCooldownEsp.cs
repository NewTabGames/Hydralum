using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// Kill Cooldown ESP: shows each impostor's remaining kill cooldown above their head, counting down to
// "Ready". The game doesn't network other players' kill timers, so we estimate it: read the lobby's Kill
// Cooldown setting and (re)start the countdown whenever an impostor kills, whenever a meeting ends, and at
// the start of the round.
public static class KillCooldownEsp
{
    private static readonly Dictionary<byte, float> _readyAt = new Dictionary<byte, float>();

    public static float GetKillCooldown()
    {
        try
        {
            var opts = GameManager.Instance != null && GameManager.Instance.LogicOptions != null
                ? GameManager.Instance.LogicOptions.currentGameOptions
                : null;
            if (opts != null) return opts.GetFloat(AmongUs.GameOptions.FloatOptionNames.KillCooldown);
        }
        catch { }
        return 30f;
    }

    public static void OnKill(byte killerId)
    {
        _readyAt[killerId] = Time.time + GetKillCooldown();
    }

    // Puts every living impostor on a fresh cooldown (used at round start and after meetings).
    public static void ResetAllImpostors()
    {
        if (PlayerControl.AllPlayerControls == null) return;
        float cd = GetKillCooldown();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.Role == null) continue;
            if (p.Data.Role.IsImpostor) _readyAt[p.PlayerId] = Time.time + cd;
        }
    }

    // Gameplay is "busy" during a meeting or the ejection cutscene - kill cooldowns don't run then and the
    // game hands out a fresh cooldown once play actually resumes. We detect that resume edge (both the
    // MeetingHud and ExileController gone) and reset there, so the ejection cutscene isn't counted against
    // the timer. Called every frame from HudManager.Update.
    private static bool _wasBusy;

    public static void Update()
    {
        bool busy = MeetingHud.Instance != null || ExileController.Instance != null;
        if (_wasBusy && !busy) ResetAllImpostors();
        _wasBusy = busy;
    }

    // Label drawn above an impostor's head (empty for non-impostors / toggle off / dead).
    public static string GetLabel(PlayerControl player)
    {
        if (!CheatToggles.killCooldownEsp || player == null || player.Data == null || player.Data.Role == null) return "";
        if (!player.Data.Role.IsImpostor || player.Data.IsDead) return "";

        float remaining = _readyAt.TryGetValue(player.PlayerId, out var t) ? t - Time.time : 0f;

        if (remaining > 0.05f)
            return $"<color=#ff5555><size=70%>{Mathf.CeilToInt(remaining)}s</size></color>";

        return "<color=#55ff55><size=70%>Ready</size></color>";
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class KillCooldownEsp_Murder
{
    public static void Postfix(PlayerControl __instance, MurderResultFlags resultFlags)
    {
        try
        {
            if (__instance == null || __instance.Data == null) return;
            if (!resultFlags.HasFlag(MurderResultFlags.Succeeded)) return;
            KillCooldownEsp.OnKill(__instance.PlayerId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class KillCooldownEsp_RoundStart
{
    // The intro is destroyed once roles are assigned and the round actually begins.
    public static void Postfix()
    {
        try { KillCooldownEsp.ResetAllImpostors(); } catch { }
    }
}
