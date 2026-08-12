using HarmonyLib;
using System.Linq;
using Sentry.Internal.Extensions;

namespace MalumMenu;

[HarmonyPatch(typeof(EngineerRole), nameof(EngineerRole.FixedUpdate))]
public static class EngineerRole_FixedUpdate
{
    public static void Postfix(EngineerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            MalumCheats.HandleEngineerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(ShapeshifterRole), nameof(ShapeshifterRole.FixedUpdate))]
public static class ShapeshifterRole_FixedUpdate
{
    public static void Postfix(ShapeshifterRole __instance)
    {
        try
        {
            if(__instance.Player.AmOwner)
            {
                MalumCheats.HandleShapeshifterCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(ScientistRole), nameof(ScientistRole.Update))]
public static class ScientistRole_Update
{
    public static void Postfix(ScientistRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            MalumCheats.HandleScientistCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FixedUpdate))]
public static class TrackerRole_FixedUpdate
{
    public static void Postfix(TrackerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            MalumCheats.HandleTrackerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.IsValidTarget))]
public static class PhantomRole_IsValidTarget
{
    // Postfix patch of PhantomRole.IsValidTarget to allow killing while invisible
    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killVanished)
        {
            __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
public static class ImpostorRole_IsValidTarget
{
    // Postfix patch of ImpostorRole.IsValidTarget to allow forbidden kill targets for killAnyone cheat
    // Allows killing ghosts (with seeGhosts), impostors, players in vents, etc...
    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        // No Kill Checks forces every non-self target valid and, unlike Kill Anyone, works without
        // being host. Adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
        if (CheatToggles.noKillChecks && PlayerControl.LocalPlayer != null && target != PlayerControl.LocalPlayer.Data)
        {
            __result = true;
        }
        else if (CheatToggles.killAnyone)
        {
           __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.IsValidTarget))]
public static class RoleBehaviour_IsValidTarget
{
    // Postfix patch of the base RoleBehaviour.IsValidTarget for No Kill Checks, covering killing
    // roles other than the plain impostor. Adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.noKillChecks && PlayerControl.LocalPlayer != null && target != PlayerControl.LocalPlayer.Data)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.FindClosestTarget))]
public static class ImpostorRole_FindClosestTarget
{
    // Prefix patch of ImpostorRole.FindClosestTarget to allow for infinite kill reach
    public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.killReach) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        __result = playerList[0];

        return false;
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.FindClosestTarget))]
public static class DetectiveRole_FindClosestTarget
{
    // Prefix patch of DetectiveRole.FindClosestTarget to allow for infinite interrogate reach
    public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.interrogateReach) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        __result = playerList[0];

        return false;
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FindClosestTarget))]
public static class TrackerRole_FindClosestTarget
{
    // Prefix patch of TrackerRole.FindClosestTarget to allow for infinite track reach
    public static bool Prefix(TrackerRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.trackReach) return true;

        var playerList = Utils.GetPlayersSortedByDistance().Where(player => !player.IsNull() && __instance.IsValidTarget(player.Data) && player.Collider.enabled).ToList();

        __result = playerList[0];

        return false;
    }
}
