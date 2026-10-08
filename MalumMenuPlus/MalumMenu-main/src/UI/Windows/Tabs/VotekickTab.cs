using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

public class VotekickTab : ITab
{
	public string name => "Votekick";

	private Vector2 _scrollPosition = Vector2.zero;

	public void Draw()
	{
		_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

		if (!Utils.isLobby && !Utils.isInGame && !VotekickHandler.IsVotekickRejoinLoopRunning)
		{
			VotekickHandler.VotekickRejoinStatus = "";
		}

		GUILayout.Label("<color=#aaaaaa><b>Note:</b> This acts as a way to kick people in the lobby, not get host. The host is decided by the person with the lowest game ID. AKA the 2nd person who joined the lobby and so on. Leaving and rejoining will make you the highest ID.</color>", GUIStylePreset.Hint);
		GUILayout.Space(10f);

		DrawAutomationSection();

		GUILayout.Space(14f);

		DrawPlayerList();

		GUILayout.Space(14f);

		DrawSettingsSection();

		GUILayout.EndScrollView();
	}

	private void DrawAutomationSection()
	{
		GUILayout.Label("Votekick & Rejoin System", GUIStylePreset.TabSubtitle);
		GUILayout.Space(6f);

		Color oldBg = GUI.backgroundColor;
		if (VotekickHandler.IsVotekickRejoinLoopRunning)
		{
			GUI.backgroundColor = new Color(0.9f, 0.25f, 0.2f, 1f);
			if (GUILayout.Button("STOP VOTEKICK & REJOIN LOOP", GUIStylePreset.NormalButton, GUILayout.Height(36f)))
			{
				VotekickHandler.StopVotekickRejoinLoop();
			}
		}
		else
		{
			GUI.backgroundColor = new Color(0.2f, 0.65f, 0.3f, 1f);
			if (GUILayout.Button("VOTEKICK & REJOIN", GUIStylePreset.NormalButton, GUILayout.Height(36f)))
			{
				VotekickHandler.StartVotekickRejoinLoop();
			}
		}
		GUI.backgroundColor = oldBg;

		if (!string.IsNullOrEmpty(VotekickHandler.VotekickRejoinStatus) && (Utils.isLobby || Utils.isInGame || VotekickHandler.IsVotekickRejoinLoopRunning))
		{
			GUILayout.Space(4f);
			string statusColor = VotekickHandler.IsVotekickRejoinLoopRunning ? "#ffaa00" : "#00ff88";
			GUILayout.Label($"<color={statusColor}><b>Status:</b> {VotekickHandler.VotekickRejoinStatus}</color>");
		}

		GUILayout.Space(6f);
		GUILayout.BeginHorizontal();

		GUI.backgroundColor = new Color(0.7f, 0.35f, 0.15f, 1f);
		if (GUILayout.Button("Votekick All (1x)", GUIStylePreset.NormalButton, GUILayout.Height(28f)))
		{
			VotekickHandler.VotekickAllNow();
		}

		GUI.backgroundColor = new Color(0.25f, 0.45f, 0.75f, 1f);
		if (GUILayout.Button("Rejoin Lobby", GUIStylePreset.NormalButton, GUILayout.Height(28f)))
		{
			VotekickHandler.RejoinGame();
		}

		GUI.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
		if (GUILayout.Button("Reset Counts", GUIStylePreset.NormalButton, GUILayout.Height(28f)))
		{
			VotekickHandler.ResetTracking();
		}

		GUI.backgroundColor = oldBg;
		GUILayout.EndHorizontal();
	}

	private void DrawPlayerList()
	{
		GUILayout.Label("Players (Pick & Votekick)", GUIStylePreset.TabSubtitle);
		GUILayout.Space(6f);

		int hostId = (AmongUsClient.Instance != null) ? AmongUsClient.Instance.HostId : -1;
		var otherPlayers = new List<PlayerControl>();
		var allPlayers = PlayerControl.AllPlayerControls;
		if (allPlayers != null)
		{
			foreach (var p in allPlayers)
			{
				if (p != null && !p.AmOwner && p.Data != null)
				{
					otherPlayers.Add(p);
				}
			}
		}

		if (otherPlayers.Count == 0)
		{
			GUI.color = new Color(0.6f, 0.6f, 0.6f);
			GUILayout.Label("No other players in the lobby.");
			GUI.color = Color.white;
			return;
		}

		// Show host at the top of the list, followed alphabetically
		otherPlayers.Sort((a, b) =>
		{
			bool aHost = a.OwnerId == hostId;
			bool bHost = b.OwnerId == hostId;
			if (aHost != bHost) return aHost ? -1 : 1;
			string nameA = a.Data?.DefaultOutfit?.PlayerName ?? "";
			string nameB = b.Data?.DefaultOutfit?.PlayerName ?? "";
			return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
		});

		float h = 0, s = 0, v = 0;

		foreach (var player in otherPlayers)
		{
			int clientId = player.Data.ClientId;
			bool isHost = player.OwnerId == hostId;

			int colorId = player.Data.DefaultOutfit != null ? player.Data.DefaultOutfit.ColorId : 0;
			Color playerBaseColor = (colorId < 0 || colorId >= Palette.PlayerColors.Count) ? Color.white : Palette.PlayerColors[colorId];
			Color.RGBToHSV(playerBaseColor, out h, out s, out v);
			Color cardBg = (s < 0.15f)
				? Color.HSVToRGB(0f, 0f, Mathf.Clamp(v * 2f, 0.45f, 0.85f))
				: Color.HSVToRGB(h, Mathf.Min(0.7f, s), Mathf.Clamp(v * 1.1f, 0.45f, 0.85f));

			string playerName = player.Data.DefaultOutfit != null ? player.Data.DefaultOutfit.PlayerName : $"Client {clientId}";
			string hostTag = isHost ? " <color=#ff4444>[HOST]</color>" : "";
			string levelText = $"<color=#888>Lvl {player.Data.PlayerLevel + 1}</color>";
			string platformText = "";
			string friendCodeText = "";

			int voteCount = VotekickHandler.GetVoteCount(clientId);
			string vkBadge = (voteCount >= 2)
				? "<color=#ff3333><b>[VK: 2/3 - READY TO KICK]</b></color>"
				: (voteCount == 1 ? "<color=#ffaa00><b>[VK: 1/3]</b></color>" : "<color=#777777>[VK: 0/3]</color>");

			ClientData client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(player) : null;
			if (client != null)
			{
				try
				{
					platformText = " <color=#555>|</color> <color=#d91a1f>" + Utils.PlatformTypeToString(client.PlatformData.Platform) + "</color>";
				}
				catch { }
			}

			try
			{
				if (!string.IsNullOrEmpty(player.Data.FriendCode))
				{
					friendCodeText = " <color=#555>|</color> <color=#888>" + player.Data.FriendCode + "</color>";
				}
			}
			catch { }

			string titleLine = $"<b>{playerName}</b>{hostTag}  {vkBadge}";
			string subLine = $"{levelText}{platformText}{friendCodeText}";
			string playerCardText = $"{titleLine}\n{subLine}";

			GUILayout.BeginHorizontal();

			// Player information card
			Color oldBg = GUI.backgroundColor;
			bool isSelected = VotekickHandler.SelectedTargetId == clientId;
			GUI.backgroundColor = isSelected ? new Color(0.35f, 0.65f, 0.95f, 1f) : cardBg;
			if (GUILayout.Button(playerCardText, GUIStylePreset.NormalButton, GUILayout.ExpandWidth(true), GUILayout.Height(36f)))
			{
				VotekickHandler.SelectedTargetId = isSelected ? -1 : clientId;
			}

			// Dedicated Votekick Button for this player
			bool alreadyVoted = VotekickHandler.HasVotedThisSession(clientId);
			string kickBtnText;
			Color kickBtnBg;

			if (alreadyVoted)
			{
				kickBtnText = "VOTED";
				kickBtnBg = new Color(0.35f, 0.35f, 0.35f, 0.6f);
			}
			else if (voteCount >= 2)
			{
				kickBtnText = "KICK NOW!";
				kickBtnBg = new Color(0.95f, 0.2f, 0.2f, 1f);
			}
			else
			{
				kickBtnText = "Votekick";
				kickBtnBg = new Color(0.9f, 0.55f, 0.1f, 1f);
			}

			GUI.backgroundColor = kickBtnBg;
			bool prevEnabled = GUI.enabled;
			GUI.enabled = !alreadyVoted;
			if (GUILayout.Button(kickBtnText, GUIStylePreset.NormalButton, GUILayout.Width(115f), GUILayout.Height(36f)))
			{
				VotekickHandler.VotekickPlayer(player);
			}
			GUI.enabled = prevEnabled;

			GUI.backgroundColor = oldBg;
			GUILayout.EndHorizontal();
			GUILayout.Space(2f);
		}
	}

	private void DrawSettingsSection()
	{
		GUILayout.Label("Options & Protections", GUIStylePreset.TabSubtitle);
		GUILayout.Space(4f);

		VotekickProtection.Enabled = GUILayout.Toggle(VotekickProtection.Enabled, " Votekick Protection");
		CheatToggles.preventVotekick = VotekickProtection.Enabled;

		VotekickHandler.NotifyVotekickInfo = GUILayout.Toggle(VotekickHandler.NotifyVotekickInfo, " Floating Notifications on Votekick");
		CheatToggles.notifVotekick = VotekickHandler.NotifyVotekickInfo;

		VotekickHandler.AutoRejoinEnabled = GUILayout.Toggle(VotekickHandler.AutoRejoinEnabled, " Auto-Rejoin on Disconnect");
	}
}
