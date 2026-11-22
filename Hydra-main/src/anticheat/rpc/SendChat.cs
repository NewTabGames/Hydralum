using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	// Vanilla clients can only open the chat box in the lobby or during a meeting. A SendChat RPC that
	// arrives while a round is actually in progress (ship loaded, no meeting on screen) can only come from a
	// modified client chatting mid-game, so we flag the sender.
	internal class SendChat : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			if(player == null || player.Data == null)
			{
				return true;
			}

			// In the lobby ShipStatus is null, so lobby chat passes. During a meeting MeetingHud is present,
			// so meeting chat (including ghost chat) passes. Everything else is active gameplay.
			if(ShipStatus.Instance != null && MeetingHud.Instance == null)
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} sent a chat message outside of a meeting.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SendChat;
		}
	}
}
