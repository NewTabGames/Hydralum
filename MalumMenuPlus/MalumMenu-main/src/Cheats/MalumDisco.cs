using UnityEngine;

namespace MalumMenu;

// "Disco": while enabled, rapidly cycles the local player's color (networked, everyone sees it), then
// restores the color you started with when disabled.
public static class MalumDisco
{
    private static byte _originalColor;
    private static bool _hasOriginal;
    private static bool _active;
    private static float _nextChange;

    // Called every frame from HudManager.Update.
    public static void Tick()
    {
        if (!Utils.isPlayer || PlayerControl.LocalPlayer == null)
        {
            _active = false;
            _hasOriginal = false;
            return;
        }

        if (CheatToggles.disco)
        {
            _active = true;
            if (Time.time >= _nextChange)
            {
                ChangeColor();
                float maxSpeed = Utils.isHost ? 100f : 7f; // hosts can strobe faster; clients are capped
                float interval = 1f / Mathf.Clamp(CheatToggles.discoSpeed, 1f, maxSpeed);
                _nextChange = Time.time + interval;
            }
        }
        else if (_active)
        {
            _active = false;
            Restore();
        }
    }

    private static void ChangeColor()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return;
        if (!_hasOriginal) { _originalColor = (byte)local.Data.DefaultOutfit.ColorId; _hasOriginal = true; }
        try { local.CmdCheckColor((byte)MalumAvatar.GetRandomUnusedColor()); } catch { }
    }

    private static void Restore()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && _hasOriginal)
        {
            try { local.CmdCheckColor(_originalColor); } catch { }
        }
        _hasOriginal = false;
    }
}
