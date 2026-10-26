using HarmonyLib;

namespace HydraMenu.modules.visuals
{
	internal class AccurateDisconnectReason : Module
	{
		public AccurateDisconnectReason() : base("AccurateDisconnectReason")
		{
			base.Enabled = true;
		}

		private static AccurateDisconnectReason Instance
		{
			get { return ModuleManager.accurateDisconnectReason; }
		}

		// The GameData::ShowNotification function by default only handles disconnect reasons of ExitGame, Kicked, or Banned
		// Any other disconnection reasons automatically default to the error disconnection message
		[HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
		class ShowDisconnectNotification
		{
			static bool Prefix(string playerName, DisconnectReasons reason)
			{
				if(!Instance.Enabled) return true;

				switch(reason) {
					// GameData::ShowNotification already handles these disconnect messages
					case DisconnectReasons.ExitGame:
					case DisconnectReasons.Kicked:
					case DisconnectReasons.Banned:
					case DisconnectReasons.Error:
						return true;

					// Kick Player works by tripping the Among Us anticheat, which disconnects the target with
					// Hacking. Regular (non-Hydra) clients have no label for Hacking, so GameData shows them the
					// generic "left due to game error". Let the vanilla handler show that same message here too,
					// so our kicks look identical to everyone and don't stand out as an anticheat ban.
					case DisconnectReasons.Hacking:
						return true;

					case DisconnectReasons.DuplicateConnectionDetected:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to duplicate login.");
						return false;

					// This disconnect reason happens when a player does not send the ClientReady message after the game starts in time
					case DisconnectReasons.ClientTimeout:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked due to timeout.");
						return false;

					case DisconnectReasons.IntentionalLeaving:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} left the game.");
						return false;

					case DisconnectReasons.NewConnection:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} logged in from another device.");
						return false;

					case DisconnectReasons.IncorrectVersion:
					case DisconnectReasons.MismatchedVersion:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} is on a different version of Among Us.");
						return false;

					case DisconnectReasons.GameFull:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} couldn't join: the game was full.");
						return false;

					case DisconnectReasons.GameStarted:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} couldn't join: the game had already started.");
						return false;

					case DisconnectReasons.InvalidName:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was kicked for an invalid name.");
						return false;

					case DisconnectReasons.Sanctions:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected by Among Us account sanctions.");
						return false;

					case DisconnectReasons.PlatformLock:
					case DisconnectReasons.SelfPlatformLock:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected by a platform restriction.");
						return false;

					case DisconnectReasons.LobbyInactivity:
					case DisconnectReasons.MatchmakerInactivity:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected for inactivity.");
						return false;

					case DisconnectReasons.ServerError:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected by a server error.");
						return false;

					default:
						HudManager.Instance.Notifier.AddDisconnectMessage($"{playerName} was disconnected due to {reason}.");
						return false;
				}
			}
		}
	}
}