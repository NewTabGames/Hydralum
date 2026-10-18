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
				var data = new FileData { AutoDetect = AutoDetect, Entries = _entries };
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
				// Already flagged - just refresh the last-known name so the list stays readable.
				UpdateName(puid, fc, name);
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

		private static void UpdateName(string puid, string fc, string name)
		{
			if (string.IsNullOrEmpty(name)) return;
			foreach (var e in _entries)
			{
				bool match = (!string.IsNullOrEmpty(puid) && e.Puid == puid) || (!string.IsNullOrEmpty(fc) && e.FriendCode == fc);
				if (match) e.Name = name;
			}
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

		public static void Clear()
		{
			_entries.Clear();
			RebuildIndex();
			Save();
		}
	}
}
