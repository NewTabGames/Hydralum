using Hazel;
using UnityEngine;

namespace HydraMenu.routines
{

	public class PetPlayerRoutine : IRoutine
	{
		public PetPlayerRoutine() : base("PetPlayer") { }

		// This can not be too low, otherwise the petting animation will snap and look unpleasant
		public readonly float PET_DELAY = 0.60f;

		public PlayerControl target;
		private float timeElapsed = 0.0f;

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) return;
			if(target == null || target.Data == null || target.Data.Disconnected)
			{
				Enabled = false;
				target = null;
				return;
			}
			if(target.Data != null && PresenceTracker.IsDevUser(target.Data) && target != PlayerControl.LocalPlayer)
			{
				ui.NotificationManager.AddNotification("Cannot target Developer");
				Enabled = false;
				target = null;
				return;
			}
			if(PlayerControl.LocalPlayer.cosmetics == null || PlayerControl.LocalPlayer.cosmetics.currentPet == null) return;

			timeElapsed += Time.deltaTime;
			if(timeElapsed < PET_DELAY) return;
			timeElapsed = 0.0f;

			Vector2 petPosition = target.transform.position;
			petPosition.y -= PlayerControl.LocalPlayer.cosmetics.currentPet.yOffset * 2;

			// The PlayerPhysics::CoPet function calls the PlayerPhysics::CancelPet function
			// which sets PlayerControl::moveable to true, allowing the player to move again
			// So we just reimplement the necessary parts to get our petting hand to show, and to send the Pet RPC
			if (PlayerControl.LocalPlayer.cosmetics.CurrentPet != null)
			{
				PlayerControl.LocalPlayer.cosmetics.CurrentPet.SetGettingPet(true, petPosition);
			}

			if (PlayerControl.LocalPlayer.cosmetics.PettingHand != null)
			{
				PlayerControl.LocalPlayer.cosmetics.PettingHand.StartPet(PlayerControl.LocalPlayer.cosmetics.currentPet);
			}

			if (PlayerControl.LocalPlayer.MyPhysics != null)
			{
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
					PlayerControl.LocalPlayer.MyPhysics.NetId,
					(byte)RpcCalls.Pet,
					SendOption.Reliable,
					-1
				);

				NetHelpers.WriteVector2(PlayerControl.LocalPlayer.GetTruePosition(), writer);
				NetHelpers.WriteVector2(petPosition, writer);

				AmongUsClient.Instance.FinishRpcImmediately(writer);
			}
		}

		protected override void OnEnable()
		{
			if (PlayerControl.LocalPlayer != null)
			{
				PlayerControl.LocalPlayer.moveable = false;
				if (PlayerControl.LocalPlayer.NetTransform != null && PlayerControl.LocalPlayer.NetTransform.body != null)
				{
					PlayerControl.LocalPlayer.NetTransform.body.velocity = Vector2.zero;
				}
			}
		}

		protected override void OnDisable()
		{
			target = null;
			timeElapsed = 0.0f;

			if(PlayerControl.LocalPlayer != null)
			{
				PlayerControl.LocalPlayer.moveable = true;
				if (PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.PettingHand != null)
				{
					PlayerControl.LocalPlayer.cosmetics.PettingHand.StopPetting();
				}
			}
		}

		public override void OnDisconnect()
		{
			target = null;
			timeElapsed = 0.0f;
			if(PlayerControl.LocalPlayer != null)
			{
				PlayerControl.LocalPlayer.moveable = true;
				if (PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.PettingHand != null)
				{
					PlayerControl.LocalPlayer.cosmetics.PettingHand.StopPetting();
				}
			}
			Hydra.notifications?.Send("Pet Player", "Pet Player was disabled as you left the game.", 10);
			Enabled = false;
		}
	}
}