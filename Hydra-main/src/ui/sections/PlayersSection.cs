using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HydraMenu.assets;
using HydraMenu.features;
using HydraMenu.network;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class PlayersSection : ISection
	{
		public PlayersSection() : base("Players") { }

		public static Vector2 PlayerPaneSize
		{
			get { return new Vector2(100 * MainUI.scale, MainUI.WindowSize.y - MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerPanePosition
		{
			get { return new Vector2(MainUI.SectionListPosition.x + MainUI.SectionListSize.x, MainUI.HeaderSize.y + MainUI.HeaderPosition.y); }
		}

		public static Vector2 PlayerButtonSize
		{
			get { return new Vector2(PlayerPaneSize.x, 30 * MainUI.scale); }
		}

		public static Vector2 PlayerOptionsSize
		{
			get { return new Vector2(MainUI.WindowSize.x - MainUI.SectionListSize.x - PlayerPaneSize.x, MainUI.WindowSize.y - MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerOptionsPosition
		{
			get { return new Vector2(PlayerPanePosition.x + PlayerPaneSize.x, MainUI.HeaderPosition.y + MainUI.HeaderSize.y); }
		}

		public static Vector2 PlayerColorBoxSize
		{
			get { return new Vector2(5 * MainUI.scale, PlayerButtonSize.y); }
		}

		public static PlayerControl selectedPlayer;
		public static readonly HashSet<byte> selectedPlayerIds = new();
		private Vector2 subsectionScrollVector;

		private Controls.PlayerColors selectedColor = Controls.PlayerColors.Red;
		private int selectedVent = 0;

		public override void HandleSubsectionMove(int offset)
		{
			if(PlayerControl.AllPlayerControls.Count == 0) return;

			int currentPlayer = PlayerControl.AllPlayerControls.IndexOf(selectedPlayer);
			int newPosition = Math.Clamp(currentPlayer + offset, 0, PlayerControl.AllPlayerControls.Count - 1);

			selectedPlayer = PlayerControl.AllPlayerControls[newPosition];
			selectedPlayerIds.Clear();
			if(selectedPlayer != null)
			{
				selectedPlayerIds.Add(selectedPlayer.PlayerId);
			}
		}

		public override void Render()
		{
			if(PlayerControl.AllPlayerControls.Count == 0)
			{
				GUILayout.Label("There are currently no online players.");
				return;
			}

			GUI.Box(new Rect(0, 0, PlayerPaneSize.x, PlayerPaneSize.y), "", Styles.MainBox);

			List<PlayerControl> selectedList = new();

			for(byte i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
			{
				PlayerControl player = PlayerControl.AllPlayerControls[i];
				// Wait for player data to fully load
				if(player.Data == null) continue;

				if(selectedPlayerIds.Contains(player.PlayerId))
				{
					selectedList.Add(player);
				}

				RenderPlayerSelection(i, player);
			}

			// Auto-select first player if nothing is picked
			if(selectedList.Count == 0 && PlayerControl.AllPlayerControls.Count > 0)
			{
				var first = PlayerControl.AllPlayerControls[0];
				if(first?.Data != null)
				{
					selectedPlayerIds.Add(first.PlayerId);
					selectedPlayer = first;
					selectedList.Add(first);
				}
			}

			GUILayout.BeginArea(new Rect(PlayerPaneSize.x, 0, PlayerOptionsSize.x, PlayerOptionsSize.y));
			subsectionScrollVector = GUILayout.BeginScrollView(subsectionScrollVector);

			if(selectedList.Count > 1)
			{
				RenderMultiPlayerControls(selectedList);
			}
			else if(selectedList.Count == 1)
			{
				selectedPlayer = selectedList[0];
				RenderPlayerControls(selectedList[0]);
			}
			else
			{
				GUILayout.Label("Select a player on the left.\n<color=#888888>(Hold Ctrl to select multiple)</color>");
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void RenderPlayerSelection(byte position, PlayerControl player)
		{
			Rect playerInfo = new Rect(0, position * PlayerButtonSize.y, PlayerButtonSize.x, PlayerButtonSize.y);

			string playerName = player.Data.PlayerName;
			playerName += $"\n<color=\"{GetRoleColor(player.Data.RoleType)}\">{player.Data.RoleType}</color>";

			bool isSelected = selectedPlayerIds.Contains(player.PlayerId);
			GUIStyle style = isSelected ? Styles.PlayerBoxActive : Styles.PlayerBox;

			if(AmongUsClient.Instance != null && player.OwnerId == AmongUsClient.Instance.HostId)
			{
				style.normal.textColor = new Color(1.0f, 0.84f, 0.0f); // #FFD700
			}

			if(GUI.Button(playerInfo, playerName, style))
			{
				bool isCtrl = (Event.current != null && Event.current.control) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
				if(isCtrl)
				{
					if(selectedPlayerIds.Contains(player.PlayerId))
					{
						selectedPlayerIds.Remove(player.PlayerId);
						if(selectedPlayer == player)
						{
							selectedPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.Data != null && selectedPlayerIds.Contains(p.PlayerId));
						}
					}
					else
					{
						selectedPlayerIds.Add(player.PlayerId);
						selectedPlayer = player;
					}
				}
				else
				{
					selectedPlayerIds.Clear();
					selectedPlayerIds.Add(player.PlayerId);
					selectedPlayer = player;
				}
			}

			Rect playerColor = new Rect(0, position * PlayerButtonSize.y, PlayerColorBoxSize.x, PlayerColorBoxSize.y);
			Controls.DrawCrewmateColorBox(playerColor, player.Data);
		}

		private string GetRoleColor(RoleTypes role)
		{
			return RoleManager.IsImpostorRole(role) ? "red" : "#8afcfc";
		}

		private void RenderPlayerControls(PlayerControl target)
		{
			if(target == null || target.Data == null)
			{
				GUILayout.Label("Specified target is not valid.");
				return;
			}

			bool hasAnticheat = Utilities.IsAnticheatPresent();

			string playerInfo =
				$"Name: {target.Data.PlayerName} ({Utilities.GetPlayerColor(target.Data)})" +
				$"\nRole: {target.Data.RoleType}" +
				$"\nState: " + (target.Data.IsDead ? "Dead" : "Alive");

			ClientData clientData = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(target) : null;
			if(clientData != null)
			{
				var platform = clientData.PlatformData;
				bool streamerMode = DataManager.Settings.Gameplay.StreamerMode;
				string friendCodeStr = !string.IsNullOrEmpty(target.Data.FriendCode) ? target.Data.FriendCode : (clientData.FriendCode ?? "-");
				string puidStr = !string.IsNullOrEmpty(clientData.ProductUserId) ? clientData.ProductUserId : "-";

				playerInfo +=
					$"\nFriendcode: " + (streamerMode ? "REDACTED" : friendCodeStr) +
					$"\nLevel: {target.Data.PlayerLevel + 1}" +
					$"\nDevice: {platform?.Platform}" +
					(AmongUsClient.Instance != null && target.OwnerId == AmongUsClient.Instance.HostId ? "\nHost: true" : "");
			}

			GUILayout.Label(playerInfo);

			Visuals.SpectatePlayer.Enabled = Controls.PlayerSpecificToggle("Spectate", target, ref Visuals.SpectatePlayer.target);
			Hydra.routines.petPlayer.Enabled = Controls.PlayerSpecificToggle("Pet Player", target, ref Hydra.routines.petPlayer.target);
			Hydra.routines.playerFollower.Enabled = Controls.PlayerSpecificToggle("Follow", target, ref Hydra.routines.playerFollower.following);
			Hydra.routines.jailPlayer.Enabled = Controls.PlayerSpecificToggle("Place in Jail", target, ref Hydra.routines.jailPlayer.targets);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Teleport"))
			{
				Teleporter.TeleportTo(target.transform.position);
			}

			if(GUILayout.Button("Teleport to Me"))
			{
				if(AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null) { }
				else if(AmongUsClient.Instance.AmHost || !hasAnticheat)
				{
					Teleporter.TeleportPlayerTo(target, PlayerControl.LocalPlayer.transform.position);
				}
				else
				{
					Hydra.notifications.Send("Teleport", "Teleporting other players requires Host authority on official servers.");
				}
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Teleport All To"))
			{
				if(AmongUsClient.Instance == null) { }
				else if(AmongUsClient.Instance.AmHost || !hasAnticheat)
				{
					Teleporter.TeleportAllTo(target.transform.position);
				}
				else
				{
					Hydra.notifications.Send("Teleport", "Teleporting all players requires Host authority on official servers.");
				}
			}

			if(GUILayout.Button("Murder"))
			{
				AttemptMurder(target);
			}

			if(GUILayout.Button("Copy Avatar"))
			{
				Utilities.CopyPlayer(target);
			}

			if(GUILayout.Button("Report Body"))
			{
				Utilities.AttemptStartMeeting(PlayerControl.LocalPlayer, target.Data);
			}

			if(GUILayout.Button("Kick Player"))
			{
				Utilities.KickPlayer(target);
			}

			Dictionary<int, string> vents = MapAssets.GetVents();

			int ventCount = vents != null && vents.Count > 0 ? vents.Count : (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null ? ShipStatus.Instance.AllVents.Count : 0);
			string ventName = vents != null && vents.ContainsKey(selectedVent) ? vents[selectedVent] : selectedVent.ToString();
			GUILayout.Label($"Teleport player to vent: {ventName}");
			selectedVent = (int)GUILayout.HorizontalSlider(selectedVent, 0, Math.Max(0, ventCount - 1));
			if(GUILayout.Button("Teleport") && ventCount > 0)
			{
				Teleporter.TeleportToVent(target, selectedVent);
			}

			GUILayout.Space(5);
			GUILayout.Label("Host Only Features:" + (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? "" : "\n(Using these will get you kicked!)"));

			Troll.AutoReportBodies.Enabled = Controls.PlayerSpecificToggle("Auto Report Bodies As", target, ref Troll.AutoReportBodies.source);
			Hydra.routines.discoHost.Enabled = Controls.PlayerSpecificToggle("Disco Mode", target, ref Hydra.routines.discoHost.targets);

			if(GUILayout.Button("Force Meeting As"))
			{
				Utilities.AttemptStartMeeting(target, null);
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Force All Votes To"))
			{
				if(MeetingHud.Instance == null)
				{
					Hydra.notifications.Send("Vote Forcer", "This option can only be used when there is an active meeting.");
				}
				else
				{
					MeetingHud.VoterState[] array = new MeetingHud.VoterState[PlayerControl.AllPlayerControls.Count];

					for(int i = 0; i < array.Length; i++)
					{
						MeetingHud.VoterState state = array[i];

						state.VoterId = (byte)i;
						state.VotedForId = target.PlayerId;

						array[i] = state;
					}

					BatchedMessage batch = new BatchedMessage();
					batch.QueueVotingComplete(array, target.Data, false);
					batch.FinishBatch();
				}
			}

			if(GUILayout.Button("Eject"))
			{
				BatchedMessage batch = new BatchedMessage();

				if(MeetingHud.Instance == null)
				{
					MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(HudManager.Instance.MeetingPrefab);
				}

				MeetingHud.VoterState[] votes = Array.Empty<MeetingHud.VoterState>();

				batch.QueueVotingComplete(votes, target.Data, false);
				batch.QueueCloseMeeting();
				batch.FinishBatch();
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Frame Shapeshift"))
			{
				PlayerControl randomPl = Utilities.GetRandomPlayer(false, false, false, false);
				Utilities.ShapeshiftPlayer(target, randomPl);
			}

			if(GUILayout.Button("Frame for Killing All"))
			{
				target.StartCoroutine(AttemptFrameForKillingAll(target).WrapToIl2Cpp());
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Flood Player with Tasks"))
			{
				byte[] taskIds = new byte[255];

				for(byte i = 0; i < 255; i++)
				{
					taskIds[i] = i;
				}

				target.Data.RpcSetTasks(taskIds);
			}

			if(GUILayout.Button("Clear Tasks"))
			{
				target.Data.RpcSetTasks(Array.Empty<byte>());
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Game Options Modifier:");

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Blind"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
					gameOptions.SetFloat(FloatOptionNames.CrewLightMod, -1.0f);
					gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, -1.0f);

					GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
				}
			}

			if(GUILayout.Button("Fullbright"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
					gameOptions.SetFloat(FloatOptionNames.CrewLightMod, 1000f);
					gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, 1000f);

					GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Slow Speed"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
					gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.1f);

					GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
				}
			}

			if(GUILayout.Button("Super Speed"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					float maxSpeed = Utilities.IsAnticheatPresent() ? 3.0f : 5.0f;

					IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
					gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, maxSpeed);

					GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
				}
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Reset to Defaults"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
					GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
				}
			}

			GUILayout.Space(5);
			GUILayout.Label($"Change color to: {selectedColor}");
			selectedColor = Controls.HorizontalColorSlider(selectedColor);

			if(GUILayout.Button("Set Color"))
			{
				target.RpcSetColor((byte)selectedColor);
			}
		}

		private void RenderMultiPlayerControls(List<PlayerControl> targets)
		{
			bool hasAnticheat = Utilities.IsAnticheatPresent();

			GUILayout.BeginHorizontal();
			GUILayout.Label($"<b>{targets.Count} Players Selected</b>");
			if(GUILayout.Button("Deselect All", GUILayout.Width(90)))
			{
				selectedPlayerIds.Clear();
				GUILayout.EndHorizontal();
				return;
			}
			GUILayout.EndHorizontal();

			string chips = string.Join(", ", targets.Where(p => p != null && p.Data != null).Select(p => $"<color=\"{GetRoleColor(p.Data.RoleType)}\">{p.Data.PlayerName}</color>"));
			GUILayout.Label($"Targets: {chips}");

			GUILayout.Space(5);
			GUILayout.Label("General Multi-Target Actions:");

			GUILayout.BeginHorizontal();
			if(GUILayout.Button($"Murder Selected ({targets.Count})"))
			{
				foreach(var target in targets)
				{
					AttemptMurder(target);
				}
			}

			if(GUILayout.Button("Teleport All to Me"))
			{
				if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null) { }
				else if(AmongUsClient.Instance.AmHost || !hasAnticheat)
				{
					foreach(var target in targets)
					{
						Teleporter.TeleportPlayerTo(target, PlayerControl.LocalPlayer.transform.position);
					}
				}
				else
				{
					Hydra.notifications.Send("Teleport", "Teleporting other players requires Host authority on official servers.");
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			bool allJailed = targets.All(t => Hydra.routines.jailPlayer.targets.Contains(t.GetHashCode()));
			if(GUILayout.Button(allJailed ? "Release All from Jail" : "Place All in Jail"))
			{
				if(allJailed)
				{
					foreach(var t in targets) Hydra.routines.jailPlayer.targets.Remove(t.GetHashCode());
				}
				else
				{
					foreach(var t in targets) Hydra.routines.jailPlayer.targets.Add(t.GetHashCode());
				}
				Hydra.routines.jailPlayer.Enabled = Hydra.routines.jailPlayer.targets.Count > 0;
			}

			if(GUILayout.Button("Kick Selected"))
			{
				foreach(var target in targets)
				{
					Utilities.KickPlayer(target);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Label($"Teleport all selected to vent: {selectedVent}");
			selectedVent = (int)GUILayout.HorizontalSlider(selectedVent, 0, ShipStatus.Instance?.AllVents != null ? ShipStatus.Instance.AllVents.Count - 1 : 10);
			if(GUILayout.Button("Teleport to Vent"))
			{
				foreach(var target in targets)
				{
					Teleporter.TeleportToVent(target, selectedVent);
				}
			}

			GUILayout.Space(8);
			GUILayout.Label("Host Only Features:" + (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? "" : "\n(Using these will get you kicked!)"));

			bool allDisco = targets.All(t => Hydra.routines.discoHost.targets.Contains(t.GetHashCode()));
			if(GUILayout.Button(allDisco ? "Disable Disco Mode" : "Enable Disco Mode on Selected"))
			{
				if(allDisco)
				{
					foreach(var t in targets) Hydra.routines.discoHost.targets.Remove(t.GetHashCode());
				}
				else
				{
					foreach(var t in targets) Hydra.routines.discoHost.targets.Add(t.GetHashCode());
				}
				Hydra.routines.discoHost.Enabled = Hydra.routines.discoHost.targets.Count > 0;
			}

			if(GUILayout.Button("Eject Selected"))
			{
				BatchedMessage batch = new BatchedMessage();
				if(MeetingHud.Instance == null)
				{
					MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(HudManager.Instance.MeetingPrefab);
				}

				foreach(var target in targets)
				{
					batch.QueueVotingComplete(Array.Empty<MeetingHud.VoterState>(), target.Data, false);
				}
				batch.QueueCloseMeeting();
				batch.FinishBatch();
			}

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Flood Tasks"))
			{
				byte[] taskIds = new byte[255];
				for(byte i = 0; i < 255; i++) taskIds[i] = i;

				foreach(var target in targets)
				{
					target.Data.RpcSetTasks(taskIds);
				}
			}

			if(GUILayout.Button("Clear Tasks"))
			{
				foreach(var target in targets)
				{
					target.Data.RpcSetTasks(Array.Empty<byte>());
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(4);
			GUILayout.Label("Host Options Modifier (Selected):");

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Blind"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					foreach(var target in targets)
					{
						IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
						gameOptions.SetFloat(FloatOptionNames.CrewLightMod, -1.0f);
						gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, -1.0f);
						GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
					}
				}
			}

			if(GUILayout.Button("Fullbright"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					foreach(var target in targets)
					{
						IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
						gameOptions.SetFloat(FloatOptionNames.CrewLightMod, 1000f);
						gameOptions.SetFloat(FloatOptionNames.ImpostorLightMod, 1000f);
						GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Slow Speed"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					foreach(var target in targets)
					{
						IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
						gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 0.1f);
						GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
					}
				}
			}

			if(GUILayout.Button("Super Speed"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					float maxSpeed = Utilities.IsAnticheatPresent() ? 3.0f : 5.0f;
					foreach(var target in targets)
					{
						IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
						gameOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, maxSpeed);
						GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
					}
				}
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Reset Options to Defaults"))
			{
				if (GameManager.Instance?.LogicOptions?.currentGameOptions != null)
				{
					foreach(var target in targets)
					{
						IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
						GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
					}
				}
			}

			GUILayout.Space(4);
			GUILayout.Label($"Change color of all selected to: {selectedColor}");
			selectedColor = Controls.HorizontalColorSlider(selectedColor);

			if(GUILayout.Button("Set Color"))
			{
				foreach(var target in targets)
				{
					target.RpcSetColor((byte)selectedColor);
				}
			}
		}

		private static void AttemptMurder(PlayerControl target)
		{
			if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
			bool hasAnticheat = Utilities.IsAnticheatPresent();

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Hydra.notifications.Send("Murder Player", $"You can only kill players once the game has started.");
				return;
			}

			if(AmongUsClient.Instance.AmHost)
			{
				Hydra.Log.LogInfo($"Attempting to murder {target.Data.PlayerName}, we are the host so we can use the MurderPlayer RPC");
				PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
				Hydra.notifications.Send("Murder Player", $"Killed {target.Data.PlayerName}.", 5);
				return;
			}

			if(!hasAnticheat)
			{
				Hydra.Log.LogInfo($"Attempting to murder {target.Data.PlayerName}, we are are in a host-authoritative lobby so we can use the MurderPlayer RPC");
				PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
				Hydra.notifications.Send("Murder Player", $"Killed {target.Data.PlayerName}.", 5);
				return;
			}

			Hydra.Log.LogInfo($"Attempting to kill {target.Data.PlayerName}, we are not the host so we have to use the CheckMurder RPC");

			if(!RoleManager.IsImpostorRole(PlayerControl.LocalPlayer.Data.RoleType))
			{
				Hydra.notifications.Send("Murder Player", "You can only murder players when you are an Impostor, unless you are the host of the lobby.");
				return;
			}

			if(MeetingHud.Instance != null)
			{
				Hydra.notifications.Send("Murder Player", "You can only murder players outside of meetings, unless you are the host of the lobby.");
				return;
			}

			Hydra.notifications.Send("Murder Player", $"Attempted to kill {target.Data.PlayerName}.", 5);
			PlayerControl.LocalPlayer.CmdCheckMurder(target);
		}

		private static IEnumerator AttemptFrameForKillingAll(PlayerControl target)
		{
			if (AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null) yield break;
			Hydra.Log.LogInfo($"Attempting to frame {target.Data.PlayerName} for killing all players...");

			bool hasAnticheat = Utilities.IsAnticheatPresent();
			if(hasAnticheat && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Framer", "You must be the host of the lobby in order to use this option.");
				yield break;
			}

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Hydra.notifications.Send("Framer", "The game must have started for this option for this option to work.");
				yield break;
			}

			Host.DisableGameEnd.Enabled = true;

			if(target != PlayerControl.LocalPlayer)
			{
				Utilities.ShapeshiftPlayer(PlayerControl.LocalPlayer, target, false);
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == target) continue;

				PlayerControl.LocalPlayer.RpcMurderPlayer(player, true);
			}

			yield return Effects.Wait(3.0f);

			Host.DisableGameEnd.Enabled = false;
			Hydra.notifications.Send("Framer", $"Framed {target.Data.PlayerName} for killing all players!");
		}
	}
}