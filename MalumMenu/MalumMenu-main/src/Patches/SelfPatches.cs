using HarmonyLib;

namespace MalumMenu;

// Self features adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0

[HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
public static class Ladder_SetDestinationCooldown
{
    // Postfix to zero out the ladder cooldown so you can climb up/down repeatedly (No Ladder Cooldown)
    public static void Postfix(Ladder __instance)
    {
        if (!CheatToggles.noLadderCd) return;

        __instance.CoolDown = 0f;
        if (__instance.Destination != null) __instance.Destination.CoolDown = 0f;
    }
}

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Begin))]
public static class EmergencyMinigame_Begin
{
    // Prefix to refill the local player's remaining emergency meetings each time the button panel
    // opens, so the "X remaining" limit never runs out (Unlimited Meetings)
    public static void Prefix()
    {
        if (!CheatToggles.unlimitedMeetings || PlayerControl.LocalPlayer == null) return;

        PlayerControl.LocalPlayer.RemainingEmergencies = 999999;
    }
}
