using System.Collections.Generic;
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
        return true;
    }

    // Postfix: two local-player adjustments to who/where a vent can be used from:
    //   Unlock Vents: let a non-venting role use vents at all (crewmate venting).
    //   Vent Interaction Range: let a role that can already vent (or an unlocked one) reach a vent from
    //     farther away, scaled by the configurable multiplier, so you don't have to stand right on it.
    // When neither applies we leave the game's own result untouched, so normal behaviour is unchanged.
    public static void Postfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            if (!local || local.Data == null || pc == null || pc.Object != local || local.Collider == null) return;

            bool alive = !local.Data.IsDead;
            bool roleCanVent = alive && local.Data.Role != null && local.Data.Role.CanVent;
            bool unlocked = alive && CheatToggles.unlockVents;

            bool wantUnlock = unlocked && !roleCanVent;                    // crewmate venting via Unlock Vents
            bool wantRange = CheatToggles.ventRange && (roleCanVent || unlocked); // reach vents from farther
            if (!wantUnlock && !wantRange) return;

            var center = local.Collider.bounds.center;
            var position = __instance.transform.position;
            var num = Vector2.Distance(center, position);

            float maxDist = __instance.UsableDistance;
            if (wantRange) maxDist *= Mathf.Max(1f, CheatToggles.ventRangeMult);

            canUse = num <= maxDist && !PhysicsHelpers.AnythingBetween(local.Collider, center, position, Constants.ShipOnlyMask, false);
            couldUse = true;
            __result = num;
        }
        catch { }
    }
}

// Rewires each vent's Left/Right/Center arrows into a nearest-neighbour chain across every vent, so the
// on-vent directional arrows can walk you to any vent on the map instead of just the map's default 1-3
// connections. The game only supports three arrow targets per vent, so Left/Right traverse the chain and
// Center jumps to the nearest other vent. Chain is Malum's shared nearest-vent tour; the original
// connections are cached and restored whenever Vent Network is off. (Approach adapted from Nocturne.)
[HarmonyPatch(typeof(Vent), nameof(Vent.SetButtons))]
public static class Vent_SetButtons_Network
{
    private static readonly Dictionary<int, Vent[]> _orig = new();

    public static void Reset() => _orig.Clear();

    public static void Prefix(Vent __instance)
    {
        try
        {
            if (__instance == null || ShipStatus.Instance == null) return;
            var vents = ShipStatus.Instance.AllVents;
            if (vents == null || vents.Count < 2) return;

            int id = __instance.Id;

            if (!CheatToggles.ventNetwork)
            {
                // Vent Network is off: only undo a rewire we actually applied to this vent. If we never
                // touched it, leave the vanilla Left/Right/Center alone so the game's normal arrows show.
                // (Capturing _orig unconditionally here risked snapshotting not-yet-assigned/null links at
                // map load and then "restoring" those nulls forever, which hid the regular vent arrows.)
                if (_orig.TryGetValue(id, out var o))
                {
                    __instance.Left = o[0];
                    __instance.Right = o[1];
                    __instance.Center = o[2];
                    _orig.Remove(id);
                }
                return;
            }

            // Vent Network on: snapshot the real connections once, before we rewire them, so we can restore.
            if (!_orig.ContainsKey(id))
                _orig[id] = new[] { __instance.Left, __instance.Right, __instance.Center };

            var tour = MalumCheats.BuildNearestVentTour();
            if (tour == null || tour.Count < 2) return;

            int idx = -1;
            for (int i = 0; i < tour.Count; i++)
                if (tour[i] != null && tour[i].Id == id) { idx = i; break; }
            if (idx < 0) return;

            int n = tour.Count;
            Vent right = tour[(idx + 1) % n];
            Vent left = n > 2 ? tour[(idx - 1 + n) % n] : null;

            __instance.Right = right;
            __instance.Left = left;
            __instance.Center = NearestOther(__instance, tour, right, left);
        }
        catch { }
    }

    // Closest vent that isn't this one and isn't already the Left/Right target - gives the Center arrow
    // a useful destination (a quick hop to the nearest neighbour).
    private static Vent NearestOther(Vent from, List<Vent> tour, Vent a, Vent b)
    {
        Vector2 p = from.transform.position;
        Vent best = null;
        float bd = float.MaxValue;
        foreach (var v in tour)
        {
            if (v == null || v.Id == from.Id) continue;
            if (a != null && v.Id == a.Id) continue;
            if (b != null && v.Id == b.Id) continue;
            float d = Vector2.Distance(p, (Vector2)v.transform.position);
            if (d < bd) { bd = d; best = v; }
        }
        return best;
    }
}

// New map / round: forget the cached original vent connections so they're recaptured cleanly.
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class ShipStatus_Start_VentReset
{
    public static void Postfix() => Vent_SetButtons_Network.Reset();
}

// Vent Interaction Range on exit: with Move In Vents you can walk your avatar away from the vent you
// entered, so this redirects the local player's exit to pop out at the NEAREST vent within the (extended)
// interaction range of where you've walked - the exit-side mirror of entering a vent from a distance.
// When you're standing on the vent you entered it's already the nearest, so the id is unchanged and normal
// exits behave exactly as before; the redirect only kicks in once you've walked closer to another vent.
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcExitVent))]
public static class PlayerPhysics_RpcExitVent_Range
{
    public static void Prefix(PlayerPhysics __instance, ref int ventId)
    {
        try
        {
            if (!CheatToggles.ventRange) return;

            var local = PlayerControl.LocalPlayer;
            if (local == null || __instance == null || __instance.myPlayer != local) return;
            if (local.Data == null || local.Data.IsDead) return;
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllVents == null) return;

            Vector2 pos = local.GetTruePosition();
            Vent nearest = null;
            float best = float.MaxValue;

            foreach (var v in ShipStatus.Instance.AllVents)
            {
                if (v == null) continue;
                float d = Vector2.Distance(pos, (Vector2)v.transform.position);
                float maxDist = v.UsableDistance * Mathf.Max(1f, CheatToggles.ventRangeMult);
                if (d <= maxDist && d < best) { best = d; nearest = v; }
            }

            if (nearest != null) ventId = nearest.Id;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
public static class Vent_EnterVent
{
    // Postfix patch of Vent.EnterVent to:
    // 1) Fire DisableVents boot when an RPC indicates a player entered a vent
    // 2) Log on ConsoleUI when a player enters a vent along with the room
    public static void Postfix(Vent __instance, PlayerControl pc)
    {
        // Fire the disable-vents boot immediately on this RPC event
        MalumCheats.OnPlayerEnteredVent(pc);

        // Force the directional arrows to build the moment the local player enters a vent. Without this the
        // game's connected-vent arrows sometimes don't paint until you hop to another vent (which triggers
        // SetButtons via ClickRight). This fires unconditionally for the local player - not gated on any
        // cheat toggle - so the plain vanilla vent system (both Unlock Vents and Vent Network off) still
        // paints its arrows, and any leftover Vent Network rewire is restored by SetButtons's own prefix on
        // this same call. SetButtons(true) is idempotent, so re-calling it for a normal venter is harmless.
        if (pc != null && pc.AmOwner && __instance != null)
        {
            try { __instance.SetButtons(true); } catch { }
        }

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
    // Prefix to suppress vent exit when local player is protected by Exclude Yourself during a cheat boot
    public static bool Prefix(Vent __instance, PlayerControl pc)
    {
        if (CheatToggles.isCheatBootingVents && CheatToggles.ventsExcludeSelf)
        {
            if (pc != null && pc.AmOwner)
            {
                return false;
            }
        }
        return true;
    }

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
