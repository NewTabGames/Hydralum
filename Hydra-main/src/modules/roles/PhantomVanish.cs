using HarmonyLib;
using UnityEngine;

namespace HydraMenu.modules.roles
{
	// Phantom vanish tweaks. Two independent options:
	//
	//   EndlessDuration - keeps you invisible past the normal vanish duration. Implemented purely by freezing
	//     the client's own countdown so it never fires the reappear. This sends nothing to the server, so none
	//     of Innersloth's CheckVanish/CheckAppear ban conditions are touched. Remote clients only reappear you
	//     when they receive your Appear RPC, so as long as you never send one you stay hidden to everyone.
	//
	//   NoCooldown - lets you re-vanish with no cooldown. This one CANNOT just zero the timer: pressing vanish
	//     while the server still thinks you're in cooldown sends CheckVanish and gets you BANNED. So when it's
	//     on we route vanish (and its matching appear) through the raw Vanish/Appear RPCs instead of the
	//     server-checked CheckVanish/CheckAppear - the exact path "Kill While Vanished" already uses safely
	//     (see NoKillChecks.VanishBypass/AppearBypass, whose conditions we extend). The server never sees a
	//     CheckVanish, so there's no cooldown to violate. Because the server then isn't tracking your vanish,
	//     it won't authorise kills-while-vanished on its own - pair with "Kill While Vanished" for that.
	internal class PhantomVanish : Module
	{
		public PhantomVanish() : base("PhantomVanish") { }

		private static PhantomVanish Instance
		{
			get { return ModuleManager.phantomVanish; }
		}

		public bool EndlessDuration { get; set; } = false;
		public bool NoCooldown { get; set; } = false;

		// True when vanish/appear must be rerouted through the raw RPCs to stay ban-safe.
		public static bool ShouldBypassChecks()
		{
			return Instance.NoCooldown;
		}

		[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.FixedUpdate))]
		class PhantomTweaks
		{
			static void Postfix(PhantomRole __instance)
			{
				if(__instance == null || __instance.Player == null || !__instance.Player.AmOwner) return;

				if(Instance.EndlessDuration && __instance.isInvisible)
				{
					// Never let the local reappear countdown reach zero, so we never trigger an Appear.
					__instance.durationSecondsRemaining = float.MaxValue;
				}

				if(Instance.NoCooldown)
				{
					__instance.cooldownSecondsRemaining = 0f;

					if(HudManager.InstanceExists && HudManager.Instance.AbilityButton != null)
					{
						HudManager.Instance.AbilityButton.ResetCoolDown();
						HudManager.Instance.AbilityButton.SetCooldownFill(0f);
					}
				}
			}
		}
	}
}
