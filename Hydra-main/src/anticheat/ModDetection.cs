using System.Collections.Generic;
using HarmonyLib;
using Hazel;

namespace HydraMenu.anticheat
{
	// Passive client fingerprinting.
	//
	// Vanilla Among Us only sends PlayerControl RPCs whose callId is in the RpcCalls enum (plus a few
	// newer vanilla ones). A menu that adds custom network features sends RPCs with its own out-of-range
	// callId, so an out-of-range callId betrays a modded client - and specific callIds map to specific
	// known menus (their handshake).
	//
	// Passive identification (the callId fingerprint) only catches menus that emit a CUSTOM (non-vanilla)
	// RPC or a non-vanilla GameData tag; stealth menus that merely abuse vanilla RPCs (Hydra and Malum
	// included) read as "Unmodded". To partly cover those, Observe ALSO does behavioural flood/overload
	// detection on every RPC, and known handshakes (164 Sicko, 242 Onyx) are parsed for their name/version.
	// Observe runs for all player-owned HandleRpc objects (PlayerControl / PlayerPhysics / CustomNetworkTransform).
	internal static class ModDetection
	{
		// callId -> menu name, for menus with a confirmed signature. Anything
		// else non-vanilla is reported generically as "Modded (RPC N)".
		private static readonly Dictionary<byte, string> KnownMods = new Dictionary<byte, string>()
		{
			{ 121, "Chocoo" },
			{ 167, "TuffMenu" },
			{ 164, "SickoMenu" }, // confirmed: Sicko broadcasts its version on RPC 164 while in a lobby
			{ 176, "HostGuard / TOH" },
			{ 195, "Polar" },
			{ 204, "Polar" },
			{ 154, "GNC" },
			// NOTE: callId 250 was mapped to "KillNet" from the same old spoof list as the removed 169 entry,
			// but it's seen in the wild broadcast by multiple players at once - i.e. a COMMON menu, not niche
			// KillNet. Left unmapped so it reports generically as "Modded (RPC 250)" and its payload (below) is
			// read for the real name/version. 80/85 are KillNet from the same unverified source - treat as suspect.
			{ 80,  "KillNet" },
			{ 85,  "KillNet" },
			{ 150, "BetterAmongUs / GreaterAmongUs" },
			{ 82,  "Unknown (RPC 82)" },
			{ 103, "Unknown (Askinchik)" },
			{ 212, "BanMod" },
			{ 213, "BanMod" },
			{ 214, "BanMod" },
			{ 215, "BanMod" },
			{ 216, "BanMod" },
			{ 217, "BanMod" },
			{ 218, "BanMod" },
			{ 144, "Gaff Menu" },
			{ 145, "Gaff Menu" },
			{ 188, "GMM" },
			{ 189, "GMM" },
			// NOTE: callId 169 was mapped to "Malum Menu" but current base Malum (and this fork) are stealth -
			// they send only vanilla RPCs, so they never emit 169. The mapping was a stale leftover from an old
			// spoof list and risked false-flagging a legit player if 169 ever becomes a vanilla RPC, so it's gone.
			{ 133, "Lunar / NjordMenu" },
			{ 210, "Lunar / NjordMenu" },
			{ 89,  "NjordMenu (Old)" },
			{ 202, "ModMenuCrew" },        // sends an "MMC_v5" handshake
			{ 162, "Unknown (RPC 162)" },
			{ 242, "OnyxMenu / Nocturne" }, // broadcasts a name+version handshake on RPC 242 while in a lobby
			{ 255, "Unknown (RPC 255)" }
		};

		// Newer-but-vanilla RPCs added after the pinned GameLibs; these must not be flagged.
		private static readonly HashSet<byte> ExtraVanillaRpcs = new HashSet<byte>() { 67 }; // SpiritGuide

		// playerId -> detected client label(s). Cleared on joining a new lobby.
		private static readonly Dictionary<byte, HashSet<string>> _detected = new Dictionary<byte, HashSet<string>>();

		// Per (senderPlayerId, callId) sliding-window RPC counters for flood/overload detection.
		private static readonly Dictionary<int, (long start, int count)> _flood = new Dictionary<int, (long, int)>();
		private const int FloodThreshold = 50;      // same callId from one sender within the window = abnormal
		private const long FloodWindowMs = 1000;    // ... per this long

		// RPCs a HOST legitimately sends once PER PLAYER at game start (role + task assignment). In a large
		// (modded) lobby that's a big same-callId burst from the host, so they're exempt from flood detection
		// to avoid false-flagging the host. Everything else has no legitimate 50+/sec same-callId use.
		private static readonly HashSet<byte> FloodExemptRpcs = new HashSet<byte>()
		{
			(byte)RpcCalls.SetRole,
			(byte)RpcCalls.SetTasks,
		};

		// Custom GameData message tags we've already surfaced (log once each).
		private static readonly HashSet<int> _customTagsSeen = new HashSet<int>();

		// Handshake callIds whose payload begins with human-readable name/version strings. Reading them turns
		// "Modded (RPC 242)" into "OnyxMenu Nocturne 2.3". Only these are parsed - we never read arbitrary RPCs.
		private static readonly HashSet<byte> HandshakePayloadRpcs = new HashSet<byte>() { 164, 242, 250 };

		public static void Reset()
		{
			_detected.Clear();
			_flood.Clear();
			_customTagsSeen.Clear();
		}

		public static void Observe(PlayerControl player, byte callId, MessageReader reader = null)
		{
			if(player == null || player.Data == null || player.AmOwner) return;

			// Flood/overload detection runs for EVERY RPC (vanilla or not) before we decide if the callId
			// itself is a fingerprint - a pet-flood uses a vanilla callId. Guarded so it can never break RPC
			// handling.
			try { CheckFlood(player, callId); } catch { }

			string label;   // short tag kept per player (shown as the "Client" label)
			string reason;  // longer text stored on the blacklist entry - always includes the raw callId
			if(KnownMods.TryGetValue(callId, out var mod))
			{
				label = mod;
				reason = $"Sent modded RPC {callId} ({mod})";
			}
			else if(IsNonVanilla(callId))
			{
				label = $"Modded (RPC {callId})";
				reason = $"Sent modded RPC {callId}";
			}
			else
				return; // ordinary vanilla RPC - tells us nothing (flood was already counted above)

			// Some menus put their name/version right in the handshake payload. Peek it (never consume the
			// reader) for a precise label.
			string payload = TryReadHandshakePayload(callId, reader);
			if(!string.IsNullOrEmpty(payload))
			{
				label = $"{label} [{payload}]";
				reason = $"{reason} - {payload}";
			}

			Record(player.PlayerId, label);

			// A non-vanilla / custom RPC is a modded client caught in the act - auto-blacklist them (no-op
			// when auto-detect is off, or for ourselves / a menu dev). The reason records the exact callId so
			// a false positive (e.g. a new vanilla RPC mislabelled as a known menu) can be traced.
			Blacklist.AutoFlag(player, reason);
		}

		// Flags a sender who emits an abnormal burst of the SAME callId in a short window (e.g. a pet-flood /
		// overload attack). The threshold is far above any legitimate same-callId rate, so this does not
		// false-flag normal play. (A single oversized flood message is dropped earlier by AntiOverload and so
		// may not reach here - this catches the multi-message / sub-limit case.)
		private static void CheckFlood(PlayerControl player, byte callId)
		{
			if(FloodExemptRpcs.Contains(callId)) return; // host role/task assignment - legitimately bursty

			int key = (player.PlayerId << 8) | callId;
			long now = System.Environment.TickCount64;
			_flood.TryGetValue(key, out var e);
			if(now - e.start > FloodWindowMs) e = (now, 0); // window expired (or first time) -> reset
			e.count++;
			_flood[key] = e;
			if(e.count == FloodThreshold + 1) // fire exactly once as it crosses the threshold
				Blacklist.AutoFlag(player, $"Sent an abnormal burst of RPC {callId} ({FloodThreshold}+/sec) - overload/flood attack");
		}

		// Reads up to two leading strings from a known handshake RPC's payload WITHOUT consuming the reader
		// (position is always restored). Returns null on anything unexpected, so a format change can never
		// break RPC handling or produce garbage.
		private static string TryReadHandshakePayload(byte callId, MessageReader reader)
		{
			if(reader == null || !HandshakePayloadRpcs.Contains(callId)) return null;
			int pos = reader.Position;
			try
			{
				var parts = new List<string>();
				for(int i = 0; i < 2 && reader.BytesRemaining > 0; i++)
				{
					string s = reader.ReadString();
					// Stop on anything that isn't a clean, readable name/version token - a non-handshake custom
					// RPC's binary payload must never surface as garbage in the label.
					if(string.IsNullOrEmpty(s) || s.Length > 24 || !IsCleanToken(s)) break;
					parts.Add(s);
				}
				return parts.Count > 0 ? string.Join(" ", parts) : null;
			}
			catch { return null; }
			finally { try { reader.Position = pos; } catch { } }
		}

		// A readable name/version token: printable ASCII only, with at least one letter or digit. Rejects the
		// binary payloads of non-handshake custom RPCs so they can't produce junk labels.
		private static bool IsCleanToken(string s)
		{
			bool hasAlnum = false;
			foreach(char c in s)
			{
				if(c < ' ' || c > '~') return false; // control / non-ASCII byte -> not a handshake string
				if((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) hasAlnum = true;
			}
			return hasAlnum;
		}

		// A handshake can also arrive as a GameData message with a NON-vanilla tag, which AntiOverload's
		// invalid-message filter drops before any RPC handler runs. We surface those here so they're not
		// silently lost. Log-only (no auto-flag): modded SERVERS also use custom tags, so flagging would
		// false-positive - this just makes a custom-tag menu visible for investigation.
		public static void ObserveCustomTag(byte tag)
		{
			try
			{
				if(!_customTagsSeen.Add(tag)) return;
				Hydra.Log.LogInfo($"[ModDetection] Observed non-vanilla GameData message tag {tag} - a modded client or server is present.");
			}
			catch { }
		}

		// Starlight is a third-party Android BepInEx loader that reports platform ID 112, so a
		// player on platform 112 is running it. This is a platform signature, not an RPC one.
		private const int StarlightPlatformId = 112;

		public static void CheckPlatform(PlayerControl player)
		{
			if(player == null || player.Data == null || player.AmOwner) return;
			try
			{
				var client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(player) : null;
				if(client != null && (int)client.PlatformData.Platform == StarlightPlatformId)
				{
					Record(player.PlayerId, "Starlight");
					// Platform-based signature, so there's no RPC callId - record the platform id instead
					// (parallels how RPC detections record the callId in the reason).
					Blacklist.AutoFlag(player, $"Starlight loader (platform {StarlightPlatformId})");
				}
			}
			catch { }
		}

		private static void Record(byte playerId, string label)
		{
			if(!_detected.TryGetValue(playerId, out var set))
				set = _detected[playerId] = new HashSet<string>();
			set.Add(label);
		}

		private static bool IsNonVanilla(byte callId)
		{
			if(System.Enum.IsDefined(typeof(RpcCalls), (RpcCalls)callId)) return false;
			if(ExtraVanillaRpcs.Contains(callId)) return false;
			return true;
		}

		public static bool IsDetected(byte playerId)
			=> _detected.TryGetValue(playerId, out var set) && set.Count > 0;

		public static string GetClientLabel(byte playerId)
		{
			if(_detected.TryGetValue(playerId, out var set) && set.Count > 0)
				return string.Join(", ", set);
			return "Unmodded";
		}

		[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
		private static class ResetOnJoin
		{
			private static void Postfix() => Reset();
		}

		// Platform-based detection runs when a player spawns (platform data is available by then).
		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
		private static class CheckPlatformOnStart
		{
			private static void Postfix(PlayerControl __instance) => CheckPlatform(__instance);
		}
	}
}
