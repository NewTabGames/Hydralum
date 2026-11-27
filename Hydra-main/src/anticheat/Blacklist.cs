using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HydraMenu.anticheat
{
	// A persistent, local blacklist of players. Flagged players get a red "Flagged" tag above their name
	// (see BlacklistEsp) and are remembered across games, so a cheater you catch once stays flagged the
	// next time they join.
	//
	// Identity is keyed on PUID (ProductUserId) - the most persistent per-account id - then falls back to
	// friend code. Names are NOT used for matching (too broad: names collide and change), only stored for
	// display. A player with neither a PUID nor a friend code can't be flagged. The list lives in its OWN
	// folder+file under the config directory, independent of the Save/Load config system, so switching
	// configs never loses your blacklist.
	internal static class Blacklist
	{
		// <BepInEx config>/Hydra/Blacklist/blacklist.json
		public static readonly string FOLDER_PATH = Path.Combine(Paths.ConfigPath, "Hydra", "Blacklist");
		private static string FilePath => Path.Combine(FOLDER_PATH, "blacklist.json");

		// When on, players the anticheat / mod-detection catches sending invalid or non-vanilla (modded)
		// RPCs are added to the blacklist automatically. Manual flagging from the Blacklist tab works
		// regardless. Persisted in the blacklist file.
		public static bool AutoDetect = true;

		// When on, flagged players are automatically removed on sight: BANNED if you're the host, or kicked via
		// the server-authoritative exploit (once the round has started) if you're not. Persisted in the
		// blacklist file alongside AutoDetect.
		public static bool AutoKick = false;

		public class Entry
		{
			public string Puid { get; set; }
			public string FriendCode { get; set; }
			public string Name { get; set; }
			public string Reason { get; set; }
			public bool Auto { get; set; }
		}

		// On-disk shape of the file.
		private class FileData
		{
			public bool AutoDetect { get; set; } = true;
			public bool AutoKick { get; set; } = false;
			public List<Entry> Entries { get; set; } = new List<Entry>();
		}

		private static readonly List<Entry> _entries = new List<Entry>();
		private static readonly HashSet<string> _puids = new HashSet<string>();
		private static readonly HashSet<string> _friendCodes = new HashSet<string>();

		public static IReadOnlyList<Entry> Entries => _entries;

		// Called on plugin load (Hydra.Load). Creates the Blacklist folder if missing and loads the list.
		public static void Initialize()
		{
			try
			{
				if (!Directory.Exists(FOLDER_PATH))
				{
					Hydra.Log.LogInfo("No blacklist folder was found, creating...");
					Directory.CreateDirectory(FOLDER_PATH);
				}
				Load();
			}
			catch { }
		}

		private static void RebuildIndex()
		{
			_puids.Clear();
			_friendCodes.Clear();
			foreach (var e in _entries)
			{
				if (!string.IsNullOrEmpty(e.Puid)) _puids.Add(e.Puid);
				if (!string.IsNullOrEmpty(e.FriendCode)) _friendCodes.Add(e.FriendCode);
			}
		}

		private static void Load()
		{
			_entries.Clear();
			try
			{
				if (File.Exists(FilePath))
				{
					var data = JsonSerializer.Deserialize<FileData>(File.ReadAllText(FilePath));
					if (data != null)
					{
						AutoDetect = data.AutoDetect;
						AutoKick = data.AutoKick;
						if (data.Entries != null) _entries.AddRange(data.Entries.Where(e => e != null));
					}
				}
			}
			catch { }
			RebuildIndex();
		}

		public static void Save()
		{
			try
			{
				if (!Directory.Exists(FOLDER_PATH)) Directory.CreateDirectory(FOLDER_PATH);
				var opts = new JsonSerializerOptions { WriteIndented = true };
				var data = new FileData { AutoDetect = AutoDetect, AutoKick = AutoKick, Entries = _entries };
				File.WriteAllText(FilePath, JsonSerializer.Serialize(data, opts));
			}
			catch { }
		}

		// Reads the most stable identity we can for a player: PUID, then friend code, plus last-known name.
		private static (string puid, string fc, string name) GetIdentity(PlayerControl p)
		{
			string name = "";
			string fc = null;
			string puid = null;
			try
			{
				if (p.Data != null)
				{
					name = p.Data.PlayerName ?? "";
					if (!string.IsNullOrEmpty(p.Data.FriendCode)) fc = p.Data.FriendCode;
				}

				var cd = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(p) : null;
				if (cd != null)
				{
					if (!string.IsNullOrEmpty(cd.ProductUserId)) puid = cd.ProductUserId;
					if (string.IsNullOrEmpty(fc) && !string.IsNullOrEmpty(cd.FriendCode)) fc = cd.FriendCode;
				}
			}
			catch { }
			return (puid, fc, name);
		}

		public static bool IsBlacklisted(PlayerControl p)
		{
			if (p == null) return false;
			var (puid, fc, _) = GetIdentity(p);
			return IsBlacklistedIdentity(puid, fc);
		}

		private static bool IsBlacklistedIdentity(string puid, string fc)
		{
			if (!string.IsNullOrEmpty(puid) && _puids.Contains(puid)) return true;
			if (!string.IsNullOrEmpty(fc) && _friendCodes.Contains(fc)) return true;
			return false;
		}

		// Manual flag from the Blacklist tab. Returns true if a new entry was added.
		public static bool AddManual(PlayerControl p)
		{
			if (p == null) return false;
			return AddInternal(p, "Manually flagged", false);
		}

		// Auto flag from the anticheat / mod-detection. No-op when AutoDetect is off, for ourselves, or for
		// a menu developer (devs legitimately emit custom RPCs; flagging them would be noise).
		public static void AutoFlag(PlayerControl p, string reason)
		{
			if (!AutoDetect || p == null || p.Data == null) return;
			if (p == PlayerControl.LocalPlayer || p.AmOwner) return;
			try { if (PresenceTracker.IsDevUser(p.Data)) return; } catch { }

			// AddInternal returns true only the first time a player is added, so this fires once per newly
			// flagged player rather than every frame their RPCs keep coming in.
			if (AddInternal(p, reason, true))
			{
				try
				{
					string shown = System.Text.RegularExpressions.Regex.Replace(p.Data.PlayerName ?? "A player", "<.*?>", string.Empty);
					Hydra.notifications.Send("Blacklist", $"{shown} was flagged ({reason}).", 7);
				}
				catch { }
			}
		}

		private static bool AddInternal(PlayerControl p, string reason, bool auto)
		{
			var (puid, fc, name) = GetIdentity(p);

			if (IsBlacklistedIdentity(puid, fc))
			{
				// Already flagged - refresh the last-known name AND merge in any NEW reason, so the entry
				// reflects everything they've been caught for rather than only the first thing.
				bool changed = UpdateName(puid, fc, name);
				if (AddReason(puid, fc, reason)) changed = true;
				if (changed) Save();
				return false;
			}

			// No stable id to remember them by - can't flag.
			if (string.IsNullOrEmpty(puid) && string.IsNullOrEmpty(fc)) return false;

			var entry = new Entry { Puid = puid, FriendCode = fc, Name = name, Reason = reason, Auto = auto };
			_entries.Add(entry);
			if (!string.IsNullOrEmpty(puid)) _puids.Add(puid);
			if (!string.IsNullOrEmpty(fc)) _friendCodes.Add(fc);
			Save();
			return true;
		}

		// Updates the stored (display) name of every entry matching this identity to `name`. Returns true if
		// anything actually changed, so the caller persists only when needed. Keyed on PUID / friend code,
		// never on the name itself, so it correctly follows a cheater across any number of name changes.
		private static bool UpdateName(string puid, string fc, string name)
		{
			if (string.IsNullOrEmpty(name)) return false;
			bool changed = false;
			foreach (var e in _entries)
			{
				bool match = (!string.IsNullOrEmpty(puid) && e.Puid == puid) || (!string.IsNullOrEmpty(fc) && e.FriendCode == fc);
				if (match && e.Name != name)
				{
					e.Name = name;
					changed = true;
				}
			}
			return changed;
		}

		// Merges a new reason into every entry matching this identity, de-duping so the same reason is never
		// listed twice. Returns true if anything changed, so an already-flagged player accumulates the full set
		// of things they've been caught for (e.g. "illegal cosmetic | modded RPC | overflow attempt").
		private static bool AddReason(string puid, string fc, string reason)
		{
			if (string.IsNullOrEmpty(reason)) return false;
			bool changed = false;
			foreach (var e in _entries)
			{
				bool match = (!string.IsNullOrEmpty(puid) && e.Puid == puid) || (!string.IsNullOrEmpty(fc) && e.FriendCode == fc);
				if (!match) continue;

				if (string.IsNullOrEmpty(e.Reason))
				{
					e.Reason = reason;
					changed = true;
				}
				else if (!ReasonAlreadyListed(e.Reason, reason))
				{
					e.Reason = e.Reason + " | " + reason;
					changed = true;
				}
			}
			return changed;
		}

		private static bool ReasonAlreadyListed(string existing, string reason)
		{
			foreach (var part in existing.Split('|'))
				if (string.Equals(part.Trim(), reason.Trim(), System.StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}

		// Called when a blacklisted player is encountered in a lobby/game (driven from BlacklistEsp). If the
		// player is on the list but is now using a different name than we have stored, update the stored name
		// to the one they are using now and persist it. Because matching is on PUID / friend code, this keeps
		// working no matter how many times they rename, and only writes to disk when the name actually changes.
		public static void SyncName(PlayerControl p)
		{
			if (p == null) return;
			var (puid, fc, name) = GetIdentity(p);
			if (string.IsNullOrEmpty(name)) return;
			if (!IsBlacklistedIdentity(puid, fc)) return;
			if (UpdateName(puid, fc, name)) Save();
		}

		// Unflag a currently-online player (matches by PUID or friend code).
		public static void RemovePlayer(PlayerControl p)
		{
			if (p == null) return;
			var (puid, fc, _) = GetIdentity(p);
			_entries.RemoveAll(e =>
				(!string.IsNullOrEmpty(puid) && e.Puid == puid) ||
				(!string.IsNullOrEmpty(fc) && e.FriendCode == fc));
			RebuildIndex();
			Save();
		}

		public static void Remove(Entry entry)
		{
			if (entry == null) return;
			_entries.Remove(entry);
			RebuildIndex();
			Save();
		}

		// Throttle so we don't resend the kick RPC every FixedUpdate while a flagged player is still
		// disconnecting. Keyed on owner id; a rejoin gets a fresh id and is re-kicked after the window.
		private static readonly Dictionary<int, long> _lastAutoKick = new Dictionary<int, long>();
		private const long AutoKickCooldownMs = 5000;

		// Called per flagged player (driven from BlacklistEsp's per-frame sweep). When Auto-Kick is on, remove
		// the flagged player on sight - throttled so we don't fire every FixedUpdate. Routing mirrors the
		// Players > Kick Player feature via Utilities.KickPlayer: as HOST it bans them from the lobby; as a
		// NON-HOST it runs the server-authoritative kick exploit, which only works once the round has started,
		// so we wait for ShipStatus (and a server-authoritative lobby) before trying.
		public static void EnforceAutoKick(PlayerControl p)
		{
			if (!AutoKick) return;
			try
			{
				if (p == null || p.Data == null || p.Data.Disconnected) return;
				if (AmongUsClient.Instance == null) return;
				if (p == PlayerControl.LocalPlayer || p.AmOwner) return;        // never target yourself
				try { if (PresenceTracker.IsDevUser(p.Data)) return; } catch { } // never target a developer

				bool host = AmongUsClient.Instance.AmHost;
				if (!host)
				{
					// Non-host kick only works in a server-authoritative lobby, after the round starts, and
					// never against the lobby host. Bail quietly otherwise so we don't spam notifications.
					if (ShipStatus.Instance == null) return;
					if (p.OwnerId == AmongUsClient.Instance.HostId) return;
					if (!Utilities.IsAnticheatPresent()) return;
				}

				int ownerId = p.OwnerId;
				long now = System.Environment.TickCount64;
				if (_lastAutoKick.TryGetValue(ownerId, out long last) && now - last < AutoKickCooldownMs) return;
				_lastAutoKick[ownerId] = now;

				// Host -> ban; non-host -> server-authoritative kick. Utilities.KickPlayer picks the path and
				// handles its own notification.
				Utilities.KickPlayer(p);
			}
			catch { }
		}

		public static void Clear()
		{
			_entries.Clear();
			RebuildIndex();
			Save();
		}
	}
}
