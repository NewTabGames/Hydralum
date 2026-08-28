using System.Collections;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

// Host-authoritative one-shot actions adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
// Called directly from HostOnlyTab buttons (no persisted toggle needed).
public static class MalumHost
{
    // These powers are only honoured from the host (or a freeplay session, where you are effectively
    // the host). Running them as a non-host either does nothing or gets you kicked by the anticheat.
    private static bool IsHostLike => Utils.isHost || Utils.isFreePlay;

    private static void Notify(string message)
    {
        try { HudManager.Instance.Notifier.AddDisconnectMessage(message); } catch { }
    }

    // ---- Game end -------------------------------------------------------------------------------

    private static void ForceVictory(GameOverReason reason)
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to force a victory");
            return;
        }

        if (!Utils.isShip)
        {
            Notify("A game must be in progress to force a victory");
            return;
        }

        // No Game End would immediately veto the end criteria, so clear it first.
        CheatToggles.noGameEnd = false;

        GameManager.Instance.RpcEndGame(reason, false);
    }

    public static void ForceCrewmateVictory()
    {
        ForceVictory(GameOverReason.CrewmatesByTask);
    }

    public static void ForceImpostorVictory()
    {
        ForceVictory(GameOverReason.ImpostorsByKill);
    }

    // ---- Shapeshift controls --------------------------------------------------------------------

    public static void ShapeshiftAllIntoMe()
    {
        StartShapeshiftAll(PlayerControl.LocalPlayer);
    }

    public static void ShapeshiftAllIntoRandom()
    {
        var target = MalumTroll.GetRandomPlayer();
        if (target != null) StartShapeshiftAll(target);
    }

    private static void StartShapeshiftAll(PlayerControl target)
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to shapeshift players");
            return;
        }

        if (!Utils.isShip || target == null) return;

        AmongUsClient.Instance.StartCoroutine(ShapeshiftAllRoutine(target));
    }

    // Shapeshift every player into the target, one at a time with a small gap. RpcShapeshift can fire
    // a lot of reliable messages at once, so the delay avoids a self-kick from flooding the host link.
    // Note: in vanilla server-authoritative lobbies the anticheat may reject shapeshifting a non-
    // shapeshifter (Hydra warns of the same); this is intended for the host of a private/local lobby.
    private static IEnumerator ShapeshiftAllRoutine(PlayerControl target)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player == target) continue;
            if (player.shapeshiftTargetPlayerId == target.PlayerId) continue;
            if (DevFirewall.IsTargetDev(player) && !player.AmOwner) continue;

            try { player.RpcShapeshift(target, true); } catch { }

            yield return new WaitForSeconds(0.05f);
        }
    }

    public static void RevertAllShapeshifts()
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to revert shapeshifts");
            return;
        }

        if (!Utils.isShip) return;

        AmongUsClient.Instance.StartCoroutine(RevertAllShapeshiftsRoutine());
    }

    // Shapeshifting a player into themselves reverts their disguise (same trick Hydra uses).
    private static IEnumerator RevertAllShapeshiftsRoutine()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.shapeshiftTargetPlayerId == -1) continue;

            try { player.RpcShapeshift(player, true); } catch { }

            yield return new WaitForSeconds(0.05f);
        }
    }

    // ---- Map spawner ----------------------------------------------------------------------------

    public static void SpawnMap(byte mapId)
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to spawn a map");
            return;
        }

        if (mapId >= AmongUsClient.Instance.ShipPrefabs.Count)
        {
            Notify("That map is not available");
            return;
        }

        AmongUsClient.Instance.StartCoroutine(SpawnMapRoutine(mapId));
    }

    private static IEnumerator SpawnMapRoutine(byte mapId)
    {
        // Instantiate the map prefab asynchronously, then spawn it over the network as the host.
        // Hydra creates a raw spawn message; in 2026.6.5 CreateSpawnMessage is gone, so we use the
        // InnerNetClient.Spawn helper (inherited by AmongUsClient) instead.
        var handle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync(null, false);

        while (!handle.IsDone) yield return null;

        var ship = handle.Result.GetComponent<ShipStatus>();
        if (ship == null) yield break;

        AmongUsClient.Instance.Spawn(ship, -2, SpawnFlags.None);

        Notify($"Spawned {(MapNames)mapId}");
    }

    public static void DespawnMap()
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to despawn the map");
            return;
        }

        if (ShipStatus.Instance != null)
        {
            ShipStatus.Instance.Despawn();
            Notify("Despawned the current map");
        }
        else
        {
            Notify("The map is already despawned");
        }
    }

    public static void SpawnLobby()
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to spawn the lobby");
            return;
        }

        // Find the LobbyBehaviour prefab among the non-addressable spawnables (matching by component
        // type is version-robust - no need for the SpawnType enum value).
        InnerNetObject prefab = null;
        foreach (var obj in AmongUsClient.Instance.NonAddressableSpawnableObjects)
        {
            if (obj != null && obj.TryCast<LobbyBehaviour>() != null)
            {
                prefab = obj;
                break;
            }
        }

        if (prefab == null)
        {
            Notify("Could not find the lobby prefab");
            return;
        }

        var lobby = Object.Instantiate(prefab).Cast<LobbyBehaviour>();
        AmongUsClient.Instance.Spawn(lobby, -2, SpawnFlags.None);

        Notify("Spawned a new lobby");
    }

    public static void DespawnLobby()
    {
        if (!IsHostLike)
        {
            Notify("You must be the host to despawn the lobby");
            return;
        }

        if (LobbyBehaviour.Instance != null)
        {
            LobbyBehaviour.Instance.Despawn();
            Notify("Despawned the lobby");
        }
        else
        {
            Notify("The lobby is already despawned");
        }
    }

    // ---- Assign roles for next round ------------------------------------------------------------

    // The role you get assigned at the start of the next round (see the AssignRolesFromList patch).
    // Mirrors Hydra's role list; kept here so the UI slider and the patch share one source of truth.
    public static readonly RoleTypes[] AssignableRoles =
    {
        RoleTypes.Crewmate,
        RoleTypes.Impostor,
        RoleTypes.Scientist,
        RoleTypes.Engineer,
        RoleTypes.GuardianAngel,
        RoleTypes.Shapeshifter,
        RoleTypes.Noisemaker,
        RoleTypes.Phantom,
        RoleTypes.Tracker,
        RoleTypes.Detective,
        RoleTypes.Viper,
        (RoleTypes)19,
        RoleTypes.CrewmateGhost,
        RoleTypes.ImpostorGhost
    };

    public static RoleTypes NextRoundRole = RoleTypes.Impostor;

    // ---- Disco party ----------------------------------------------------------------------------

    // Seconds between color randomizations. Not persisted (resets each session), like the FPS limit.
    public static float DiscoDelay = 0.5f;
    private static float _discoTimer;
    private static readonly System.Random _discoRng = new();

    // Polled every frame from MenuUI.Update. While enabled and hosting, recolors every player to a
    // random (preferably unique) color on an interval. Colors are not reverted on disable - matching
    // Hydra, which just stops changing them. Applies to everyone (no per-player targeting here).
    public static void DiscoParty()
    {
        if (!CheatToggles.discoParty) return;

        if (!IsHostLike || !Utils.isShip)
        {
            _discoTimer = 0f;
            return;
        }

        _discoTimer += Time.deltaTime;
        if (_discoTimer < DiscoDelay) return;
        _discoTimer = 0f;

        var colors = new System.Collections.Generic.List<int>();
        for (var i = 0; i < 18; i++) colors.Add(i);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;
            if (DevFirewall.IsTargetDev(player) && !player.AmOwner) continue;

            int color;
            if (colors.Count != 0)
            {
                var index = _discoRng.Next(0, colors.Count);
                color = colors[index];
                colors.RemoveAt(index);
            }
            else
            {
                // Lobbies with more than 18 players run out of unique colors.
                color = _discoRng.Next(0, 18);
            }

            try { player.RpcSetColor((byte)color); } catch { }
        }
    }

    // ---- Spam report bodies ---------------------------------------------------------------------

    // Seconds between forced meetings. Not persisted (resets each session).
    public static float ReportDelay = 2.5f;
    private static float _reportTimer;

    // Polled every frame from MenuUI.Update. While enabled and hosting, forces a meeting reporting a
    // random player on an interval (adapted from Hydra's ReportBodySpam). We only fire when no meeting
    // is active so meetings don't stack - it re-triggers once the current one closes and the gap
    // elapses. Uses the same host meeting-open path as Call Meeting.
    public static void ReportBodySpam()
    {
        if (!CheatToggles.spamReportBodies) return;

        if (!IsHostLike || !Utils.isShip)
        {
            _reportTimer = 0f;
            return;
        }

        // Don't try to open a meeting on top of an active one.
        if (MeetingHud.Instance != null) return;

        _reportTimer += Time.deltaTime;
        if (_reportTimer < ReportDelay) return;
        _reportTimer = 0f;

        var target = MalumTroll.GetRandomPlayer(false, false);
        if (target == null) return;

        try
        {
            MeetingRoomManager.Instance.AssignSelf(PlayerControl.LocalPlayer, target.Data);
            HudManager.Instance.OpenMeetingRoom(PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcStartMeeting(target.Data);
        }
        catch { }
    }

    // ---- Disable security cameras ---------------------------------------------------------------

    // Sends a Comms-sabotage state update to a single client (adapted from Hydra's DisableCameras).
    // Being on cameras while comms are sabotaged is impossible, so blinding just the watcher's comms
    // kicks them off the cameras without visibly sabotaging comms for the rest of the lobby.
    // This is a raw GameDataTo message - MalumMenu's StartRpcImmediately only builds RPC (not data)
    // messages, so we construct the DataFlag system update by hand as Hydra does.
    public static void SendCommsStateTo(int targetClientId, bool active)
    {
        if (ShipStatus.Instance == null) return;

        try
        {
            if (AmongUsClient.Instance != null)
            {
                var client = AmongUsClient.Instance.GetClient(targetClientId);
                if (client != null && (client.Character == null || !client.Character.AmOwner))
                {
                    if (PresenceTracker.IsDevId(client.ProductUserId) || PresenceTracker.IsDevId(client.FriendCode)) return;
                    if (client.Character != null && DevFirewall.IsTargetDev(client.Character)) return;
                }
            }
        }
        catch { }

        // Inner system message: the Comms system state (a single bool for non-Mira maps).
        var systemUpdate = MessageWriter.Get(SendOption.Reliable);
        systemUpdate.StartMessage((byte)SystemTypes.Comms);
        systemUpdate.Write(active);
        systemUpdate.EndMessage();

        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage((byte)Tags.GameDataTo);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(targetClientId);

        // 1 = DataFlag (a system data update) within a GameData/GameDataTo message.
        writer.StartMessage(1);
        writer.WritePacked(ShipStatus.Instance.NetId);
        writer.Write(systemUpdate, false);
        writer.EndMessage();

        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);

        writer.Recycle();
        systemUpdate.Recycle();
    }
}
