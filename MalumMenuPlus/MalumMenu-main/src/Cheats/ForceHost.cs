using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using InnerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MalumMenu;

/// <summary>
/// Force Host exploit ported from ViperMenu (Tuff feature set).
/// Repeatedly votekicks the current lobby host via VoteBanSystem.CmdAddVote and re-joins
/// the room code until host migration designates the local player as the new host.
/// </summary>
public static class ForceHost
{
    // ==================== Configuration & State ====================

    public static bool ForceHostRunning { get; private set; } = false;

    /// <summary>
    /// Number of kick votes sent to the host per attempt (default 3).
    /// </summary>
    public static int ForceHostVoteCount { get; set; } = 3;

    /// <summary>
    /// Delay in seconds between each kick vote sent to the host (default 0.05s).
    /// </summary>
    public static float ForceHostVoteDelay { get; set; } = 0.05f;

    /// <summary>
    /// Delay in seconds before rejoining or after exiting the game (default 0.6s).
    /// </summary>
    public static float ForceHostExitDelay { get; set; } = 0.6f;

    /// <summary>
    /// Whether to abort Force Host if rejoining the game fails (default true).
    /// </summary>
    public static bool ForceHostStopOnJoinFailure { get; set; } = true;

    /// <summary>
    /// Counter for how many hosts were kicked during the current session.
    /// </summary>
    public static int ForceHostHostsKicked { get; private set; } = 0;

    private static string _forceHostCurrentCode = "";
    private static int _forceHostCurrentGameId = 0;
    private static Coroutine _forceHostCoroutine = null;

    // ==================== Public Methods ====================

    /// <summary>
    /// Starts the Force Host routine if in an active multiplayer room and not already host.
    /// </summary>
    /// <param name="coroutineRunner">MonoBehaviour instance used to run the coroutine (e.g., MenuUI component).</param>
    public static void StartForceHost(MonoBehaviour coroutineRunner = null)
    {
        if (ForceHostRunning) return;
        if (AmongUsClient.Instance == null)
        {
            try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Must be in a lobby or game!</color>"); } catch { }
            return;
        }

        if (AmongUsClient.Instance.AmHost)
        {
            try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ffaa00>You are already the host!</color>"); } catch { }
            return;
        }

        int gameId = AmongUsClient.Instance.GameId;
        string roomCode = (gameId != 0) ? GameCode.IntToGameName(gameId) : VotekickHandler.LastGameCode;

        if (string.IsNullOrEmpty(roomCode))
        {
            try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>No game code found!</color>"); } catch { }
            return;
        }

        if (gameId == 0)
        {
            try { gameId = GameCode.GameNameToIntV2(roomCode); } catch { gameId = 0; }
        }

        if (gameId == 0)
        {
            try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Invalid game code!</color>"); } catch { }
            return;
        }

        // Avoid conflict with standard votekick rejoin loop
        if (VotekickHandler.IsVotekickRejoinLoopRunning)
        {
            VotekickHandler.StopVotekickRejoinLoop();
        }

        _forceHostCurrentCode = roomCode;
        _forceHostCurrentGameId = gameId;
        VotekickHandler.LastGameCode = roomCode;
        ForceHostHostsKicked = 0;
        ForceHostRunning = true;

        try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#00ff88>Force Host started...</color>"); } catch { }

        MonoBehaviour runner = coroutineRunner ?? MalumMenu.menuUI;
        if (runner != null)
        {
            _forceHostCoroutine = runner.StartCoroutine(ForceHostLoop());
        }
        else if (AmongUsClient.Instance != null)
        {
            _forceHostCoroutine = AmongUsClient.Instance.StartCoroutine(ForceHostLoop());
        }
    }

    /// <summary>
    /// Stops the Force Host routine.
    /// </summary>
    public static void StopForceHost()
    {
        if (ForceHostRunning)
        {
            ForceHostRunning = false;
            if (_forceHostCoroutine != null && MalumMenu.menuUI != null)
            {
                try { MalumMenu.menuUI.StopCoroutine(_forceHostCoroutine); } catch { }
                _forceHostCoroutine = null;
            }
            try { HudManager.Instance?.Notifier?.AddDisconnectMessage("Force Host stopped."); } catch { }
        }
    }

    // ==================== Core Coroutine ====================

    private static IEnumerator ForceHostLoop()
    {
        while (ForceHostRunning)
        {
            if (AmongUsClient.Instance == null)
            {
                yield return new WaitForSeconds(ForceHostExitDelay);
                continue;
            }

            // If we became the host, stop the routine immediately
            if (AmongUsClient.Instance.AmHost)
            {
                ForceHostRunning = false;
                _forceHostCoroutine = null;
                try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#00ff88><b>Force Host:</b> Succeeded! You are now Host.</color>"); } catch { }
                yield break;
            }

            // If not currently inside game or lobby, attempt to rejoin using saved code/id
            if (!IsInGameOrLobby())
            {
                int gameId = _forceHostCurrentGameId;
                if (gameId == 0)
                {
                    try
                    {
                        gameId = GameCode.GameNameToIntV2(_forceHostCurrentCode);
                    }
                    catch
                    {
                        gameId = 0;
                    }
                }

                if (gameId == 0)
                {
                    ForceHostRunning = false;
                    _forceHostCoroutine = null;
                    yield break;
                }

                bool joinFailed = false;
                try
                {
                    AmongUsClient.Instance.GameId = gameId;
                    var joinRoutine = AmongUsClient.Instance.CoJoinOnlineGameFromCode(gameId, true);
                    if (joinRoutine != null)
                    {
                        AmongUsClient.Instance.StartCoroutine(joinRoutine);
                    }
                    else
                    {
                        joinFailed = true;
                    }
                }
                catch
                {
                    joinFailed = true;
                }

                if (joinFailed)
                {
                    if (ForceHostStopOnJoinFailure)
                    {
                        ForceHostRunning = false;
                        _forceHostCoroutine = null;
                        try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Force Host: Join failed, stopped.</color>"); } catch { }
                        yield break;
                    }

                    yield return new WaitForSeconds(ForceHostExitDelay);
                    continue;
                }

                // Wait up to 5 seconds for lobby/game connection to establish
                float wait = 0f;
                while (wait < 5f && (AmongUsClient.Instance == null || (int)AmongUsClient.Instance.GameState == 0))
                {
                    yield return null;
                    wait += Time.deltaTime;
                }

                if (!ForceHostRunning)
                {
                    _forceHostCoroutine = null;
                    yield break;
                }

                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    ForceHostRunning = false;
                    _forceHostCoroutine = null;
                    try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#00ff88><b>Force Host:</b> Succeeded! You are now Host.</color>"); } catch { }
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            // If in game/lobby, send votekicks against the current host
            if (VoteBanSystem.Instance != null && AmongUsClient.Instance != null)
            {
                int hostId = AmongUsClient.Instance.HostId;
                if (hostId == AmongUsClient.Instance.ClientId || AmongUsClient.Instance.AmHost)
                {
                    ForceHostRunning = false;
                    _forceHostCoroutine = null;
                    try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#00ff88><b>Force Host:</b> Succeeded! You are now Host.</color>"); } catch { }
                    yield break;
                }

                for (int i = 0; i < ForceHostVoteCount; i++)
                {
                    if (!ForceHostRunning)
                    {
                        _forceHostCoroutine = null;
                        yield break;
                    }

                    try
                    {
                        VoteBanSystem.Instance.CmdAddVote(hostId);
                        VotekickHandler.RecordVoteSent(hostId, 1);
                    }
                    catch
                    {
                    }

                    yield return new WaitForSeconds(ForceHostVoteDelay);
                }

                ForceHostHostsKicked++;
                try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"<color=#ffaa00>Voted host out (kick #{ForceHostHostsKicked})</color>"); } catch { }
            }

            yield return new WaitForSeconds(0.3f);
            if (!ForceHostRunning)
            {
                _forceHostCoroutine = null;
                yield break;
            }

            // Exit the lobby if not yet host, to reset and cycle back in
            if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost && IsInGameOrLobby())
            {
                try
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                }
                catch
                {
                }
            }

            yield return new WaitForSeconds(ForceHostExitDelay);
        }

        ForceHostRunning = false;
        _forceHostCoroutine = null;
        yield break;
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Checks whether the local client is currently in a lobby or active match.
    /// </summary>
    public static bool IsInGameOrLobby()
    {
        try
        {
            if (AmongUsClient.Instance == null)
            {
                return false;
            }

            if ((int)AmongUsClient.Instance.GameState > 0)
            {
                return true;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName != "MainMenu" && sceneName != "MatchMaking";
        }
        catch
        {
            return false;
        }
    }
}
