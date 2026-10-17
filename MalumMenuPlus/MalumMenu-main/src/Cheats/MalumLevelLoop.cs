using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils; // StartCoroutine extension that accepts a managed IEnumerator
using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

// "Level" tab, host-only auto-cycle. While enabled it runs a hands-off loop in a lobby you host:
//   lobby   -> turn on No Game End, assign yourself Impostor (role assigner), force-start the game
//   in-game -> turn on LevelShield for the slider duration, which strobes at the kills/sec slider value
//   timeout -> turn off LevelShield, turn off No Game End (so the game then ends)
//   end     -> hold on the end ("Play Again") screen ~2s, then press Play Again back to the lobby
//   ...and repeat, with short pauses between steps so it paces itself.
// Every feature it touches is a Malum one (noGameEnd, role assigner, LevelShield, SendStartGame), so the
// whole thing lives here - no Hydra involved. It's host-only and self-contained; it never acts on anyone
// else. Driven once per frame from HudManager.Update. Uses an explicit phase so game/lobby transitions
// can't make it re-enter a step or race through the cycle.
public static class MalumLevelLoop
{
    public const float MinDuration = 10f;
    public const float MaxDuration = 60f;

    // Pacing (seconds). Deliberate pauses so the cycle doesn't blow through its steps.
    private const float BetweenCycles = 3f;  // breather in the lobby before starting the next game
    private const float EndSettle = 2f;      // after releasing the game end, before we accept the lobby again
    private const float StartTimeout = 12f;  // if the game never spins up, go back and retry the start
    private const float PlayAgainTimeout = 12f; // max wait for the Game Complete progression to reveal Play Again
    private const float PlayAgainGrace = 4f;    // let the finished screen sit after Play Again lights up

    private enum Phase { Idle, Start, AwaitGame, Shield, AwaitLobby }

    private static Phase _phase = Phase.Idle;
    private static float _timer;        // generic per-phase deadline (Time.time based)
    private static float _shieldUntil;  // when the in-game shield window ends
    private static bool _engaged;       // have we flipped any toggles (for clean teardown)

    // Saved graphics / FPS settings, restored when the loop stops (see EnsureOptimized / RestoreOptimized).
    private static bool _optimized;
    private static int _savedQualityLevel;
    private static int _savedVSync;
    private static int _savedTargetFps;
    private static ShadowQuality _savedShadows;
    private static int _savedAA;

    public static void Tick()
    {
        if (!CheatToggles.levelLoop) { Teardown(); return; }

        // Host-only and only when we actually have a client. In a solo lobby we're always host.
        if (!Utils.isHost || AmongUsClient.Instance == null) return;

        // Crank the game to max performance while the loop runs (restored on Teardown).
        EnsureOptimized();

        switch (_phase)
        {
            case Phase.Idle:
                // Kick off once we're actually sitting in a lobby.
                if (LobbyBehaviour.Instance != null)
                {
                    _phase = Phase.Start;
                    _timer = Time.time + BetweenCycles;
                }
                break;

            case Phase.Start:
                if (LobbyBehaviour.Instance == null) break; // only ever start from the lobby
                if (Time.time < _timer) break;              // brief breather first

                _engaged = true;
                CheatToggles.noGameEnd = true;

                var lp = PlayerControl.LocalPlayer;
                if (lp != null)
                {
                    CheatToggles.roleAssignerEnabled = true;
                    MalumRoleAssign.SetChosen(lp.PlayerId, RoleTypes.Impostor);
                }

                AmongUsClient.Instance.SendStartGame();
                _phase = Phase.AwaitGame;
                _timer = Time.time + StartTimeout;
                break;

            case Phase.AwaitGame:
                if (ShipStatus.Instance != null)
                {
                    // Game is up - run the shield for the FULL slider duration. It strobes at the kills/sec
                    // slider value (MalumLevelStrobe reads that itself); we just switch it on.
                    _shieldUntil = Time.time + Mathf.Clamp(CheatToggles.levelLoopDuration, MinDuration, MaxDuration);
                    CheatToggles.levelStrobe = true;
                    _phase = Phase.Shield;
                }
                else if (Time.time >= _timer)
                {
                    // Start didn't take - back to the lobby step to try again.
                    _phase = Phase.Start;
                    _timer = Time.time + BetweenCycles;
                }
                break;

            case Phase.Shield:
                if (ShipStatus.Instance == null)
                {
                    // Game ended out from under us - bail to the lobby wait.
                    CheatToggles.levelStrobe = false;
                    _phase = Phase.AwaitLobby;
                    _timer = Time.time + EndSettle;
                    break;
                }
                if (Time.time >= _shieldUntil)
                {
                    CheatToggles.levelStrobe = false;  // LevelShield off
                    CheatToggles.noGameEnd = false;    // let the game end (ShowButtons patch returns us to lobby)
                    _phase = Phase.AwaitLobby;
                    _timer = Time.time + EndSettle;
                }
                break;

            case Phase.AwaitLobby:
                // The ShowButtons patch presses Play Again; just wait until we're genuinely back in a lobby
                // (and past the settle delay) before kicking off the next cycle.
                if (LobbyBehaviour.Instance != null && Time.time >= _timer)
                {
                    _phase = Phase.Start;
                    _timer = Time.time + BetweenCycles;
                }
                break;
        }
    }

    // When the loop is switched off, reset the phase and undo anything we were holding so a half-finished
    // cycle doesn't strand the game in "no end" or leave the shield / assigner running.
    private static void Teardown()
    {
        _phase = Phase.Idle;
        RestoreOptimized();
        if (!_engaged) return;
        _engaged = false;
        CheatToggles.levelStrobe = false;
        CheatToggles.noGameEnd = false;
        CheatToggles.roleAssignerEnabled = false;
    }

    // While the loop runs: lowest quality, no shadows/AA/vSync, FPS uncapped. Captures the originals on the
    // first call and re-asserts the uncap each frame (scene loads can reset vSync / targetFrameRate).
    private static void EnsureOptimized()
    {
        if (!_optimized)
        {
            try
            {
                _savedQualityLevel = QualitySettings.GetQualityLevel();
                _savedVSync = QualitySettings.vSyncCount;
                _savedTargetFps = Application.targetFrameRate;
                _savedShadows = QualitySettings.shadows;
                _savedAA = QualitySettings.antiAliasing;
                _optimized = true;

                if (QualitySettings.GetQualityLevel() != 0) QualitySettings.SetQualityLevel(0, false);
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 0;
            }
            catch { }
        }

        try
        {
            if (QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Application.targetFrameRate != -1) Application.targetFrameRate = -1; // uncapped (vSync off)
        }
        catch { }
    }

    // Restores whatever the game had before the loop optimised it.
    private static void RestoreOptimized()
    {
        if (!_optimized) return;
        _optimized = false;
        try
        {
            QualitySettings.SetQualityLevel(_savedQualityLevel, false);
            QualitySettings.vSyncCount = _savedVSync;
            QualitySettings.shadows = _savedShadows;
            QualitySettings.antiAliasing = _savedAA;
            Application.targetFrameRate = _savedTargetFps;
        }
        catch { }
    }

    // On game over the real flow is Victory -> Game Complete (the Lvl/Beans progression page) -> lobby.
    // We advance to the progression page via ShowProgression() (not the Continue button, whose handler
    // stops the EndGameManager's coroutines), wait for the Play Again button to light up, then click it.
    // Runs on the persistent menu object so the progression transition can't kill the coroutine mid-flow.
    private static System.Collections.IEnumerator EndFlowRoutine(EndGameNavigation nav)
    {
        try { nav.ShowProgression(); } catch { }

        // Wait for the progression to finish and the Play Again button to turn active (it starts inactive).
        float deadline = Time.realtimeSinceStartup + PlayAgainTimeout;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (IsActive(nav != null && nav.PlayAgainButton != null ? nav.PlayAgainButton.gameObject : null)) break;
            yield return null;
        }

        // Grace so the finished screen is visible, then click the (now active) Play Again button.
        float graceEnd = Time.realtimeSinceStartup + PlayAgainGrace;
        while (Time.realtimeSinceStartup < graceEnd)
            yield return null;

        if (!CheatToggles.levelLoop) yield break;

        var playAgainGo = nav != null && nav.PlayAgainButton != null ? nav.PlayAgainButton.gameObject : null;
        if (!ClickButton(playAgainGo))
        {
            try { if (nav != null) nav.NextGame(); } catch { }
        }
    }

    // Fires a PassiveButton's click on the given GameObject (searching it, then its parent, then its
    // children), force-enabling the button first. Returns true if a button was found and clicked.
    private static bool ClickButton(GameObject go)
    {
        if (go == null) return false;
        try
        {
            var pb = go.GetComponent<PassiveButton>();
            if (pb == null) pb = go.GetComponentInParent<PassiveButton>();
            if (pb == null) pb = go.GetComponentInChildren<PassiveButton>(true);
            if (pb != null)
            {
                try { pb.SetButtonEnableState(true); } catch { }
                if (pb.OnClick != null) { pb.OnClick.Invoke(); return true; }
            }
        }
        catch { }
        return false;
    }

    private static bool IsActive(GameObject go)
    {
        try { return go != null && go.activeInHierarchy; } catch { return false; }
    }

    // EndGameManager.ShowButtons runs once the Victory screen is ready. We kick off the end-screen flow
    // coroutine (Game Complete -> Play Again) on the persistent menu object (advancing the page stops the
    // EndGameManager's own coroutines). Gated on loop + host.
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.ShowButtons))]
    private static class LevelLoop_AutoContinue
    {
        private static void Postfix(EndGameManager __instance)
        {
            if (!CheatToggles.levelLoop || !Utils.isHost || __instance == null) return;
            var nav = __instance.Navigation;
            var host = MalumMenu.menuUI;
            if (nav != null && host != null)
                host.StartCoroutine(EndFlowRoutine(nav));
        }
    }
}
