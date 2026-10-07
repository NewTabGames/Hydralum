using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// Advanced Role Cooldown ESP (single "Advanced Role Cooldown" toggle): shows each player's role-ability
// cooldown(s) above their head, colour-coded per ability, counting down to "Ready". Like the other cooldown
// ESPs (Kill, Guardian Angel), the game doesn't network other players' timers, so we ESTIMATE them: read the
// lobby's cooldown setting and (re)start the countdown whenever we observe the ability being used via a call
// that runs on every client. Estimates are frozen while a meeting / ejection is on screen (cooldowns pause
// then). Impostor ability cooldowns stack ABOVE the kill line.
//
// Covered so far: impostor kill (red), Viper kill (dark green, replaces red), Shapeshifter (darker red),
// Phantom disappear (dark purple), Engineer vent (grey). The kill line is read from the existing Kill
// Cooldown ESP estimator so the two features never double-count; while this toggle is on, the standalone
// "Impostor Kill" cooldown label is suppressed (see KillCooldownEsp.GetLabel).
public static class AdvancedRoleCooldownEsp
{
    // Per-ability "ready at" timestamps (Time.time) keyed by player id.
    private static readonly Dictionary<byte, float> _shapeshiftReady = new Dictionary<byte, float>();
    private static readonly Dictionary<byte, float> _vanishReady = new Dictionary<byte, float>();
    private static readonly Dictionary<byte, float> _ventReady = new Dictionary<byte, float>();

    // Colours. Shapeshift / disappear keep the requested red / purple hues but are brightened from the
    // original #2a0000 / #360f5a so they're actually readable as on-screen text over dark maps.
    private const string KillColor = "#ff3b3b";       // red
    private const string ViperKillColor = "#1f8a3b";  // dark green (replaces red for Viper)
    private const string ShapeshiftColor = "#8a1a1a"; // darker red (brightened from #2a0000)
    private const string VanishColor = "#7a3bd0";     // dark purple (brightened from #360f5a)
    private const string VentColor = "#9a9a9a";       // grey
    private const string ReadyColor = "#55ff55";

    private static float Opt(FloatOptionNames name, float fallback)
    {
        try
        {
            var opts = GameManager.Instance != null && GameManager.Instance.LogicOptions != null
                ? GameManager.Instance.LogicOptions.currentGameOptions
                : null;
            if (opts != null) return opts.GetFloat(name);
        }
        catch { }
        return fallback;
    }

    public static void OnShapeshift(byte id) => _shapeshiftReady[id] = Time.time + Opt(FloatOptionNames.ShapeshifterCooldown, 10f);
    public static void OnVanish(byte id) => _vanishReady[id] = Time.time + Opt(FloatOptionNames.PhantomCooldown, 30f);
    public static void OnVent(byte id) => _ventReady[id] = Time.time + Opt(FloatOptionNames.EngineerCooldown, 30f);

    // Cleared at round start.
    public static void Reset()
    {
        _shapeshiftReady.Clear();
        _vanishReady.Clear();
        _ventReady.Clear();
    }

    // Freeze every estimate while a meeting / ejection cutscene is on screen (ability cooldowns don't tick
    // then). Kill is handled by the Kill Cooldown ESP estimator, which instead resets after meetings - that
    // matches the game giving a fresh kill cooldown when play resumes. Called every frame from HudManager.
    public static void Update()
    {
        bool busy = MeetingHud.Instance != null || ExileController.Instance != null;
        if (!busy) return;

        float dt = Time.deltaTime;
        Freeze(_shapeshiftReady, dt);
        Freeze(_vanishReady, dt);
        Freeze(_ventReady, dt);
    }

    private static void Freeze(Dictionary<byte, float> d, float dt)
    {
        // Push still-running timers forward by dt so their remaining time holds steady during the meeting.
        var keys = new List<byte>(d.Keys);
        foreach (var k in keys) if (d[k] > Time.time) d[k] += dt;
    }

    private static float Rem(Dictionary<byte, float> d, byte id) => d.TryGetValue(id, out var t) ? t - Time.time : 0f;

    private static string Line(string color, float remaining)
        => remaining > 0.05f
            ? $"<color={color}><size=70%>{Mathf.CeilToInt(remaining)}s</size></color>"
            : $"<color={ReadyColor}><size=70%>Ready</size></color>";

    // The stacked cooldown label for a player (empty when the toggle is off / the player is dead / has no
    // covered ability). Ability cooldowns come first (they render above the kill line).
    public static string GetLabel(PlayerControl player)
    {
        if (!CheatToggles.advancedRoleCooldownEsp || player == null || player.Data == null
            || player.Data.Role == null || player.Data.IsDead) return "";

        var role = player.Data.Role.Role;
        byte id = player.PlayerId;
        var lines = new List<string>();

        // Impostor ability cooldowns stack above the kill line.
        if (role == RoleTypes.Shapeshifter) lines.Add(Line(ShapeshiftColor, Rem(_shapeshiftReady, id)));
        if (role == RoleTypes.Phantom) lines.Add(Line(VanishColor, Rem(_vanishReady, id)));

        // Kill line (base) for every impostor; Viper shows it in dark green. Read from the shared Kill
        // Cooldown ESP estimator so we never double-count.
        if (player.Data.Role.IsImpostor)
        {
            string killColor = role == RoleTypes.Viper ? ViperKillColor : KillColor;
            lines.Add(Line(killColor, KillCooldownEsp.GetRemaining(id)));
        }

        // Crewmate: Engineer vent.
        if (role == RoleTypes.Engineer) lines.Add(Line(VentColor, Rem(_ventReady, id)));

        return lines.Count == 0 ? "" : string.Join("\n", lines);
    }
}

// --- Ability-use hooks (each runs on every client, so __instance / pc is the acting player) ---

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public static class AdvancedRoleCooldownEsp_Shapeshift
{
    public static void Postfix(PlayerControl __instance, PlayerControl targetPlayer)
    {
        try
        {
            if (__instance == null) return;
            // Reverting (shapeshifting back into yourself) reuses Shapeshift but doesn't start a new
            // cooldown - only an actual shift into another player does. Ignore the revert so the estimate
            // isn't pushed out every time a shapeshifter changes back.
            if (targetPlayer == __instance) return;
            AdvancedRoleCooldownEsp.OnShapeshift(__instance.PlayerId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleServerVanish))]
public static class AdvancedRoleCooldownEsp_Vanish
{
    public static void Postfix(PlayerControl __instance)
    {
        try { if (__instance != null) AdvancedRoleCooldownEsp.OnVanish(__instance.PlayerId); } catch { }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
public static class AdvancedRoleCooldownEsp_Vent
{
    // Engineer vent cooldown starts when they leave the vent. Impostor vents are free, so gate on Engineer.
    public static void Postfix(PlayerControl pc)
    {
        try
        {
            if (pc == null || pc.Data == null || pc.Data.Role == null) return;
            if (pc.Data.Role.Role == RoleTypes.Engineer) AdvancedRoleCooldownEsp.OnVent(pc.PlayerId);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class AdvancedRoleCooldownEsp_RoundStart
{
    public static void Postfix() { try { AdvancedRoleCooldownEsp.Reset(); } catch { } }
}
