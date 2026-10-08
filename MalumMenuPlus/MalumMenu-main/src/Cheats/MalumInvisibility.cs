using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

// Serverside/networked invisibility, ported from KrushMenu (KrushMenu.features.Invisibility).
//
// This is NOT a client-side effect: while enabled we suppress the local player's own position
// broadcast (CustomNetworkTransform.FixedUpdate) and instead repeatedly send a real SnapTo RPC
// (callId 21) to EVERY other client (targetClientId -1), pinning our avatar to an off-map "void"
// position (~3000, 3000). Remote players and the host therefore see us far outside any camera -
// effectively gone - while our own client keeps our true position and moves normally (we never
// apply the void snap to ourselves). Many events (kills, vents, meetings, a received SnapTo,
// ladders, platforms, exile, death) can yank our networked position back on-map, so we re-send
// the void position right after each of them.
public static class MalumInvisibility
{
    // Driven by the Self-tab toggle so it auto-persists / can be keybound like every other cheat.
    public static bool Enabled
    {
        get => CheatToggles.invisibility;
        set => CheatToggles.invisibility = value;
    }

    private const float VoidCenter = 3000f;
    private const float VoidSpread = 100f;
    private const float ReinforceInterval = 1.2f;

    private static float _lastReinforceTime;
    private static bool _wasEnabled;

    // Only meaningful inside an actual game - there is nowhere to hide in the lobby, and broadcasting
    // the void teleport there would just desync us. Matches KrushMenu's OnlyInGame default.
    private static bool ShouldRun() => Enabled && ShipStatus.Instance != null;

    // Called from HudManager.Update: when invisibility is switched off, snap our avatar back to our
    // real position for everyone so we reappear immediately instead of lingering in the void.
    public static void Tick()
    {
        if (_wasEnabled && !Enabled) Reappear();
        _wasEnabled = Enabled;
    }

    private static void Reappear()
    {
        var lp = PlayerControl.LocalPlayer;
        if (lp == null || lp.NetTransform == null || AmongUsClient.Instance == null) return;
        if (ShipStatus.Instance == null) return;
        try { BroadcastSnapTo(lp.NetTransform, lp.GetTruePosition()); } catch { }
    }

    // Re-broadcast the void position after an event that may have reset our networked transform.
    private static void Reinforce()
    {
        if (!ShouldRun() || PlayerControl.LocalPlayer == null) return;
        var netTransform = PlayerControl.LocalPlayer.NetTransform;
        if (netTransform != null) BroadcastSnapTo(netTransform, NextVoidPosition());
    }

    // Send a SnapTo to EVERY other client (-1) WITHOUT moving ourselves locally. We deliberately
    // avoid CustomNetworkTransform.RpcSnapTo here because that also snaps our own view - we only
    // want remote clients to relocate our avatar, while our own screen keeps the true position.
    private static void BroadcastSnapTo(CustomNetworkTransform netTransform, Vector2 position)
    {
        if (AmongUsClient.Instance == null) return;
        try
        {
            ushort seq = (ushort)(netTransform.lastSequenceId + 1);
            netTransform.lastSequenceId = seq;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                netTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable, -1);
            NetHelpers.WriteVector2(position, writer);
            writer.Write(seq);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        catch { }
    }

    private static Vector2 NextVoidPosition() =>
        new Vector2(VoidCenter + Random.Range(-VoidSpread, VoidSpread),
                    VoidCenter + Random.Range(-VoidSpread, VoidSpread));

    // ---- Harmony patches (auto-applied via Harmony.PatchAll) ----

    // Core: suppress the vanilla position broadcast for ourselves and, outside meetings, keep
    // re-pinning our avatar to the void on the reinforce cadence. Everyone else / when disabled
    // runs vanilla untouched.
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
    private static class Invisibility_FixedUpdate
    {
        private static bool Prefix(CustomNetworkTransform __instance)
        {
            if (!ShouldRun()) return true;
            if (__instance == null || __instance.myPlayer == null || __instance.myPlayer != PlayerControl.LocalPlayer)
                return true;

            if (MeetingHud.Instance == null && Time.time - _lastReinforceTime >= ReinforceInterval)
            {
                _lastReinforceTime = Time.time;
                BroadcastSnapTo(__instance, NextVoidPosition());
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    private static class Invisibility_OnKill
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcEnterVent))]
    private static class Invisibility_OnVentEnterRpc
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcExitVent))]
    private static class Invisibility_OnVentExitRpc
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    private static class Invisibility_OnVentEnter
    {
        private static void Postfix(PlayerControl __0)
        {
            if (__0 == PlayerControl.LocalPlayer) Reinforce();
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    private static class Invisibility_OnVentExit
    {
        private static void Postfix(PlayerControl __0)
        {
            if (__0 == PlayerControl.LocalPlayer) Reinforce();
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcBootFromVent))]
    private static class Invisibility_OnBootFromVent
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcClimbLadder))]
    private static class Invisibility_OnClimbLadder
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcUsePlatform))]
    private static class Invisibility_OnUsePlatform
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    private static class Invisibility_OnMeetingStart
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    private static class Invisibility_OnMeetingEnd
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
    private static class Invisibility_OnExiled
    {
        private static void Postfix() => Reinforce();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    private static class Invisibility_OnDie
    {
        private static void Postfix(PlayerControl __instance)
        {
            if (__instance == PlayerControl.LocalPlayer) Reinforce();
        }
    }

    // A SnapTo (or any transform/control RPC) received for our own player is the host/server trying to
    // pull us back on-map, so immediately re-pin to the void right after we process it.
    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
    private static class Invisibility_OnTransformRpc
    {
        private static void Postfix(CustomNetworkTransform __instance)
        {
            if (__instance != null && __instance.myPlayer == PlayerControl.LocalPlayer) Reinforce();
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
    private static class Invisibility_OnPhysicsRpc
    {
        private static void Postfix(PlayerPhysics __instance)
        {
            if (__instance != null && __instance.myPlayer == PlayerControl.LocalPlayer) Reinforce();
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    private static class Invisibility_OnControlRpc
    {
        private static void Postfix(PlayerControl __instance)
        {
            if (__instance == PlayerControl.LocalPlayer) Reinforce();
        }
    }
}
