using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using InnerNet;
using System.Collections;
using UnityEngine;

namespace HydraMenu.anticheat
{
	// Join-time detections, routed through Anticheat.Flag so they blacklist (+ notify, + kick when we're
	// host, + never flag ourselves) instead of doing a raw kick.
	//
	// Each check waits ~3s after the client joins so their player data has finished syncing - judging it too
	// early false-positives on a player who is simply still loading.
	internal class JoinChecks
	{
		// Overflow attack: a malformed join leaves the player's networked data permanently incomplete.
		public static bool CheckOverflow { get; set; } = true;

		// Bots / invalid clients join with no friend code at all.
		public static bool CheckInvalidFriendCode { get; set; } = true;

		[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
		private static class OnPlayerJoined
		{
			private static void Postfix(ClientData data)
			{
				if(!Anticheat.Enabled) return;
				if(!CheckOverflow && !CheckInvalidFriendCode) return;
				if(AmongUsClient.Instance == null || data == null) return;

				AmongUsClient.Instance.StartCoroutine(CheckData(data).WrapToIl2Cpp());
			}
		}

		private static IEnumerator CheckData(ClientData client)
		{
			// Let the player's data finish syncing before judging it.
			yield return new WaitForSeconds(3f);

			if(client == null || client.Character == null || client.Character.Data == null) yield break;

			PlayerControl player = client.Character;
			string name = player.Data.PlayerName ?? "Unknown";

			// 1. Overflow attempt - the player's data never completed.
			if(CheckOverflow && player.Data.IsIncomplete)
			{
				Anticheat.Flag(player, $"{name} sent an overflow attempt (their player data never finished syncing).");
				yield break;
			}

			// 2. Invalid / missing friend code (bots join without one).
			if(CheckInvalidFriendCode && string.IsNullOrEmpty(client.FriendCode))
			{
				Anticheat.Flag(player, $"{name} joined with an invalid (empty) friend code.");
			}
		}
	}
}
