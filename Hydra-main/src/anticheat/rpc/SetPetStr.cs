using Hazel;

namespace HydraMenu.anticheat.rpc
{
	// Flags a player who equips an illegal pet mid-lobby.
	internal class SetPetStr : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			string petId = reader.ReadString();
			IllegalCosmetics.CheckId(player, petId, "pet");
			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.SetPetStr;
		}
	}
}
