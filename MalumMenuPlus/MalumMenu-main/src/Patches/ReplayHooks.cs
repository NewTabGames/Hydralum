using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

namespace MalumMenu;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class Replay_MurderPlayer
{
    // __1 is the MurderResultFlags argument (bound positionally to avoid depending on its name).
    public static void Postfix(PlayerControl __instance, PlayerControl target, MurderResultFlags __1)
    {
        if (__instance == null || target == null) return;
        // Only record kills that actually landed. MurderPlayer also fires for blocked attempts
        // (e.g. Guardian Angel shields -> FailedProtected, or host-deferred checks), which were
        // showing up as phantom "K"/"x" markers on the replay map.
        if ((__1 & MurderResultFlags.Succeeded) == 0) return;
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Kill, __instance.PlayerId, __instance.GetTruePosition(), target.GetTruePosition());
        }
        catch { }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class Replay_MeetingHud_Start
{
    public static void Postfix()
    {
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Meeting, 255, Vector2.zero, Vector2.zero);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
public static class Replay_Vent_EnterVent
{
    public static void Postfix(PlayerControl __0, Vent __instance)
    {
        if (__0 == null) return;
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Vent, __0.PlayerId, __0.GetTruePosition(), Vector2.zero);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public static class Replay_PlayerControl_Shapeshift
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Shift, __instance.PlayerId, __instance.GetTruePosition(), Vector2.zero);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
public static class Replay_PlayerControl_ProtectPlayer
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Protect, __instance.PlayerId, __instance.GetTruePosition(), Vector2.zero);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
public static class Replay_PlayerControl_CmdReportDeadBody
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null) return;
        try
        {
            if (MalumMenu.replayUI != null)
                ReplayUI.Rec(ReplayUI.Rt.Report, __instance.PlayerId, __instance.GetTruePosition(), Vector2.zero);
        }
        catch { }
    }
}
