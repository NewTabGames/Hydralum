using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

// "Level" tab, host-only. In a private lobby you host (meant for a solo lobby), each swing shields the
// local player and then lands a host-decided "protected" murder on that same player: the shield absorbs
// the hit, so you never die. The shield-break shatter is suppressed (the "skip the shield-break visuals"
// optimisation) so it stays smooth.
//
// Deliberately NOT a max-throughput loop: the rate is driven off a slider (levelKillsPerSecond) and
// hard-capped at MaxKillsPerSecond. Each frame fires however many swings that rate calls for, and the
// frame's murder RPCs are packed into a SINGLE GameData packet (same batching the game uses for its own
// outgoing messages) instead of one packet each - so the number of murders per packet scales with the
// slider. It only ever targets yourself, and it requires host because the "protected" outcome is a host
// decision (DecisionByHost).
public static class MalumLevelStrobe
{
    // Ceiling for the kills/sec slider. Kept to a sane strobe rate rather than "as fast as the socket
    // allows" - this is a visual, not a throughput tool.
    public const float MaxKillsPerSecond = 2500f;

    // Batched sends go out on a fixed cadence (not once per frame), so the PACKET rate stays low and steady
    // and well under the server's packet-rate limit no matter how high the frame rate or the kills/sec
    // slider is. Every swing accrued since the last flush is packed into that one packet.
    private const float FlushInterval = 0.05f;  // 20 packets/sec max, regardless of the chosen rate
    private const int MaxSwingsPerBatch = 8;     // burst guard + keeps the packet far under the size limit

    // Fractional swing accumulator - carries the leftover between flushes so the real kills/sec matches the
    // slider regardless of frame rate.
    private static float _accum;
    private static float _lastFlush;

    // Host-decided "the kill failed because the target was protected": absorbed by the shield, no death.
    private const MurderResultFlags ProtectedResult = MurderResultFlags.FailedProtected | MurderResultFlags.DecisionByHost;

    // Called every frame from HudManager.Update.
    public static void Tick()
    {
        if (!CheatToggles.levelStrobe) { _accum = 0f; _lastFlush = Time.time; return; }

        // Host-only, only inside an actual game, and not during a meeting. Never touches anyone but you.
        if (!Utils.isHost || PlayerControl.LocalPlayer == null || ShipStatus.Instance == null
            || MeetingHud.Instance != null)
        {
            _accum = 0f;
            _lastFlush = Time.time;
            return;
        }

        var local = PlayerControl.LocalPlayer;
        if (local.Data == null || local.Data.Disconnected) return;

        float rate = Mathf.Clamp(CheatToggles.levelKillsPerSecond, 1f, MaxKillsPerSecond);

        // Accrue swings continuously so the real kills/sec tracks the slider...
        _accum += Time.deltaTime * rate;

        // ...but only emit a packet on the fixed flush cadence, carrying everything accrued since last time.
        // This caps packets/sec no matter the frame rate or the chosen kills/sec.
        if (Time.time - _lastFlush < FlushInterval) return;
        _lastFlush = Time.time;

        int swings = Mathf.FloorToInt(_accum);
        if (swings <= 0) return;
        _accum -= swings;                               // consume the backlog...
        swings = Mathf.Min(swings, MaxSwingsPerBatch);  // ...but never burst more than the guard allows

        SwingBatch(local, swings);
    }

    // Fires `count` swings and packs all of their murder RPCs into one GameData packet.
    private static void SwingBatch(PlayerControl local, int count)
    {
        var client = AmongUsClient.Instance;
        if (client == null) return;

        MessageWriter writer = null;
        try
        {
            // One GameData batch addressed to everyone (relayed by the server; in a solo lobby there's no
            // one to relay to). All of this frame's murders ride inside this single packet.
            writer = MessageWriter.Get(SendOption.Reliable);
            writer.StartMessage(Tags.GameData);
            writer.Write(client.GameId);

            int queued = 0;
            for (int i = 0; i < count; i++)
            {
                // Shield ourselves LOCALLY only - no networked protect RPC. A networked RpcProtectPlayer
                // from a non-Guardian-Angel is an invalid RPC the server rejects (the likely cause of the
                // host kick), so we just set the protection flag on our own client, exactly like the
                // reference ("flags you as shielded on your own client"). Then apply the protected murder
                // locally for the no-death result (shatter suppressed below). Only the murder-result
                // broadcast below goes over the wire.
                local.TurnOnProtection(false, local.cosmetics.ColorId, local.PlayerId);
                local.MurderPlayer(local, ProtectedResult);

                // Pack the networked murder into the batch (serialization mirrors the game's own
                // MurderPlayer RPC: object netId, callId, target netId, result flags).
                writer.StartMessage((byte)GameDataTypes.RpcFlag);
                writer.WritePacked(local.NetId);
                writer.Write((byte)RpcCalls.MurderPlayer);
                writer.WritePacked(local.NetId);
                writer.Write((int)ProtectedResult);
                writer.EndMessage();
                queued++;
            }

            writer.EndMessage();
            if (queued > 0) client.SendOrDisconnect(writer);
        }
        catch { }
        finally
        {
            writer?.Recycle();
        }
    }

    // "Skip the shield-break visuals" optimisation: while the strobe is running, swallow our own
    // shield-break so the repeated absorbs don't spam the shatter animation (which also keeps the loop
    // smooth at higher slider rates). This only drops the break visual/flag cleanup - the no-death result
    // comes from the FailedProtected flag itself, so we stay alive regardless - and it's gated on the
    // toggle, so protection clears normally again the moment the strobe is turned off.
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    private static class LevelStrobe_SuppressShieldBreak
    {
        private static bool Prefix(PlayerControl __instance)
        {
            if (CheatToggles.levelStrobe && __instance != null && __instance == PlayerControl.LocalPlayer)
                return false;
            return true;
        }
    }
}
