using HarmonyLib;

namespace HydraMenu.modules.spoofer
{
	internal class SpoofDevice : Module
	{
		public SpoofDevice() : base("SpoofDevice") { }

		private static SpoofDevice Instance
		{
			get { return ModuleManager.spoofDevice; }
		}

		public Platforms SpoofedPlatform { get; set; } = Constants.GetPlatformType();

		// Applies the spoofed platform (and the matching platform-specific fields) to a data object.
		private static void ApplySpoof(PlatformSpecificData data)
		{
			if (data == null) return;

			data.Platform = Instance.SpoofedPlatform;

			switch (Instance.SpoofedPlatform)
			{
				case Platforms.StandaloneWin10:
					data.PlatformName = "TESTNAME";
					data.XboxPlatformId = 2584878536129841;
					data.PsnPlatformId = 0;
					break;

				case Platforms.Xbox:
					// You can find the proper XUID for an Xbox gamertag at https://www.cxkes.me/xbox/xuid
					data.PlatformName = "Major Nelson";
					data.XboxPlatformId = 2584878536129841;
					data.PsnPlatformId = 0;
					break;

				case Platforms.Playstation:
					data.PlatformName = "";
					data.XboxPlatformId = 0;
					data.PsnPlatformId = 0;
					break;

				case Platforms.Switch:
					data.PlatformName = "Sus";
					data.XboxPlatformId = 0;
					data.PsnPlatformId = 0;
					break;

				default:
					// Other platforms do not send additional platform specific data
					data.PlatformName = "TESTNAME";
					data.XboxPlatformId = 0;
					data.PsnPlatformId = 0;
					break;
			}
		}

		// Primary hook: Constants.GetPlatformData is the SOURCE the game reads the local platform
		// from when it builds the data it sends. Patching the source (rather than only
		// PlatformSpecificData.Serialize) keeps the spoof working regardless of how the game
		// serializes it. A fresh object is returned so toggling the spoof off restores the real
		// platform instead of leaving a mutated cached instance behind.
		[HarmonyPatch(typeof(Constants), nameof(Constants.GetPlatformData))]
		class GetPlatformData
		{
			static void Postfix(ref PlatformSpecificData __result)
			{
				if (Instance == null || !Instance.Enabled) return;

				PlatformSpecificData data = new PlatformSpecificData();
				ApplySpoof(data);
				__result = data;
			}
		}

		// Belt-and-suspenders: also cover the serialize path in case the game serializes a
		// PlatformSpecificData that didn't come through GetPlatformData.
		[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
		class SerializeDevice
		{
			static void Prefix(PlatformSpecificData __instance)
			{
				if (Instance == null || !Instance.Enabled) return;

				ApplySpoof(__instance);
			}
		}
	}
}
