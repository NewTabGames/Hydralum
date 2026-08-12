using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;

namespace HydraMenu.features
{
	internal class Spoofer
	{
		public static bool shouldSpoofVersion = false;
		public static int spoofedVersion = Constants.GetBroadcastVersion();
		public static bool useModdedProtocol = false;

		[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
		class SpoofVersion
		{
			static bool Prefix(ref int __result)
			{
				// Starting a local lobby or entering Freeplay will bug out if we are using a spoofed version
				if(!shouldSpoofVersion || !AmongUsClient.Instance || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame) return true;

				__result = spoofedVersion;
				if(useModdedProtocol) __result += 25;

				return false;
			}
		}

		[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
		class MarkVersionModded
		{
			static bool Prefix(ref bool __result)
			{
				if(shouldSpoofVersion && useModdedProtocol)
				{
					__result = true;
					return false;
				} else
				{
					return true;
				}
			}
		}
	}
}