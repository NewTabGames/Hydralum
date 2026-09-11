using HarmonyLib;

namespace HydraMenu.modules.roles
{
	internal class UnlockSabotageButton : Module
	{
		public UnlockSabotageButton() : base("UnlockSabotageButton")
		{
			base.Enabled = true;
		}

		private static UnlockSabotageButton Instance
		{
			get { return ModuleManager.unlockSabotageButton; }
		}

		public bool SabotageInVents { get; set; } = false;

		// Set true only for the instant we force the sabotage map open from inside a vent, so the map's
		// "can't open while you can't move" guard (PlayerControl.CanMove) can be bypassed just for that call.
		internal static bool ForceMapOpen;

		private static bool ImpostorVentingWithFeatureOn()
		{
			PlayerControl player = PlayerControl.LocalPlayer;
			return Instance.SabotageInVents
				&& player != null && player.Data != null
				&& player.inVent
				&& RoleManager.IsImpostorRole(player.Data.RoleType);
		}

		// The sabotage map (MapBehaviour) refuses to open while PlayerControl.CanMove is false, and venting
		// makes it false. Flip ForceMapOpen for the duration of the open call so the CanMove getter patch
		// reports true just long enough for the map to appear, then restore normal behaviour immediately.
		private static void OpenSabotageMap()
		{
			ForceMapOpen = ImpostorVentingWithFeatureOn();
			try
			{
				HudManager.Instance.ToggleMapVisible(new MapOptions { Mode = MapOptions.Modes.Sabotage });
			}
			finally
			{
				ForceMapOpen = false;
			}
		}

		// Clicking the sabotage button has checks to make sure the current player is indeed an imposter, not in a vent, and that the current gamemode supports sabotages
		// This means setting the GameObject's sabotage button state to active won't allow crewmates to sabotage alone, we need to override the DoClick function to not have those checks
		[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
		class SkipSabotageChecks
		{
			static bool Prefix()
			{
				PlayerControl player = PlayerControl.LocalPlayer;

				// We have to limit this to Imposters as the crewmate exit vent button will be on the same position as the imposter sabotage button
				if(!Instance.SabotageInVents && player.inVent && !RoleManager.IsImpostorRole(player.Data.RoleType)) return true;

				OpenSabotageMap();
				return false;
			}
		}

		// HudManager calls SabotageButton.Refresh() every frame, and it greys the button out (SetDisabled)
		// while the local player is in a vent. SetDisabled also clears canInteract / kills the button's
		// click target, so the click never even reaches DoClick - which is why overriding DoClick alone
		// wasn't enough. Re-enable the button right after each Refresh when an imposter is venting with the
		// feature on, so it stays lit up and clickable inside vents.
		[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
		class KeepSabotageEnabledInVent
		{
			static void Postfix(SabotageButton __instance)
			{
				if (ImpostorVentingWithFeatureOn()) __instance.SetEnabled();
			}
		}

		// PlayerControl.CanMove is false inside a vent, and the sabotage map won't open while it's false.
		// Only while we're deliberately opening that map from a vent (ForceMapOpen), report CanMove as true
		// so the map's guard passes. Scoped to the single open call, so movement/vent behaviour is unaffected.
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
		class ForceCanMoveWhileOpeningSabotageMap
		{
			static void Postfix(ref bool __result)
			{
				if (ForceMapOpen) __result = true;
			}
		}
	}
}