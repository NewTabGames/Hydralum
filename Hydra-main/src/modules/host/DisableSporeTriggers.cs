using HarmonyLib;

namespace HydraMenu.modules.host
{
	internal class DisableSporeTriggers : Module
	{
		public DisableSporeTriggers() : base("DisableSporeTriggers") { }

		private static DisableSporeTriggers Instance
		{
			get { return ModuleManager.disableSporeTriggers; }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
		class TriggerSporeRequest
		{
			static bool Prefix()
			{
				return !Instance.Enabled;
			}
		}
	}
}
