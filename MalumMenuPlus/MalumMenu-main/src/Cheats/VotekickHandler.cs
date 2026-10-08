using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MalumMenu;

public static class VotekickHandler
{
	[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
	public static class VotekickReset_OnJoin
	{
		public static void Postfix(string gameIdString)
		{
			bool isSameLobby = !string.IsNullOrEmpty(gameIdString) && gameIdString == LastGameCode;
			if (!string.IsNullOrEmpty(gameIdString))
			{
				LastGameCode = gameIdString;
			}

			// Fresh connection: reset votes cast in this session so you can vote once per target again
			VotedThisSession.Clear();

			// When rejoining a brand new lobby, reset cumulative vote tracking
			if (!isSameLobby && !_isRejoining && !IsVotekickRejoinLoopRunning)
			{
				VotekickRejoinStatus = "";
				UniqueVoters.Clear();
				VotesSentByUs.Clear();
			}

			if (_isRejoining && !IsVotekickRejoinLoopRunning)
			{
				RejoinCount++;
				try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#88ff88>Rejoined lobby</color>"); } catch { }
			}
			_isRejoining = false;
		}
	}

	[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
	public static class VotekickReset_OnExitGame
	{
		public static void Postfix()
		{
			if (!IsVotekickRejoinLoopRunning && !_isRejoining)
			{
				VotekickRejoinStatus = "";
				UniqueVoters.Clear();
				VotesSentByUs.Clear();
				VotedThisSession.Clear();
				LastGameCode = "";
				SelectedTargetId = -1;
			}
		}
	}

	[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
	public static class AutoRejoin_OnDisconnect
	{
		public static void Postfix()
		{
			VotedThisSession.Clear();

			// Do not interfere if the automated Votekick & Rejoin loop is running
			if (IsVotekickRejoinLoopRunning)
			{
				return;
			}

			if (!_isRejoining)
			{
				VotekickRejoinStatus = "";
				SelectedTargetId = -1;
			}
			if (!AutoRejoinEnabled || _isRejoining || AmongUsClient.Instance == null)
			{
				return;
			}
			int gameId = AmongUsClient.Instance.GameId;
			string text = (gameId != 0) ? GameCode.IntToGameName(gameId) : LastGameCode;
			if (!string.IsNullOrEmpty(text))
			{
				LastGameCode = text;
				_isRejoining = true;
				if (MalumMenu.menuUI != null)
				{
					MalumMenu.menuUI.StartCoroutine(RejoinCoroutine(text));
				}
			}
		}
	}

	[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
	public static class VotekickInfo_Patch
	{
		[HarmonyPostfix]
		public static void Postfix(int srcClient, int clientId)
		{
			if (_isProcessingVote || AmongUsClient.Instance == null)
			{
				return;
			}
			_isProcessingVote = true;
			try
			{
				if (!UniqueVoters.ContainsKey(clientId))
				{
					UniqueVoters[clientId] = new HashSet<int>();
				}
				UniqueVoters[clientId].Add(srcClient);

				if (srcClient == AmongUsClient.Instance.ClientId)
				{
					VotedThisSession.Add(clientId);
					return;
				}

				ClientData client = AmongUsClient.Instance.GetClient(srcClient);
				string srcName = (client != null && !string.IsNullOrEmpty(client.PlayerName)) ? client.PlayerName : $"Client {srcClient}";
				ClientData client2 = AmongUsClient.Instance.GetClient(clientId);
				string tgtName = (client2 != null && !string.IsNullOrEmpty(client2.PlayerName)) ? client2.PlayerName : $"Client {clientId}";
				ShowInfo(srcName, tgtName);
			}
			catch { }
			finally
			{
				_isProcessingVote = false;
			}
		}
	}

	// Loop State
	public static bool IsVotekickRejoinLoopRunning = false;
	public static int VotekickRejoinCycle = 0;
	public static string VotekickRejoinStatus = "";
	private static Coroutine _votekickRejoinCoroutine = null;

	// Preferences & Settings
	public static bool IgnoreOwnVotekicks = false;
	public static bool NotifyVotekickInfo = true;
	public static bool AutoRejoinEnabled = false;
	public static int SelectedTargetId = -1;
	public static int RejoinCount = 0;
	public static string LastGameCode = "";

	public static readonly Dictionary<int, HashSet<int>> UniqueVoters = new();
	public static readonly Dictionary<int, int> VotesSentByUs = new();
	public static readonly HashSet<int> VotedThisSession = new();

	private static bool _isRejoining = false;
	private static bool _isProcessingVote = false;

	public static bool HasVotedThisSession(int clientId)
	{
		if (VotedThisSession.Contains(clientId))
		{
			return true;
		}
		if (VoteBanSystem.Instance != null)
		{
			try
			{
				return VoteBanSystem.Instance.HasMyVote(clientId);
			}
			catch { }
		}
		return false;
	}

	public static int GetVoteCount(int clientId)
	{
		int count = 0;
		if (UniqueVoters.TryGetValue(clientId, out var voters) && voters != null)
		{
			count = Math.Max(count, voters.Count);
		}
		if (VotesSentByUs.TryGetValue(clientId, out int sent))
		{
			count = Math.Max(count, sent);
		}
		return count;
	}

	public static void RecordVoteSent(int clientId, int count)
	{
		VotedThisSession.Add(clientId);

		if (!VotesSentByUs.ContainsKey(clientId))
		{
			VotesSentByUs[clientId] = 0;
		}
		VotesSentByUs[clientId] = Math.Min(3, VotesSentByUs[clientId] + count);

		if (AmongUsClient.Instance != null)
		{
			if (!UniqueVoters.ContainsKey(clientId))
			{
				UniqueVoters[clientId] = new HashSet<int>();
			}
			UniqueVoters[clientId].Add(AmongUsClient.Instance.ClientId);
		}
	}

	public static void StartVotekickRejoinLoop()
	{
		if (IsVotekickRejoinLoopRunning)
		{
			StopVotekickRejoinLoop();
			return;
		}

		if (MalumMenu.menuUI != null)
		{
			_votekickRejoinCoroutine = MalumMenu.menuUI.StartCoroutine(VotekickAndRejoinLoop());
		}
	}

	public static void StopVotekickRejoinLoop()
	{
		IsVotekickRejoinLoopRunning = false;
		VotekickRejoinCycle = 0;
		VotekickRejoinStatus = "Stopped by user.";
		_isRejoining = false;

		if (_votekickRejoinCoroutine != null && MalumMenu.menuUI != null)
		{
			try { MalumMenu.menuUI.StopCoroutine(_votekickRejoinCoroutine); } catch { }
			_votekickRejoinCoroutine = null;
		}

		try { HudManager.Instance?.Notifier?.AddDisconnectMessage("Votekick & Rejoin stopped."); } catch { }
	}

	public static IEnumerator VotekickAndRejoinLoop()
	{
		if (AmongUsClient.Instance == null || VoteBanSystem.Instance == null)
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Must be in a lobby!</color>"); } catch { }
			yield break;
		}

		int gameId = AmongUsClient.Instance.GameId;
		string code = (gameId != 0) ? GameCode.IntToGameName(gameId) : LastGameCode;
		if (string.IsNullOrEmpty(code))
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>No lobby code found!</color>"); } catch { }
			yield break;
		}
		LastGameCode = code;

		int codeInt = 0;
		try { codeInt = GameCode.GameNameToIntV2(code); } catch { codeInt = 0; }
		if (codeInt == 0)
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Invalid lobby code!</color>"); } catch { }
			yield break;
		}

		IsVotekickRejoinLoopRunning = true;

		// ==========================================
		// PASS 1: Votekick all players (1st vote)
		// ==========================================
		VotekickRejoinCycle = 1;
		VotekickRejoinStatus = "Pass 1/2: Votekicking all players...";
		try { HudManager.Instance?.Notifier?.AddDisconnectMessage("Pass 1/2: Votekicking all players..."); } catch { }

		VotekickAllInternal(1);
		yield return new WaitForSeconds(0.4f);

		// Rejoin 1
		VotekickRejoinStatus = "Pass 1/2: Rejoining lobby...";
		_isRejoining = true;
		AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
		yield return new WaitForSeconds(0.6f);

		var joinRoutine1 = AmongUsClient.Instance.CoJoinOnlineGameFromCode(codeInt);
		if (joinRoutine1 != null)
		{
			AmongUsClient.Instance.StartCoroutine(joinRoutine1);
		}

		float timer = 0f;
		while (_isRejoining && timer < 15f)
		{
			yield return null;
			timer += Time.deltaTime;
		}
		_isRejoining = false;

		// Wait for lobby to settle
		yield return new WaitForSeconds(0.8f);

		if (!IsVotekickRejoinLoopRunning)
		{
			VotekickRejoinStatus = "Cancelled.";
			yield break;
		}

		// ==========================================
		// PASS 2: Votekick all players (2nd vote)
		// ==========================================
		VotekickRejoinCycle = 2;
		VotekickRejoinStatus = "Pass 2/2: Votekicking all players...";
		try { HudManager.Instance?.Notifier?.AddDisconnectMessage("Pass 2/2: Votekicking all players..."); } catch { }

		VotekickAllInternal(1);
		yield return new WaitForSeconds(0.4f);

		// Rejoin 2
		VotekickRejoinStatus = "Pass 2/2: Final rejoin...";
		_isRejoining = true;
		AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
		yield return new WaitForSeconds(0.6f);

		var joinRoutine2 = AmongUsClient.Instance.CoJoinOnlineGameFromCode(codeInt);
		if (joinRoutine2 != null)
		{
			AmongUsClient.Instance.StartCoroutine(joinRoutine2);
		}

		timer = 0f;
		while (_isRejoining && timer < 15f)
		{
			yield return null;
			timer += Time.deltaTime;
		}
		_isRejoining = false;

		yield return new WaitForSeconds(0.8f);

		// ==========================================
		// COMPLETE: Everyone at 2 votes!
		// ==========================================
		IsVotekickRejoinLoopRunning = false;
		VotekickRejoinCycle = 0;
		VotekickRejoinStatus = "Ready! Everyone is at 2 votes. Click Votekick to kick anyone!";
		try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#00ff88>Ready! All players at 2 votes. Click Votekick on any player to kick them!</color>"); } catch { }
		_votekickRejoinCoroutine = null;
	}

	public static int VotekickAllInternal(int votesPerPlayer = 1)
	{
		if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null)
		{
			return 0;
		}

		int votedCount = 0;
		string src = PlayerControl.LocalPlayer?.Data?.DefaultOutfit?.PlayerName ?? "Me";
		foreach (PlayerControl current in PlayerControl.AllPlayerControls)
		{
			if (current == null || current.AmOwner || current.Data == null)
			{
				continue;
			}
			int clientId = current.Data.ClientId;
			if (HasVotedThisSession(clientId))
			{
				continue;
			}

			for (int i = 0; i < votesPerPlayer; i++)
			{
				VoteBanSystem.Instance.CmdAddVote(clientId);
			}
			RecordVoteSent(clientId, votesPerPlayer);
			votedCount++;
		}
		return votedCount;
	}

	public static void VotekickAllNow()
	{
		if (VoteBanSystem.Instance == null)
		{
			return;
		}
		int count = VotekickAllInternal(1);
		if (count > 0)
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"Votekicked {count} player(s) (1 vote)."); } catch { }
		}
		else
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ffaa00>Already voted all players this session! Rejoin to vote again.</color>"); } catch { }
		}
	}

	public static void VotekickPlayer(PlayerControl player)
	{
		if (player == null || player.Data == null || VoteBanSystem.Instance == null)
		{
			return;
		}
		try
		{
			int clientId = player.Data.ClientId;
			if (HasVotedThisSession(clientId))
			{
				try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ffaa00>Already voted this session! Rejoin to vote again.</color>"); } catch { }
				return;
			}

			string src = PlayerControl.LocalPlayer?.Data?.DefaultOutfit?.PlayerName ?? "Me";
			string tgt = player.Data.DefaultOutfit?.PlayerName ?? $"Client {clientId}";

			VoteBanSystem.Instance.CmdAddVote(clientId);
			RecordVoteSent(clientId, 1);

			int currentVotes = GetVoteCount(clientId);
			if (currentVotes < 3)
			{
				try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"Voted to kick {tgt} ({currentVotes}/3)"); } catch { }
			}
		}
		catch { }
	}

	public static void VotekickTarget()
	{
		if (SelectedTargetId == -1 || VoteBanSystem.Instance == null)
		{
			return;
		}
		try
		{
			if (HasVotedThisSession(SelectedTargetId))
			{
				try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ffaa00>Already voted this session! Rejoin to vote again.</color>"); } catch { }
				return;
			}

			string src = PlayerControl.LocalPlayer?.Data?.DefaultOutfit?.PlayerName ?? "Me";
			ClientData client = AmongUsClient.Instance?.GetClient(SelectedTargetId);
			string text = (client != null && !string.IsNullOrEmpty(client.PlayerName)) ? client.PlayerName : $"Client {SelectedTargetId}";

			VoteBanSystem.Instance.CmdAddVote(SelectedTargetId);
			RecordVoteSent(SelectedTargetId, 1);

			int currentVotes = GetVoteCount(SelectedTargetId);
			if (currentVotes < 3)
			{
				try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"Voted to kick {text} ({currentVotes}/3)"); } catch { }
			}
		}
		catch { }
	}

	public static void RejoinGame()
	{
		if (_isRejoining || AmongUsClient.Instance == null || MalumMenu.menuUI == null)
		{
			return;
		}
		int gameId = AmongUsClient.Instance.GameId;
		string text = LastGameCode;
		if (gameId != 0)
		{
			try
			{
				text = GameCode.IntToGameName(gameId);
			}
			catch { }
		}
		if (string.IsNullOrEmpty(text))
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>No game code available</color>"); } catch { }
		}
		else
		{
			LastGameCode = text;
			_isRejoining = true;
			MalumMenu.menuUI.StartCoroutine(RejoinCoroutine(text));
		}
	}

	private static IEnumerator RejoinCoroutine(string code)
	{
		if (AmongUsClient.Instance == null || string.IsNullOrEmpty(code))
		{
			_isRejoining = false;
			yield break;
		}

		try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"Rejoining {code}..."); } catch { }

		if (IsInGameOrLobby())
		{
			AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
			yield return new WaitForSeconds(0.6f);
		}
		else
		{
			yield return null;
		}

		int gameId = 0;
		try { gameId = GameCode.GameNameToIntV2(code); } catch { gameId = 0; }
		if (gameId == 0)
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage("<color=#ff4444>Invalid code - cannot rejoin</color>"); } catch { }
			_isRejoining = false;
			yield break;
		}

		var joinRoutine = AmongUsClient.Instance.CoJoinOnlineGameFromCode(gameId);
		if (joinRoutine != null)
		{
			AmongUsClient.Instance.StartCoroutine(joinRoutine);
		}

		float timer = 0f;
		while (_isRejoining && timer < 15f)
		{
			yield return null;
			timer += Time.deltaTime;
		}

		_isRejoining = false;
	}

	public static void ResetTracking()
	{
		UniqueVoters.Clear();
		VotesSentByUs.Clear();
		VotedThisSession.Clear();
		SelectedTargetId = -1;
		VotekickRejoinStatus = "";
		try { HudManager.Instance?.Notifier?.AddDisconnectMessage("Vote tracking reset."); } catch { }
	}

	private static void ShowInfo(string src, string tgt)
	{
		if (NotifyVotekickInfo)
		{
			try { HudManager.Instance?.Notifier?.AddDisconnectMessage($"{src} voted to kick {tgt}"); } catch { }
		}
	}

	private static bool IsInGameOrLobby()
	{
		try
		{
			if (AmongUsClient.Instance == null)
			{
				return false;
			}
			if ((int)AmongUsClient.Instance.GameState == 0)
			{
				Scene activeScene = SceneManager.GetActiveScene();
				string name = activeScene.name;
				return name != "MainMenu" && name != "MatchMaking";
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void Tick()
	{
		// Tick hook for any per-frame background logic if needed
	}
}
