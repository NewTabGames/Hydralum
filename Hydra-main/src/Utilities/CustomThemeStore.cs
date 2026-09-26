using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx;

namespace HydraMenu
{
	// Shared saved-theme store for both Hydralum menus. Paths.ConfigPath is identical for every plugin in
	// the install, so the folder is the same for Hydra and MalumMenu — a theme saved in one menu shows up
	// in the other. Each theme is a .txt whose contents are the color value: "#RRGGBB" or
	// "grad:#AAAAAA,#BBBBBB" (the same format ThemeColor / Malum's menuHtmlColor already use).
	public static class CustomThemeStore
	{
		public static string Dir => Path.Combine(Paths.ConfigPath, "CThemes");
		public static string PathFor(string name) => Path.Combine(Dir, name + ".txt");

		// Cleans a user-entered name into something safe to use as a filename. Returns null if unusable.
		public static string Sanitize(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;
			name = name.Trim();
			foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
			if (name.Length > 24) name = name.Substring(0, 24);
			return string.IsNullOrWhiteSpace(name) ? null : name;
		}

		// Every saved theme as (name, value), sorted by name. Re-read from disk each call so a theme saved
		// by the other menu shows up without any cross-plugin messaging.
		public static List<(string name, string value)> LoadAll()
		{
			var list = new List<(string name, string value)>();
			try
			{
				if (!Directory.Exists(Dir)) return list;
				foreach (var f in Directory.GetFiles(Dir, "*.txt"))
				{
					try
					{
						string val = File.ReadAllText(f).Trim();
						if (!string.IsNullOrEmpty(val))
							list.Add((Path.GetFileNameWithoutExtension(f), val));
					}
					catch { }
				}
				list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
			}
			catch { }
			return list;
		}

		public static bool Exists(string rawName)
		{
			var name = Sanitize(rawName);
			return name != null && File.Exists(PathFor(name));
		}

		public static bool Save(string rawName, string value)
		{
			var name = Sanitize(rawName);
			if (name == null || string.IsNullOrWhiteSpace(value)) return false;
			try
			{
				Directory.CreateDirectory(Dir);
				File.WriteAllText(PathFor(name), value.Trim());
				return true;
			}
			catch { return false; }
		}

		public static bool Delete(string rawName)
		{
			var name = Sanitize(rawName);
			if (name == null) return false;
			try
			{
				var p = PathFor(name);
				if (File.Exists(p)) File.Delete(p);
				return true;
			}
			catch { return false; }
		}

		public static void OpenFolder()
		{
			try
			{
				Directory.CreateDirectory(Dir);
				Process.Start("explorer.exe", Dir);
			}
			catch { }
		}
	}
}
