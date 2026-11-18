using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using HydraMenu.network;
using System.Collections;
using UnityEngine;

namespace HydraMenu.modules.roles
{
	internal class NoPhantomAnimation : Module
	{
		public NoPhantomAnimation() : base("NoPhantomAnimation") { }

		private static NoPhantomAnimation Instance
		{
			get { return ModuleManager.noPhantomAnimation; }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckVanish))]
		class CheckVanish
		{
			static void Prefix(PlayerControl __instance)
			{
				if(!Instance.Enabled || __instance != PlayerControl.LocalPlayer) return;

				PlayerControl.LocalPlayer.StartCoroutine(HideVanishAnimation().WrapToIl2Cpp());
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckAppear))]
		class CheckAppear
		{
			static void Prefix(PlayerControl __instance, ref bool shouldAnimate)
			{
				if(!Instance.Enabled || __instance != PlayerControl.LocalPlayer) return;

				shouldAnimate = false;

				// If fading stays true, the Kill/Phantom buttons get greyed out and stuck, so clear it manually.
				PhantomRole role = PlayerControl.LocalPlayer.Data.Role.TryCast<PhantomRole>();
				if(role != null)
				{
					role.fading = false;
				}
			}
		}

		// Unlike Appear, Vanish has no "shouldAnimate" field, so instead we teleport ourselves to the void
		// (on remote screens only) for the duration of the animation so nobody sees the vanish poof.
		public static IEnumerator HideVanishAnimation()
		{
			PhantomRole role = PlayerControl.LocalPlayer.Data.Role.TryCast<PhantomRole>();
			if(role == null)
			{
				Hydra.Log.LogWarning("Our player started the phantom animation when our role is not Phantom");
				yield break;
			}

			// Pause our NetTransform so we don't snap back to our old position once we move.
			PlayerControl.LocalPlayer.NetTransform.SetPaused(true);
			PlayerControl.LocalPlayer.NetTransform.sendQueue.Clear();
			PlayerControl.LocalPlayer.NetTransform.ClearDirtyBits();

			BatchedMessage batch = new BatchedMessage();
			batch.skipLocalHandling = true;

			batch.QueueSnapTo(PlayerControl.LocalPlayer, new Vector2(999, 999));
			batch.FinishBatch();

			// Hardcoded wait to cover ping differences and the little poof at the end of the vanish.
			yield return Effects.Wait(0.75f);

			PlayerControl.LocalPlayer.NetTransform.SetPaused(false);
		}
	}
}
