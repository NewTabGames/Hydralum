using UnityEngine;

namespace MalumMenu;

public static class FpsUnlocker
{
    public const int MinFps = 30;
    public const int MaxFps = 240;

    public static int TargetFps = 120;

    private static bool _active;
    private static int _originalTarget;
    private static int _originalVSync;

    // Called every frame (from MenuUI.Update) so the cap sticks across scene loads and any
    // frame-rate the game sets itself. Captures the game's original values on enable and
    // restores them on disable.
    public static void Apply()
    {
        if (CheatToggles.unlockFps)
        {
            if (!_active)
            {
                _originalTarget = Application.targetFrameRate;
                _originalVSync = QualitySettings.vSyncCount;
                _active = true;
            }

            // vSync caps the frame rate to the display regardless of targetFrameRate, so it must be off
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != TargetFps) Application.targetFrameRate = TargetFps;
        }
        else if (_active)
        {
            QualitySettings.vSyncCount = _originalVSync;
            Application.targetFrameRate = _originalTarget;
            _active = false;
        }
    }
}
