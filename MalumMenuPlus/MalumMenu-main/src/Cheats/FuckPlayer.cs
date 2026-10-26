using UnityEngine;

namespace MalumMenu;

// "Stuff" tab joke toggle: pick a target (from the player list drawn in the Stuff tab), sidle up behind
// them and thrust back and forth. We drive our own position each frame, which replicates through the
// normal CustomNetworkTransform sync (same approach as Hydra's Follow) - no SnapTo RPC, so it won't trip
// the SnapTo anticheat. Works in the lobby AND in-game (just not during meetings). Driven every frame
// from HudManager.Update.
public static class FuckPlayer
{
    public static PlayerControl Target; // set from the player list in PhysicsTab

    private static bool _moveLocked;

    private const float BaseDistance = 0.32f; // how far behind the target we sit
    private const float Amplitude    = 0.22f; // how far we push in / out
    private const float Frequency    = 15f;   // ~2.4 thrusts per second
    private const float HeightOffset = 0.45f; // sit higher up on the target

    public static void Tick()
    {
        try
        {
            var local = PlayerControl.LocalPlayer;

            bool inSession = CheatToggles.fuckPlayer
                && local != null && local.Data != null
                && (LobbyBehaviour.Instance != null || ShipStatus.Instance != null)
                && MeetingHud.Instance == null;

            // No session, toggle off, or no valid living target -> release and bail.
            if (!inSession
                || Target == null || Target.Data == null || Target.Data.Disconnected || Target.Data.IsDead || Target == local)
            {
                Unlock();
                return;
            }

            if (!_moveLocked)
            {
                _moveLocked = true;
                local.moveable = false;
                if (local.NetTransform != null && local.NetTransform.body != null)
                    local.NetTransform.body.velocity = Vector2.zero;
            }

            Vector2 tpos = Target.GetTruePosition();
            // Which side of the target we sit on. Once we're parked this stays stable (our own x is already
            // offset to that side), so it only really decides the approach side on the first frame.
            float side = (local.GetTruePosition().x >= tpos.x) ? 1f : -1f;
            float thrust = Mathf.Sin(Time.time * Frequency) * Amplitude;
            local.transform.position = tpos + new Vector2(side * (BaseDistance - thrust), HeightOffset);
        }
        catch { }
    }

    private static void Unlock()
    {
        if (_moveLocked && PlayerControl.LocalPlayer != null)
            PlayerControl.LocalPlayer.moveable = true;
        _moveLocked = false;
    }
}
