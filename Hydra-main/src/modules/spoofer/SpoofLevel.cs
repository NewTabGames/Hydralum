using AmongUs.Data;
using HarmonyLib;

namespace HydraMenu.modules.spoofer
{
	internal class SpoofLevel : Module
	{
		public SpoofLevel() : base("SpoofLevel") { }

		public uint SpoofedLevel { get; set; } = 200;

		private static SpoofLevel Instance
		{
			get { return ModuleManager.spoofLevel; }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetLevel))]
		class SetLevel
		{
			static void Prefix(ref uint level)
			{
				if(!Instance.Enabled) return;

				level = Instance.SpoofedLevel - 1;
			}
		}

		protected override void OnEnable()
		{
			if(PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.PlayerLevel != SpoofedLevel)
			{
				PlayerControl.LocalPlayer.RpcSetLevel(SpoofedLevel);
			}
		}

		protected override void OnDisable()
		{
			uint trueLevel = DataManager.Player.Stats.Level;
			if(PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.PlayerLevel != trueLevel)
			{
				PlayerControl.LocalPlayer.RpcSetLevel(trueLevel);
			}
		}
	}
}
