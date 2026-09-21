using System.Collections.Generic;
using Hazel;

namespace MalumMenu;

// Ported/expanded from VoteKickMod's "Schizo" tab. Each action sends a fake, client-sided sabotage addressed
// to ONE target's client only (StartRpcImmediately with their client id), so only that player sees it -
// nobody else does, and nothing actually happens on the host. The write format matches Malum's own
// RpcUpdateSystem (systemType, packed source netId, amount); we just aim it at a single client instead of the
// host/broadcast. Single-target visual pranks only - this does NOT port that mod's Ban or Overload (crash)
// tabs. Reactor is the proven one; the others reuse Malum's sabotage amounts and should be verified in-game.
public static class MalumSchizo
{
    // ---- Fake sabotages ----

    // Critical meltdown, using the map's reactor-equivalent system.
    public static void FakeReactor(PlayerControl target)
    {
        byte mapId = SafeMapId();
        SystemTypes sys = mapId == 2 ? SystemTypes.Laboratory
                        : mapId == 4 ? SystemTypes.HeliSabotage
                        : SystemTypes.Reactor;
        SendSabotage(target, sys, 128);
    }

    public static void FakeOxygen(PlayerControl target)   => SendSabotage(target, SystemTypes.LifeSupp, 128);
    public static void FakeComms(PlayerControl target)    => SendSabotage(target, SystemTypes.Comms, 128);
    public static void FakeMushroom(PlayerControl target) => SendSabotage(target, SystemTypes.MushroomMixupSabotage, 1);

    // ---- Fake doors ----

    // Closes every door on the target's screen (one CloseDoorsOfType per unique room).
    public static void CloseAllDoors(PlayerControl target)
    {
        if (!Valid(target) || ShipStatus.Instance.AllDoors == null) return;

        var seen = new HashSet<SystemTypes>();
        foreach (OpenableDoor door in ShipStatus.Instance.AllDoors)
        {
            if (door == null || !seen.Add(door.Room)) continue;
            CloseDoor(target, door.Room);
        }
    }

    // Closes just the doors of one room on the target's screen.
    public static void CloseDoor(PlayerControl target, SystemTypes room)
    {
        if (!Valid(target)) return;

        int clientId = ResolveClientId(target);
        if (clientId < 0 || IsHostClient(clientId)) return; // -1 would broadcast; the host applies it for real

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
            ShipStatus.Instance.NetId, (byte)RpcCalls.CloseDoorsOfType, SendOption.Reliable, clientId);
        writer.Write((byte)room);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // ---- helpers ----

    private static bool Valid(PlayerControl target) =>
        target != null && target.Data != null && ShipStatus.Instance != null
        && PlayerControl.LocalPlayer != null && AmongUsClient.Instance != null;

    // The target's client id via a proper lookup. NetworkedPlayerInfo.ClientId can be -1 for some players,
    // and StartRpcImmediately treats -1 as "broadcast to everyone" - which is exactly what leaks these fake
    // sabotages to the whole lobby. Returns -1 only when it genuinely can't be resolved, and callers skip.
    private static int ResolveClientId(PlayerControl target)
    {
        try
        {
            int id = AmongUsClient.Instance.GetClientIdFromCharacter(target);
            if (id >= 0) return id;
        }
        catch { }
        try { return target.Data != null ? target.Data.ClientId : -1; }
        catch { return -1; }
    }

    // The host is authoritative over system state: anything it applies becomes real for the whole lobby, so a
    // "client-sided" fake sent to the host isn't client-sided at all - it goes global. We refuse to send to it.
    public static bool IsHostClient(int clientId)
    {
        try { return AmongUsClient.Instance != null && clientId == AmongUsClient.Instance.HostId; }
        catch { return false; }
    }

    public static bool IsHost(PlayerControl player)
    {
        if (player == null) return false;
        try { return IsHostClient(AmongUsClient.Instance.GetClientIdFromCharacter(player)); }
        catch { return false; }
    }

    private static byte SafeMapId()
    {
        try { return GameOptionsManager.Instance.currentGameOptions.MapId; }
        catch { return 0; }
    }

    private static void SendSabotage(PlayerControl target, SystemTypes sys, byte value)
    {
        if (!Valid(target)) return;

        int clientId = ResolveClientId(target);
        if (clientId < 0 || IsHostClient(clientId)) return; // -1 would broadcast; the host applies it for real

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
            ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, clientId);
        writer.Write((byte)sys);
        writer.WritePacked(PlayerControl.LocalPlayer.NetId);
        writer.Write(value);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
