using HydraMenu.network;
using UnityEngine;

namespace HydraMenu.routines
{
	// Spams the Influencer (SpiritGuide) photo cue at everyone on a loop. Only useful on host-authoritative
	// (no-anticheat) lobbies - vanilla's anticheat rate-limits SpiritGuideMessage to one cue per player every
	// ~42s, so it barely does anything on server-authoritative servers.
	internal class SpamInfluencerCues : Routine
	{
		public SpamInfluencerCues() : base("SpamInfluencerCues") { }

		// The client throws an error if the SpiritGuideMessage RPC has more than three sprites.
		private readonly int MAX_SPRITE_COUNT = 3;
		private readonly float SEND_DELAY = 4.0f;
		private float timeElapsed = 0.0f;

		private readonly System.Random rnd = new System.Random();

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null || ShipStatus.Instance == null)
			{
				Enabled = false;
				return;
			}

			timeElapsed += Time.deltaTime;
			if(timeElapsed < SEND_DELAY) return;
			timeElapsed = 0.0f;

			int possibleSpriteCount = ShipStatus.Instance.socialMediumFeedSystem.PossibleSpriteList.Count;
			if(possibleSpriteCount <= 0) return;

			byte[] sprites = new byte[MAX_SPRITE_COUNT];
			for(int i = 0; i < sprites.Length; i++)
			{
				sprites[i] = (byte)rnd.Next(0, possibleSpriteCount);
			}

			BatchedMessage batch = new BatchedMessage();
			batch.QueueSpiritGuideMessage(sprites);
			batch.FinishBatch();
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null || ShipStatus.Instance == null)
			{
				Hydra.notifications.Send("Spam Influencer Cues", "Spam Influencer Cues can only be used once the game has started.", 10);
				Enabled = false;
			}
		}
	}
}
