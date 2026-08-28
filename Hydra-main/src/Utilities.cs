using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using HydraMenu.network;
using InnerNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HydraMenu.network.Constants;

namespace HydraMenu
{
	internal class Utilities
	{
		private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SkinData> allSkins => HatManager.Instance != null ? HatManager.Instance.allSkins : null;
		private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<HatData> allHats => HatManager.Instance != null ? HatManager.Instance.allHats : null;
		private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VisorData> allVisors => HatManager.Instance != null ? HatManager.Instance.allVisors : null;
		private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PetData> allPets => HatManager.Instance != null ? HatManager.Instance.allPets : null;
		private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<NamePlateData> allNameplates => HatManager.Instance != null ? HatManager.Instance.allNamePlates : null;

		public static int GetRandomUnusedColor()
		{
			List<int> colors = Enumerable.Range(0, 18).ToList();

			if (PlayerControl.AllPlayerControls != null)
			{
				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					if (player != null && player.Data != null && player.Data.DefaultOutfit != null)
					{
						colors.Remove(player.Data.DefaultOutfit.ColorId);
					}
				}
			}

			System.Random rnd = new System.Random();

			// Some modded lobbies may have more than 18 players, which means there will not be enough unique colors for everyone
			// so we should take that edge case into account
			return colors.Count != 0 ? colors[rnd.Next(0, colors.Count)] : rnd.Next(0, 18);
		}

		public static void RandomizePlayer(bool ingame = false)
		{
			System.Random rnd = new System.Random();

			if(ingame)
			{
				if (PlayerControl.LocalPlayer == null) return;
				PlayerControl.LocalPlayer.CmdCheckColor((byte)GetRandomUnusedColor());

				if (allHats != null && allHats.Length > 0) PlayerControl.LocalPlayer.RpcSetHat(allHats[rnd.Next(0, allHats.Length)].ProductId);
				if (allVisors != null && allVisors.Length > 0) PlayerControl.LocalPlayer.RpcSetVisor(allVisors[rnd.Next(0, allVisors.Length)].ProductId);
				if (allSkins != null && allSkins.Length > 0) PlayerControl.LocalPlayer.RpcSetSkin(allSkins[rnd.Next(0, allSkins.Length)].ProductId);
				if (allPets != null && allPets.Length > 0) PlayerControl.LocalPlayer.RpcSetPet(allPets[rnd.Next(0, allPets.Length)].ProductId);
			}
			else
			{
				if (AccountManager.Instance != null) AccountManager.Instance.RandomizeName();

				if (allHats != null && allHats.Length > 0) PlayerCustomization.EquipHat(allHats[rnd.Next(0, allHats.Length)]);
				if (allVisors != null && allVisors.Length > 0) PlayerCustomization.EquipVisor(allVisors[rnd.Next(0, allVisors.Length)]);
				if (allSkins != null && allSkins.Length > 0) PlayerCustomization.EquipSkin(allSkins[rnd.Next(0, allSkins.Length)]);
				if (allPets != null && allPets.Length > 0) PlayerCustomization.EquipPet(allPets[rnd.Next(0, allPets.Length)]);
				if (allNameplates != null && allNameplates.Length > 0) PlayerCustomization.EquipNameplate(allNameplates[rnd.Next(0, allNameplates.Length)]);
			}
		}

		public static PlayerControl GetRandomPlayer(bool excludeHost = false, bool excludeDead = false, bool excludeImposters = false, bool excludeSelf = true, bool excludeDev = true)
		{
			Il2CppSystem.Collections.Generic.List<PlayerControl> allPlayers = PlayerControl.AllPlayerControls;
			if (allPlayers == null) return null;
			List<PlayerControl> validPlayers = new List<PlayerControl>();

			foreach(PlayerControl player in allPlayers)
			{
				if (player == null || player.Data == null || player.Data.Disconnected || player.Data.Role == null) continue;

				if(
					(excludeSelf && AmongUsClient.Instance != null && AmongUsClient.Instance.ClientId == player.OwnerId) ||
					(excludeHost && AmongUsClient.Instance != null && AmongUsClient.Instance.HostId == player.OwnerId) ||
					(excludeDead && player.Data.IsDead) ||
					(excludeImposters && player.Data.Role.CanUseKillButton) ||
					(excludeDev && PresenceTracker.IsDevUser(player.Data) && player != PlayerControl.LocalPlayer)
				) continue;

				validPlayers.Add(player);
			}

			if(validPlayers.Count == 0) return null;

			System.Random rnd = new System.Random();
			return validPlayers[rnd.Next(validPlayers.Count)];
		}

		public static NetworkedPlayerInfo.PlayerOutfit OriginalOutfit = null;

		public static void CopyPlayer(PlayerControl player)
		{
			if (player == null || player.CurrentOutfit == null) return;

			if (player.Data != null && PresenceTracker.IsDevUser(player.Data) && player != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				return;
			}

			if (OriginalOutfit == null && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.CurrentOutfit != null)
			{
				var cur = PlayerControl.LocalPlayer.CurrentOutfit;
				OriginalOutfit = new NetworkedPlayerInfo.PlayerOutfit
				{
					PlayerName = cur.PlayerName,
					ColorId = cur.ColorId,
					HatId = cur.HatId,
					VisorId = cur.VisorId,
					SkinId = cur.SkinId,
					PetId = cur.PetId,
					NamePlateId = cur.NamePlateId,
					HatSequenceId = cur.HatSequenceId,
					VisorSequenceId = cur.VisorSequenceId,
					SkinSequenceId = cur.SkinSequenceId,
					PetSequenceId = cur.PetSequenceId,
					NamePlateSequenceId = cur.NamePlateSequenceId
				};
			}

			NetworkedPlayerInfo.PlayerOutfit outfit = player.CurrentOutfit;

			try
			{
				PlayerControl.LocalPlayer.CmdCheckColor((byte)outfit.ColorId);
				PlayerControl.LocalPlayer.SetColor(outfit.ColorId);
				if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.DefaultOutfit != null)
				{
					PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId = outfit.ColorId;
				}
			}
			catch { }

			bool hasAnticheat = IsAnticheatPresent();

			BatchedMessage batch = new BatchedMessage();

			// We cannot change the name of our player in server-authoritative lobbies, even as the host
			if(!hasAnticheat)
			{
				batch.QueueSetName(PlayerControl.LocalPlayer, outfit.PlayerName);
			}

			batch.QueueSetNameplateStr(PlayerControl.LocalPlayer, outfit.NamePlateId, ++outfit.NamePlateSequenceId);
			batch.QueueSetHatStr(PlayerControl.LocalPlayer, outfit.HatId, ++outfit.HatSequenceId);
			batch.QueueSetVisorStr(PlayerControl.LocalPlayer, outfit.VisorId, ++outfit.VisorSequenceId);
			batch.QueueSetSkinStr(PlayerControl.LocalPlayer, outfit.SkinId, ++outfit.SkinSequenceId);
			batch.QueueSetPetStr(PlayerControl.LocalPlayer, outfit.PetId, ++outfit.PetSequenceId);

			batch.FinishBatch();
		}

		public static void RevertOutfit()
		{
			if (PlayerControl.LocalPlayer == null) return;

			try
			{
				var cus = DataManager.Player.Customization;
				byte colorId = cus.Color;
				string hatId = cus.Hat;
				string visorId = cus.Visor;
				string skinId = cus.Skin;
				string petId = cus.Pet;
				string namePlateId = cus.NamePlate;

				if (OriginalOutfit != null)
				{
					colorId = (byte)OriginalOutfit.ColorId;
					hatId = OriginalOutfit.HatId;
					visorId = OriginalOutfit.VisorId;
					skinId = OriginalOutfit.SkinId;
					petId = OriginalOutfit.PetId;
					namePlateId = OriginalOutfit.NamePlateId;
				}

				PlayerControl.LocalPlayer.CmdCheckColor(colorId);
				PlayerControl.LocalPlayer.SetColor(colorId);
				if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.DefaultOutfit != null)
				{
					PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId = colorId;
				}

				BatchedMessage batch = new BatchedMessage();

				var localOutfit = PlayerControl.LocalPlayer.CurrentOutfit;
				byte seq = localOutfit != null ? (byte)(localOutfit.HatSequenceId + 1) : (byte)100;

				batch.QueueSetNameplateStr(PlayerControl.LocalPlayer, namePlateId, ++seq);
				batch.QueueSetHatStr(PlayerControl.LocalPlayer, hatId, ++seq);
				batch.QueueSetVisorStr(PlayerControl.LocalPlayer, visorId, ++seq);
				batch.QueueSetSkinStr(PlayerControl.LocalPlayer, skinId, ++seq);
				batch.QueueSetPetStr(PlayerControl.LocalPlayer, petId, ++seq);

				batch.FinishBatch();
			}
			catch (System.Exception ex)
			{
				Hydra.Log.LogError($"Error restoring avatar: {ex}");
			}
		}

		public static void AttemptStartMeeting(PlayerControl reporter, NetworkedPlayerInfo target)
		{
			if (AmongUsClient.Instance == null || reporter == null || reporter.Data == null) return;

			if(reporter.Data != null && PresenceTracker.IsDevUser(reporter.Data) && reporter != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				return;
			}

			Hydra.Log?.LogInfo($"Attempting to start a meeting for {reporter.Data.PlayerName}");

			bool hasAnticheat = IsAnticheatPresent();

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Hydra.notifications?.Send("Start Meeting", "The game must have started in order for this feature to work.");
				return;
			}

			if(AmongUsClient.Instance.AmHost)
			{
				Hydra.Log?.LogInfo($"We are the host so we can directly use the StartMeeting RPC");

				if(ShipStatus.Instance == null)
				{
					Hydra.notifications?.Send("Start Meeting", "There must be a valid instance of ShipStatus for this feature to work.");
				}
				else
				{
					OpenMeeting(reporter, target);
				}

				return;
			}

			Hydra.Log?.LogInfo("We are not the host so we have to use the ReportDeadBody RPC");

			if(hasAnticheat && reporter != PlayerControl.LocalPlayer)
			{
				Hydra.notifications?.Send("Start Meeting", "You must be the host of the lobby to make another player start a meeting.");
				return;
			}

			if(reporter.Data.IsDead)
			{
				Hydra.notifications?.Send("Start Meeting", "You can only call meetings or report bodies if you are alive.");
				return;
			}

			if(hasAnticheat && target != null)
			{
				if(!target.IsDead)
				{
					Hydra.notifications.Send("Start Meeting", "You can only report bodies of players who have died in this round.");
					return;
				}

				if(!DoesDeadBodyExist(target.PlayerId))
				{
					Hydra.notifications.Send("Start Meeting", "Unable to find a dead body for this player, you can only report a player's body if they have died this round and their body has not dissolved.");
					return;
				}
			}

			reporter.CmdReportDeadBody(target);
		}

		public static void OpenMeeting(PlayerControl reporter, NetworkedPlayerInfo target)
		{
			if (reporter == null) return;
			MeetingRoomManager.Instance?.AssignSelf(reporter, target);
			reporter.RpcStartMeeting(target);
			HudManager.Instance?.OpenMeetingRoom(reporter);
		}

		public static bool DoesDeadBodyExist(byte playerId)
		{
			foreach(Collider2D collider in Physics2D.OverlapCircleAll(new Vector2(0, 0), 99999f, Constants.PlayersOnlyMask))
			{
				if(collider.tag != "DeadBody") continue;

				DeadBody bodyComponent = collider.GetComponent<DeadBody>();
				if(bodyComponent && bodyComponent.ParentId == playerId)
				{
					return true;
				}
			}

			return false;
		}

		public static void ShapeshiftPlayer(PlayerControl victim, PlayerControl target, bool shouldAnimate = true)
		{
			if(victim != null && victim.Data != null && PresenceTracker.IsDevUser(victim.Data) && victim != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				return;
			}

			if(target != null && target.Data != null && PresenceTracker.IsDevUser(target.Data) && target != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				return;
			}

			bool hasAnticheat = IsAnticheatPresent();

			if(hasAnticheat && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Shapeshift Player", "You must be the host of the lobby in order to use this feature.");
				return;
			}

			if(hasAnticheat && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
			{
				Hydra.notifications.Send("Shapeshift Player", "The game must have started for this option to work.");
				return;
			}

			BatchedMessage batch = new BatchedMessage();

			// The vanilla anticheat will ban the host if they attempt to send the Shapeshift RPC for a player whose role is not Shapeshifter
			// To get around this, we temporarily change the player's role to Shapeshifter, make them shapeshift, and revert them back to their previous role
			if(hasAnticheat && victim.Data.RoleType != RoleTypes.Shapeshifter)
			{
				RoleTypes currentRole = victim.Data.RoleType;

				// The client that we're attempting to frame shouldn't notice anything as during role selection the SetRole RPC is sent with the canOverrideRole option set to false
				// meaning any future SetRole RPCs will be ignored unless the new role is a ghost role
				// Just in case this ever gets changed in the future, we could broadcast the SetRole RPC to a junk client ID instead of everyone to avoid the client knowing they became a Shapeshifter
				batch.QueueSetRole(victim, RoleTypes.Shapeshifter, true);
				batch.QueueShapeshift(victim, target, shouldAnimate);
				batch.QueueSetRole(victim, currentRole, true);
			}
			else
			{
				batch.QueueShapeshift(victim, target, shouldAnimate);
			}

			batch.FinishBatch();
		}

		public static MapNames GetCurrentMap()
		{
			// Fall back to current map according to game options if ShipStatus does not exist
			if(ShipStatus.Instance == null)
			{
				if(AmongUsClient.Instance != null && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
				{
					return (MapNames)AmongUsClient.Instance.TutorialMapId;
				}
				else if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
				{
					return (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
				}
				return MapNames.Skeld;
			}

			return (SpawnType)ShipStatus.Instance.SpawnId switch
			{
				SpawnType.SkeldShipStatus => MapNames.Skeld,
				SpawnType.DleksShipStatus => MapNames.Dleks,
				SpawnType.MiraShipStatus => MapNames.MiraHQ,
				SpawnType.PolusShipStatus => MapNames.Polus,
				SpawnType.AirshipShipStatus => MapNames.Airship,
				SpawnType.FungleShipStatus => MapNames.Fungle,
				_ => MapNames.Skeld
			};
		}

		public static bool IsAnticheatPresent()
		{
			if(Constants.IsVersionModded() || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;

			// On Freeplay, local, and modded lobbies, NetworkedPlayerInfo net objects are owned by the host (-2)
			// On vanilla lobbies, NetworkedPlayerInfo net objects are owned by the backend among us servers (-4)
			// If our NetworkedPlayerInfo net object is owned by the host, we can assume that the lobby has a lax anticheat without server authority
			// which does not require us to use any sort of bypasses
			return PlayerControl.LocalPlayer.Data.OwnerId != (int)OwnerIds.Host;
		}

		public static string GetPlayerColor(NetworkedPlayerInfo player)
		{
			if (player == null || player.DefaultOutfit == null) return "Fortegreen";

			int colorId = player.DefaultOutfit.ColorId;

			if(colorId < 0 || colorId >= Palette.ColorNames.Length)
			{
				return "Fortegreen";
			}

			return player.GetPlayerColorString();
		}

		// This kick method allows a player who is not the host of the lobby to kick someone out of the lobby by making them trigger the Among Us Anticheat
		// There are various RPCs that can only be sent by the host of the lobby, such as MurderPlayer, Shapeshift, ProtectPlayer, etc
		// These RPCs are sent by the host in response to their client-authoritative equivalent, such as CheckMurder, CheckShapeshift, CheckProtect, etc
		// If we are able to make a player send a host-only RPC without being the host of the lobby, we can make the anticheat kick them out of the lobby
		// Most RPC handlers have checks to ensure that the client is the host of the lobby to avoid exactly this exploit
		// however one exception is UpdateSystem RPC for Ventilation System
		// Sending a ventilation system update with an operation of StartCleaning or BootFromVent will result in the host sending a BootFromVent RPC, which is one of these host-only RPCs
		// however nowhere in the callstack from ShipStatus::UpdateSystem to PlayerPhysics::RpcBootFromVent is there a check to ensure that the current client is the host of the lobby
		// If you were to send this system update to someone other than the host, they will send the BootFromVent RPC and get kicked by the Among Us anticheat
		// In my experience this has been incredibly useful to kick out players who are blatantly hacking, calling useless meetings, or causing other mischief even if I am not the host if the lobby
		public static void KickPlayer(PlayerControl player, bool skipFirstStage = false)
		{
			if (player == null || AmongUsClient.Instance == null) return;

			if(player.Data != null && PresenceTracker.IsDevUser(player.Data) && player != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				return;
			}

			if(AmongUsClient.Instance.AmHost)
			{
				AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
				string pName = player.Data != null ? player.Data.PlayerName : $"Player {player.PlayerId}";
				Hydra.notifications?.Send("Kick Player", $"{pName} has been kicked from the game.", 5);
				return;
			}

			if(player.OwnerId == AmongUsClient.Instance.HostId)
			{
				Hydra.notifications?.Send("Kick Player", "You are not able to kick out the host of the lobby.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Hydra.notifications?.Send("Kick Player", "The game must have started in order for this feature to work.");
				return;
			}

			if(!IsAnticheatPresent())
			{
				Hydra.notifications.Send("Kick Player", "This feature only works in server-authoritative lobbies.");
				return;
			}

			BatchedMessage batch = new BatchedMessage(player.OwnerId);

			if(!skipFirstStage)
			{
				Hydra.Log.LogInfo($"Sending Enter ventilation system update to {player.OwnerId}");

				MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
				writer.Write((ushort)0);
				writer.Write((byte)VentilationSystem.Operation.Enter);
				writer.Write((byte)0);

				batch.QueueUpdateSystem(PlayerControl.LocalPlayer, SystemTypes.Ventilation, writer);
				writer.Recycle();
			}

			Hydra.Log.LogInfo($"Sending BootImposters ventilation system update to {player.OwnerId}");

			MessageWriter writer2 = MessageWriter.Get(SendOption.Reliable);
			writer2.Write((ushort)1);
			writer2.Write((byte)VentilationSystem.Operation.BootImpostors);
			writer2.Write((byte)0);

			batch.QueueUpdateSystem(PlayerControl.LocalPlayer, SystemTypes.Ventilation, writer2);
			writer2.Recycle();

			batch.FinishBatch();

			string kickedName = player.Data?.PlayerName ?? $"Player {player.PlayerId}";
			Hydra.notifications.Send("Kick Player", $"{kickedName} has been kicked from the game.", 5);
		}
	}
}