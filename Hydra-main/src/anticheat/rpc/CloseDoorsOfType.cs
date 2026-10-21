using Hazel;
using System;

namespace HydraMenu.anticheat.rpc
{
	internal class CloseDoorsOfType : RpcCheck
	{
		// Player is null here as we cannot determine who sent this RPC
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			// It would be nice if we could also add additional checks such as someone closing doors without being imposter
			// however we are not able to determine who sent the CloseDoorsOfType RPC (it's a ShipStatus RPC with
			// no sender), so only context-based checks are possible here.
			if(GameManager.Instance.IsHideAndSeek())
			{
				Anticheat.Flag("Someone attempted to close doors while in Hide and Seek.");
				return false;
			}

			// Doors can never be closed while a meeting is active - block it (no player to attribute/punish).
			// Toggleable via the anticheat settings.
			if(Anticheat.BlockDoorCloseInMeeting && MeetingHud.Instance != null)
			{
				Anticheat.Flag("Someone attempted to close doors during a meeting.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.CloseDoorsOfType;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(ShipStatus);
		}
	}
}