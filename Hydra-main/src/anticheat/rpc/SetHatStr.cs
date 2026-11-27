using Hazel;

namespace HydraMenu.anticheat.rpc
{
	// Flags a player who equips an illegal hat mid-lobby. The on-spawn scan (IllegalCosmetics) covers hats
	// already worn on join; this covers hats changed after.
	internal class SetHatStr : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			string hatId = reader.ReadString();
			IllegalCosmetics.CheckId(player, hatId, "hat");
			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetHatStr;
		}
	}
}
