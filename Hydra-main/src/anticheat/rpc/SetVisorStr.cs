using Hazel;

namespace HydraMenu.anticheat.rpc
{
	// Flags a player who equips an illegal visor mid-lobby (masks are usually visors).
	internal class SetVisorStr : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			string visorId = reader.ReadString();
			IllegalCosmetics.CheckId(player, visorId, "visor");
			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetVisorStr;
		}
	}
}
