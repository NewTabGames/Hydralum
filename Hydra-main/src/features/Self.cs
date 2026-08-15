using AmongUs.Data.Player;
using HarmonyLib;
using HydraMenu.network;

namespace HydraMenu.features
{
	internal class Self
	{
		[HarmonyPatch(typeof(PlayerStatsData), nameof(PlayerStatsData.ValidateStat))]
		public static class UpdateStatsFreeplay
		{
			public static bool Enabled { get; set; } = false;

			static void Prefix(PlayerStatsData __instance)
			{
				if(Enabled)
				{
					__instance.isTrackingStats = true;
				}
			}
		}

		[HarmonyPatch(typeof(Ladder), nameof(Ladder.SetDestinationCooldown))]
		public static class NoLadderCooldown
		{
			public static bool Enabled { get; set; } = true;
			static void Postfix(Ladder __instance)
			{
				if(Enabled)
				{
					Hydra.Log.LogMessage($"Used ladder");
					__instance.CoolDown = 0.0f;
					__instance.Destination.CoolDown = 0.0f;
				}
			}
		}

		[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Begin))]
		public static class UnlimitedMeetings
		{
			public static bool enabled = true;

			static void Prefix()
			{
				if(enabled) PlayerControl.LocalPlayer.RemainingEmergencies = 999999;
			}
		}

		public static class ColorSniper
		{
			public static bool Enabled { get; set; } = false;
			public static ui.Controls.PlayerColors TargetColor { get; set; } = ui.Controls.PlayerColors.Red;
			private static float timer = 0f;
			private const float checkInterval = 0.5f;

			public static void Run()
			{
				if(!Enabled) return;
				if(PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.DefaultOutfit == null) return;
				if(AmongUsClient.Instance == null || !AmongUsClient.Instance.AmConnected) return;

				byte targetColorId = (byte)TargetColor;

				// If we already have the target color, do nothing
				if(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId == targetColorId) return;

				timer += UnityEngine.Time.deltaTime;
				if(timer < checkInterval) return;
				timer = 0f;

				// Check if another player in the lobby currently has this color
				bool isColorTaken = false;
				if(PlayerControl.AllPlayerControls != null)
				{
					foreach(PlayerControl player in PlayerControl.AllPlayerControls)
					{
						if(player == null || player == PlayerControl.LocalPlayer || player.Data == null || player.Data.Disconnected || player.Data.DefaultOutfit == null) continue;
						if(player.Data.DefaultOutfit.ColorId == targetColorId)
						{
							isColorTaken = true;
							break;
						}
					}
				}

				if(!isColorTaken)
				{
					PlayerControl.LocalPlayer.CmdCheckColor(targetColorId);
				}
			}
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
		public static class PlayerControl_FixedUpdate_ColorSniper
		{
			public static void Postfix(PlayerControl __instance)
			{
				if(__instance.AmOwner)
				{
					ColorSniper.Run();
				}
			}
		}
	}
}