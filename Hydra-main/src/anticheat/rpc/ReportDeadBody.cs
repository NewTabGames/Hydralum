using Hazel;

namespace HydraMenu.anticheat.rpc
{
	internal class ReportDeadBody : RpcCheck
	{
		public override bool Validate(PlayerControl player, MessageReader reader)
		{
			if(GameManager.Instance.IsHideAndSeek())
			{
				Anticheat.Flag(player, $"{player?.Data?.PlayerName ?? "Unknown"} attempted to call a meeting in Hide and Seek");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.ReportDeadBody;
		}
	}
}