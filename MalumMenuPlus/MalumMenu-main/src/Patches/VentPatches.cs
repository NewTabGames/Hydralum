using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class Vent_CanUse
{
    // Prefix: Use Hydra's distance override method (returning 999f) when Disable Vents is on.
    // If Exclude Yourself is on, allow LocalPlayer to proceed to normal/unlockVents checks.
    public static bool Prefix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (!CheatToggles.disableVents) return true;

        if (CheatToggles.ventsExcludeSelf && pc != null && pc.Object == PlayerControl.LocalPlayer)
        {
            return true;
        }

        canUse = false;
        couldUse = false;
        __result = 999f;
        return false;
    }

    // Postfix: Allow usage of vents when Unlock Vents cheat is enabled for crewmates/non-venting roles.
    public static void Postfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        try
        {
            if (!PlayerControl.LocalPlayer || !PlayerControl.LocalPlayer.Data) return;
            if (CheatToggles.disableVents && (!CheatToggles.ventsExcludeSelf || (pc != null && pc.Object != PlayerControl.LocalPlayer))) return;

            if (PlayerControl.LocalPlayer.Data.Role == null || PlayerControl.LocalPlayer.Data.Role.CanVent || PlayerControl.LocalPlayer.Data.IsDead) return;
            if (!CheatToggles.unlockVents) return;

            var @object = pc.Object;
            if (@object == null || @object.Collider == null) return;

            var center = @object.Collider.bounds.center;
            var position = __instance.transform.position;
            var num = Vector2.Distance(center, position);

            canUse = num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(@object.Collider, center, position, Constants.ShipOnlyMask, false);
            couldUse = true;
            __result = num;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
public static class Vent_EnterVent
{
    // Postfix patch of Vent.EnterVent to log on ConsoleUI when a player enters a vent
    // along with the room they entered it in
    public static void Postfix(Vent __instance, PlayerControl pc)
    {
        if (!CheatToggles.logVents || !Utils.isShip) return;

        var (realPlayerName, displayPlayerName, isDisguised) = Utils.GetPlayerIdentity(pc);
        var room = Utils.GetRoomFromPosition(__instance.transform.position); //- (Vector3) pc.Collider.offset);
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        var msg = isDisguised
            ? $"{realPlayerName} (as {displayPlayerName}) entered a vent in {roomName}"
            : $"{realPlayerName} entered a vent in {roomName}";

        ConsoleUI.Log(msg);
        DebugUI.Log(msg);
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
public static class Vent_ExitVent
{
    // Postfix patch of Vent.ExitVent to log on ConsoleUI when a player exits a vent
    // along with the room they exited it in
    public static void Postfix(Vent __instance, PlayerControl pc)
    {
        if (!CheatToggles.logVents || !Utils.isShip) return;

        var (realPlayerName, displayPlayerName, isDisguised) = Utils.GetPlayerIdentity(pc);

        var room = Utils.GetRoomFromPosition(__instance.transform.position); //- (Vector3) pc.Collider.offset);
        var roomName = room != null ? room.RoomId.ToString() : "an unknown location";

        var msg = isDisguised
            ? $"{realPlayerName} (as {displayPlayerName}) exited a vent in {roomName}"
            : $"{realPlayerName} exited a vent in {roomName}";

        ConsoleUI.Log(msg);
        DebugUI.Log(msg);
    }
}
