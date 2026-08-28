using HydraMenu.network;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HydraMenu.routines
{
	public class DiscoHostRoutine : IRoutine
	{
		public DiscoHostRoutine() : base("DiscoHost") { }
		public HashSet<int> targets = new HashSet<int>();

		public float randomizationDelay = 0.5f;
		private float timeElapsed = 0f;

		private System.Random rnd = new System.Random();

		public override void Run()
		{
			if(PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;

			timeElapsed += Time.deltaTime;
			if(timeElapsed < randomizationDelay) return;
			timeElapsed = 0f;

			if(!IsGlobal)
			{
				targets.RemoveWhere(h => {
					var p = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(pc => pc != null && pc.GetHashCode() == h);
					return p == null || p.Data == null || p.Data.Disconnected;
				});

				if(targets.Count == 0)
				{
					Enabled = false;
					return;
				}
			}

			List<int> colors = Enumerable.Range(0, 18).ToList();

			BatchedMessage batch = new BatchedMessage();

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{
				if(player == null || player.Data == null || player.Data.Disconnected) continue;
				if(!IsGlobal && !targets.Contains(player.GetHashCode())) continue;
				if(PresenceTracker.IsDevUser(player.Data) && player != PlayerControl.LocalPlayer) continue;

				// Assign each player a unique color
				int color;
				if(colors.Count != 0)
				{
					color = colors[rnd.Next(0, colors.Count)];
					colors.Remove(color);
				}
				else
				{
					// To ensure compatability for lobbies with more than 18 players
					color = rnd.Next(0, 18);
				}

				batch.QueueSetColor(player, (byte)color);
			}

			batch.FinishBatch();
		}

		public bool IsGlobal
		{
			get { return targets.Count == 1 && targets.Contains(int.MaxValue); }
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications.Send("Disco Party", "Disco Party can only be used inside of a game.", 10);
				Enabled = false;
				return;
			}

			if(Utilities.IsAnticheatPresent() && !AmongUsClient.Instance.AmHost)
			{
				Hydra.notifications.Send("Disco Party", "Disco Party can only be used if you are the host of the lobby.", 10);
				Enabled = false;
				return;
			}
		}

		protected override void OnDisable()
		{
			targets.Clear();
			timeElapsed = 0f;
		}

		public override void OnDisconnect()
		{
			targets.Clear();
			timeElapsed = 0f;
			Hydra.notifications?.Send("Disco Party", "Disco Party was disabled as you left the game.", 10);
			Enabled = false;
		}
	}
}