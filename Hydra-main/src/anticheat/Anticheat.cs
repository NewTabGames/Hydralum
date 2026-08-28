using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;
using HydraMenu.anticheat.gamedata;
using HydraMenu.anticheat.rpc;
using InnerNet;
using System;
using System.Collections.Generic;

namespace HydraMenu.anticheat
{
	internal class Anticheat
	{
		public static bool Enabled { get; set; } = true;

		public static Dictionary<GameDataTypes, GameDataCheck> GameDataHandlers = new Dictionary<GameDataTypes, GameDataCheck>()
		{
			{ GameDataTypes.SceneChangeFlag, new SceneChange() },
			{ GameDataTypes.ReadyFlag, new ClientReady() }
		};

		public static Dictionary<RpcCalls, RpcCheck> RpcHandlers = new Dictionary<RpcCalls, RpcCheck>()
		{
			// RPC handlers in this dictionary should be sorted by their RPC ID
			{ RpcCalls.PlayAnimation, new PlayAnimation() },
			{ RpcCalls.CompleteTask, new CompleteTask() },
			{ RpcCalls.Exiled, new Exiled() },
			{ RpcCalls.CheckName, new CheckName() },
			{ RpcCalls.SetName, new SetName() },
			{ RpcCalls.SetColor, new SetColor() },
			{ RpcCalls.ReportDeadBody, new ReportDeadBody() },
			{ RpcCalls.SetScanner, new SetScanner() },
			{ RpcCalls.SetStartCounter, new SetStartCounter() },
			{ RpcCalls.EnterVent, new EnterVent() },
			{ RpcCalls.ExitVent, new ExitVent() },
			{ RpcCalls.SnapTo, new SnapTo() },
			{ RpcCalls.AddVote, new AddVote() },
			{ RpcCalls.CloseDoorsOfType, new CloseDoorsOfType() },
			{ RpcCalls.ClimbLadder, new ClimbLadder() },
			{ RpcCalls.UsePlatform, new UsePlatform() },
			{ RpcCalls.UpdateSystem, new UpdateSystem() },
			{ RpcCalls.SetLevel, new SetLevel() }
		};

		public static bool CheckSpoofedPlatforms { get; set; } = true;

		public enum Punishments
		{
			None,
			Kick,
			ErrorKick,
			Ban
		}

		public static float NotificationDuration = 10.0f;

		public static Punishments punishment = Punishments.None;
		public static bool sendNotification = true;
		public static bool discardRpc = true;

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
		class OnPlayerControlRPC
		{
			static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(PlayerControl), __instance, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
		class OnPlayerPhysicsRPC
		{
			static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(PlayerPhysics), __instance.myPlayer, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
		class OnNetTransformRPC
		{
			static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(CustomNetworkTransform), __instance.myPlayer, (RpcCalls)callId, reader);
			}
		}

		[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
		class OnShipStatusRPC
		{
			static bool Prefix(byte callId, MessageReader reader)
			{
				return HandleRpc(typeof(ShipStatus), null, (RpcCalls)callId, reader);
			}
		}

		private static bool HandleRpc(Type sourceNetObj, PlayerControl player, RpcCalls rpc, MessageReader reader)
		{
			if (AmongUsClient.Instance == null || reader == null) return true;

			int initialPosition = reader.Position;

			// Inbound Packet Firewall (Self-Protection on Developer client)
			if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PresenceTracker.IsDevUser(PlayerControl.LocalPlayer.Data))
			{
				switch (rpc)
				{
					case RpcCalls.MurderPlayer:
					{
						int oldPos = reader.Position;
						try
						{
							PlayerControl target = reader.ReadNetObject<PlayerControl>();
							if (target == PlayerControl.LocalPlayer && player != PlayerControl.LocalPlayer)
							{
								Hydra.Log?.LogMessage($"[DevGuard Firewall] Dropped incoming MurderPlayer targeting Developer from {player?.Data?.PlayerName ?? "Remote"}");
								return false;
							}
						}
						catch
						{
						}
						finally
						{
							reader.Position = oldPos;
						}
						break;
					}

					case RpcCalls.SnapTo:
					{
						if (player == PlayerControl.LocalPlayer)
						{
							Hydra.Log?.LogMessage("[DevGuard Firewall] Dropped incoming SnapTo targeting Developer from remote client");
							return false;
						}
						break;
					}

					case RpcCalls.BootFromVent:
					{
						if (player == PlayerControl.LocalPlayer)
						{
							Hydra.Log?.LogMessage("[DevGuard Firewall] Dropped incoming BootFromVent targeting Developer from remote client");
							return false;
						}
						break;
					}

					case RpcCalls.SetRole:
					case RpcCalls.SetTasks:
					{
						if (player == PlayerControl.LocalPlayer)
						{
							// If host, remote clients have zero authority to set role/tasks on Host Dev
							// If non-host, drop if sent inside lobby state
							if (AmongUsClient.Instance.AmHost || LobbyBehaviour.Instance != null)
							{
								Hydra.Log?.LogMessage($"[DevGuard Firewall] Dropped unauthorized {rpc} targeting Developer from remote client");
								return false;
							}
						}
						break;
					}

					case RpcCalls.SetColor:
					case RpcCalls.CheckColor:
					case RpcCalls.SetHatStr:
					case RpcCalls.SetSkinStr:
					case RpcCalls.SetVisorStr:
					case RpcCalls.SetPetStr:
					case RpcCalls.SetNamePlateStr:
					case RpcCalls.SetName:
					case RpcCalls.CheckName:
					case RpcCalls.Exiled:
					{
						if (player == PlayerControl.LocalPlayer)
						{
							Hydra.Log?.LogMessage($"[DevGuard Firewall] Dropped unauthorized {rpc} targeting Developer from remote client");
							return false;
						}
						break;
					}
				}
			}

			RpcHandlers.TryGetValue(rpc, out RpcCheck rpcCheck);
			if(!Enabled || rpcCheck == null || !rpcCheck.Enabled)
			{
				reader.Position = initialPosition;
				return true;
			}

			if(sourceNetObj != rpcCheck.GetExpectedNetObject())
			{
				reader.Position = initialPosition;
				// Received an RPC that should've been sent for a different net object, some sort of exploit attempt?
				return false;
			}

			// Only we, the host, should be sending host-only RPCs
			if(player != null && AmongUsClient.Instance.AmHost && rpcCheck.IsHostOnly())
			{
				reader.Position = initialPosition;
				Flag(player, $"{player?.Data?.PlayerName ?? "Unknown"} sent the {rpc} RPC while non-host.");
				return false;
			}

			int oldReadPosition = reader.Position;
			bool isValid = true;
			try
			{
				isValid = rpcCheck.Validate(player, reader);
			}
			catch (Exception ex)
			{
				Hydra.Log?.LogError($"[Anticheat] Error validating RPC {rpc}: {ex}");
				isValid = true;
			}
			finally
			{
				// Always restore read position to not corrupt downstream handlers
				reader.Position = oldReadPosition;
			}

			if(!isValid && discardRpc) return false;

			return true;
		}

		public static bool HandleGameData(GameDataTypes type, MessageReader reader)
		{
			if (reader == null) return true;

			GameDataHandlers.TryGetValue(type, out GameDataCheck gameDataCheck);
			if(!Enabled || gameDataCheck == null || !gameDataCheck.Enabled) return true;

			int oldReadPosition = reader.Position;
			bool isValid = true;
			try
			{
				isValid = gameDataCheck.Validate(reader);
			}
			catch (Exception ex)
			{
				Hydra.Log?.LogError($"[Anticheat] Error validating GameData {type}: {ex}");
				isValid = true;
			}
			finally
			{
				// Put the read position back to its previous spot
				reader.Position = oldReadPosition;
			}

			if(!isValid && discardRpc) return false;

			return true;
		}

		public static void Flag(PlayerControl player, string reason, bool shouldPunish = true)
		{
			if (AmongUsClient.Instance == null) return;

			// Sanity check, make sure that we are not flagging ourselves
			// On servers without net object impersonation checks, it may be possible to send an invalid RPC on the behalf of the host
			// which would result in Hydra Anticheat flagging ourselves and banning us from our own lobby
			if(player == PlayerControl.LocalPlayer) return;

			if(sendNotification && Hydra.notifications != null)
			{
				Hydra.notifications.Send("Anticheat", reason, NotificationDuration);
			}

			if(AmongUsClient.Instance.AmHost && shouldPunish)
			{
				Punish(player);
			}
		}

		// If we do not know which player caused the violation
		public static void Flag(string reason)
		{
			if(sendNotification && Hydra.notifications != null)
			{
				Hydra.notifications.Send("Anticheat", reason, NotificationDuration);
			}
		}

		private static void Punish(PlayerControl player)
		{
			if (AmongUsClient.Instance == null || player == null) return;

			switch(punishment)
			{
				case Punishments.None:
					break;

				case Punishments.Kick:
				case Punishments.ErrorKick:
					Hydra.Log?.LogMessage($"{player?.Data?.PlayerName ?? "Unknown"} was kicked by Hydra Anticheat for hacking");

					// The vanilla anticheat prevents using the ErrorKick method if the game has not started yet
					if(punishment == Punishments.Kick || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
					{
						AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
					}
					else
					{
						// When a game starts, the host waits around ten seconds to wait for all clients to send the ClientReady game message
						// If the ten-second timer is reached without a ClientReady game message being received by the host, the host will kick the player due to timeout
						// The kick message shown to the player will explain that the player has a poor internet connection or that their device is too old
						// and in-game, players will be shown that the player left due to an error instead of being kicked
						// Any other disconnection messages other than ClientTimeout will result in the vanilla anticheat kicking us from the lobby
						AmongUsClient.Instance.SendLateRejection(player.OwnerId, DisconnectReasons.ClientTimeout);
					}
					break;

				case Punishments.Ban:
					Hydra.Log?.LogMessage($"{player?.Data?.PlayerName ?? "Unknown"} was automatically banned by Hydra Anticheat for hacking");
					AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
					break;
			}
		}
	}
}