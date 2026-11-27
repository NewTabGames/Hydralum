using Hazel;

namespace HydraMenu.anticheat.rpc
{
	// Flags a player who equips an illegal skin mid-lobby.
	internal class SetSkinStr : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			string skinId = reader.ReadString();
			IllegalCosmetics.CheckId(player, skinId, "skin");
			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetSkinStr;
		}
	}
}
