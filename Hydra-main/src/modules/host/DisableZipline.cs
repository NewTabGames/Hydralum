using HarmonyLib;

namespace HydraMenu.modules.host
{
	internal class DisableZipline : Module
	{
		// On server-authoritative lobbies, the CheckZipline RPC is handled by the server rather than the host
		// so this module will only work on host-authoritative lobbies
		public DisableZipline() : base("DisableZipline") { }

		private static DisableZipline Instance
		{
			get { return ModuleManager.disableZipline; }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
		class ZiplineRequest
		{
			static bool Prefix()
			{
				return !Instance.Enabled;
			}
		}
	}
}
