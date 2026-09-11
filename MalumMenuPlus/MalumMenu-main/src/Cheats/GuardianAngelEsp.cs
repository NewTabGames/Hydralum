using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// Guardian Angel Protect Cooldown ESP: shows each Guardian Angel's remaining protect cooldown above their
// head, counting down to "Ready". The game doesn't network other players' protect timers (the role only
// ticks its own cooldown on the owning client), so we estimate it the same way Kill Cooldown ESP does:
// read the lobby's Guardian Angel Cooldown setting and (re)start the countdown whenever a Guardian Angel
// protects someone - a call that runs on every client.
public static class GuardianAngelEsp
{
    private static readonly Dictionary<byte, float> _readyAt = new Dictionary<byte, float>();

    public static float GetProtectCooldown()
    {
        try
        {
            var opts = GameManager.Instance != null && GameManager.Instance.LogicOptions != null
                ? GameManager.Instance.LogicOptions.currentGameOptions
                : null;
            if (opts != null) return opts.GetFloat(AmongUs.GameOptions.FloatOptionNames.GuardianAngelCooldown);
        }
        catch { }
        return 60f;
    }

    public static void OnProtect(byte guardianId)
    {
        _readyAt[guardianId] = Time.time + GetProtectCooldown();
    }

    // Cleared at round start; Guardian Angels haven't formed yet then and start able to protect.
    public static void Reset() => _readyAt.Clear();

    private static bool IsGuardianAngel(PlayerControl player)
    {
        return player != null && player.Data != null && player.Data.Role != null
            && player.Data.Role.Role == RoleTypes.GuardianAngel;
    }

    // Label drawn above a Guardian Angel's head (empty for non-GAs / toggle off).
    public static string GetLabel(PlayerControl player)
    {
        if (!CheatToggles.gaProtectCooldownEsp || !IsGuardianAngel(player)) return "";

        float remaining = _readyAt.TryGetValue(player.PlayerId, out var t) ? t - Time.time : 0f;

        if (remaining > 0.05f)
            return $"<color=#00CCFF><size=70%>{Mathf.CeilToInt(remaining)}s</size></color>";

        return "<color=#55ff55><size=70%>Ready</size></color>";
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
public static class GuardianAngelEsp_Protect
{
    // ProtectPlayer runs on every client when a Guardian Angel protects someone, so __instance is the
    // guardian - start their cooldown estimate here (mirrors Kill Cooldown ESP hooking MurderPlayer).
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            if (__instance == null) return;
            GuardianAngelEsp.OnProtect(__instance.PlayerId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class GuardianAngelEsp_RoundStart
{
    public static void Postfix()
    {
        try { GuardianAngelEsp.Reset(); } catch { }
    }
}
