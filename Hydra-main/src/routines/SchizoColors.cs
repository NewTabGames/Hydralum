using HydraMenu.modules;
using HydraMenu.network;
using Hazel;

namespace HydraMenu.routines
{
	// Schizo Colors as a toggle: when switched on, every player is shown a DIFFERENT, random color on each
	// OTHER client (addressed per receiving client), so nobody sees the real colors and no two clients agree.
	//
	// The scramble is applied EXACTLY ONCE when the toggle is turned on - there is nothing to maintain, so
	// there is no timer and nothing is re-sent (re-sending the same colors would just redo work already done).
	// It never touches the host's authoritative colors, so the real colors are always still on record and
	// disabling simply broadcasts them back, returning the lobby to normal.
	//
	// Host-authoritative: vanilla clients obey, no mod needed on them; it no-ops as a non-host.
	public class SchizoColorsRoutine : Routine
	{
		public SchizoColorsRoutine() : base("SchizoColors") { }

		private bool appliedOnce = false;

		private readonly System.Random rnd = new System.Random();

		public override void Run()
		{
			// Apply the scramble once, then idle - there is nothing to keep refreshing.
			if(appliedOnce) return;
			if(AmongUsClient.Instance == null || PlayerControl.AllPlayerControls == null) return;

			appliedOnce = true;
			ApplyScramble();
		}

		private void ApplyScramble()
		{
			foreach(PlayerControl receiver in PlayerControl.AllPlayerControls)
			{
				if(receiver == null || receiver.Data == null) continue;

				int receiverClientId = AmongUsClient.Instance.GetClientIdFromCharacter(receiver);

				foreach(PlayerControl pc in PlayerControl.AllPlayerControls)
				{
					if(pc == null || pc.Data == null) continue;

					byte color = (byte)rnd.Next(0, 18);

					// Same wire format as BatchedMessage.QueueSetColor (Data.NetId + color), addressed to one
					// receiver at a time so each client applies a different color table.
					MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.SetColor, SendOption.Reliable, receiverClientId);
					writer.Write(pc.Data.NetId);
					writer.Write(color);
					AmongUsClient.Instance.FinishRpcImmediately(writer);
				}
			}
		}

		// Broadcast every player's REAL color (still held in authoritative GameData) to everyone, undoing the scramble.
		private void RestoreColors()
		{
			if(AmongUsClient.Instance == null || PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;

			int packingLimit = AmongUsClient.Instance.GetMaxMessagePackingLimit();
			BatchedMessage batch = new BatchedMessage();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == null || player.Data == null || player.Data.DefaultOutfit == null) continue;

				if(batch.msgCount >= packingLimit)
				{
					batch.FinishBatch();
					batch = new BatchedMessage();
				}

				batch.QueueSetColor(player, (byte)player.Data.DefaultOutfit.ColorId);
			}

			batch.FinishBatch();
		}

		private void OnDisconnect()
		{
			Enabled = false;
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications.Send("Schizo Colors", "Schizo Colors can only be used inside of a game.", 10);
				Enabled = false;
				return;
			}

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Schizo Colors", "Schizo Colors can only be used if you are the host of the lobby.", 10);
				Enabled = false;
				return;
			}

			appliedOnce = false;

			EventCoordinator.OnDisconnect += OnDisconnect;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnDisconnect -= OnDisconnect;

			// Put everyone's real colors back for all clients.
			RestoreColors();
		}
	}
}
