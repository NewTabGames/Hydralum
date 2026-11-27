using HarmonyLib;
using InnerNet;
using System.Collections.Generic;

namespace HydraMenu.anticheat
{
	// Flags players wearing cosmetics they can't legitimately have:
	//  - PlatformExclusive: items locked to one platform (e.g. the PlayStation-only "Geoff" mask and "Clank"
	//    pet). They're legit on that platform, but a player wearing one on ANY OTHER platform modded it in.
	//  - AlwaysIllegal: items impossible on every platform (dev / removed cosmetics). Add ids here.
	// A match trips the anticheat (flag + blacklist, plus a kick when we're host). Ids are the game's internal
	// product ids - exactly the string shown in a saved outfit, e.g. "hat_geoff", "pet_clank".
	internal class IllegalCosmetics
	{
		public static readonly Dictionary<string, Platforms> PlatformExclusive = new Dictionary<string, Platforms>
		{
			{ "hat_geoff", Platforms.Playstation }, // "Geoff" mask (worn in the visor slot) - PlayStation exclusive
			{ "pet_clank", Platforms.Playstation }, // Ratchet & Clank "Clank" pet - PlayStation exclusive
		};

		public static readonly HashSet<string> AlwaysIllegal = new HashSet<string>
		{
			"pet_test", // dev / test pet - not obtainable on any platform
		};

		// Flags one cosmetic slot if its id is illegal. Platform-exclusive ids only flag off their platform.
		public static void CheckId(PlayerControl player, string cosmeticId, string slot)
		{
			if(!Anticheat.Enabled || !Anticheat.CheckIllegalCosmetics) return;
			if(player == null || player.Data == null || string.IsNullOrEmpty(cosmeticId)) return;

			if(AlwaysIllegal.Contains(cosmeticId))
			{
				Anticheat.Flag(player, $"{player.Data.PlayerName} is wearing an illegal {slot} ({cosmeticId}) that cannot be obtained legitimately.");
				return;
			}

			if(PlatformExclusive.TryGetValue(cosmeticId, out Platforms requiredPlatform))
			{
				ClientData client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(player) : null;

				// If we can't resolve the player's platform, don't guess - a wrong guess would false-flag a
				// legitimate PlayStation player.
				if(client == null || client.PlatformData == null) return;

				Platforms platform = client.PlatformData.Platform;
				if(platform == requiredPlatform) return; // legit on its own platform

				Anticheat.Flag(player, $"{player.Data.PlayerName} is wearing a {requiredPlatform}-exclusive {slot} ({cosmeticId}) on {platform}, which cannot be obtained there.");
			}
		}

		// Scans every cosmetic slot of a player's current outfit.
		public static void CheckOutfit(PlayerControl player)
		{
			if(!Anticheat.Enabled || !Anticheat.CheckIllegalCosmetics) return;
			if(player == null || player.Data == null) return;

			var outfit = player.Data.DefaultOutfit;
			if(outfit == null) return;

			CheckId(player, outfit.HatId, "hat");
			CheckId(player, outfit.VisorId, "visor");
			CheckId(player, outfit.SkinId, "skin");
			CheckId(player, outfit.PetId, "pet");
		}

		// Catches players already wearing an illegal cosmetic when they spawn / when we join the lobby (the
		// cosmetic comes in via GameData, not an RPC, so the per-RPC checks below wouldn't see it).
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
		class IllegalCosmeticStart
		{
			static void Postfix(PlayerControl __instance)
			{
				try { CheckOutfit(__instance); } catch { }
			}
		}
	}
}
