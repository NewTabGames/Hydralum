using BepInEx;
using HydraMenu.anticheat;
using HydraMenu.ui;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HydraMenu.modules
{
	internal class ConfigManager
	{
		public readonly string CONFIG_PATH = Path.Combine(Paths.ConfigPath, "Hydra");

		public readonly List<string> configList = new List<string>();
		public string currentConfig = "Hydra";

		public class ConfigData
		{
			public MainUI.MainUIConfig Menu { get; set; }
			public Dictionary<string, Dictionary<string, JsonElement>> Modules { get; set; }
			public Dictionary<string, Dictionary<string, JsonElement>> Routines { get; set; }
			public Anticheat.AnticheatConfigData Anticheat { get; set; }
		}

		public void Initialize()
		{
			if(!Directory.Exists(CONFIG_PATH))
			{
				Hydra.Log.LogInfo("No config folder was found, creating...");
				Directory.CreateDirectory(CONFIG_PATH);

				configList.Add(currentConfig);
				SaveConfig(currentConfig);
				SaveLastActive();
				return;
			}

			string[] configFiles = Directory.GetFiles(CONFIG_PATH, "*.json");
			Hydra.Log.LogInfo($"Discovered {configFiles.Length} config files");

			foreach(string file in configFiles)
			{
				configList.Add(Path.GetFileNameWithoutExtension(file));
			}

			// Folder exists but holds no configs (e.g. they were all deleted) — seed the default one
			if(configList.Count == 0)
			{
				configList.Add(currentConfig);
				SaveConfig(currentConfig);
				SaveLastActive();
				return;
			}

			// Decide which config to load on startup: the one that was active last launch if it still
			// exists, otherwise "Hydra" if present, otherwise the first available. This is what stops a
			// renamed config from being ignored (and a blank Hydra.json from being recreated) next launch.
			string lastActive = ReadLastActive();
			if(lastActive != null && configList.Contains(lastActive)) currentConfig = lastActive;
			else if(configList.Contains("Hydra")) currentConfig = "Hydra";
			else currentConfig = configList[0];

			LoadConfig(currentConfig);
			SaveLastActive();
		}

		// Path of the "which config was active" marker. A plain .txt so the *.json discovery above skips it.
		private string LastActivePath => Path.Combine(CONFIG_PATH, "last_config.txt");

		// Remembers/reads the active config name so the next launch reloads it instead of always
		// defaulting to "Hydra".
		private void SaveLastActive()
		{
			try { File.WriteAllText(LastActivePath, currentConfig); }
			catch { Hydra.Log.LogWarning("Failed to write last active config marker"); }
		}

		private string ReadLastActive()
		{
			try { if(File.Exists(LastActivePath)) return File.ReadAllText(LastActivePath).Trim(); }
			catch { }
			return null;
		}

		public string GetConfigPath(string configName)
		{
			return Path.Combine(CONFIG_PATH, configName + ".json");
		}

		public void LoadConfig(string configName)
		{
			string configLocation = GetConfigPath(configName);
			if(!File.Exists(configLocation))
			{
				Hydra.Log.LogWarning($"Tried to load config {configName} when no such config exists");
				// Let's just carry on with our current config
				return;
			}

			string configString = File.ReadAllText(configLocation);

			ConfigData configData = null;
			try
			{
				configData = JsonSerializer.Deserialize<ConfigData>(configString);
			}
			catch
			{
				Hydra.Log.LogError($"Failed to load config at {configLocation}");
				return;
			}

			Hydra.mainUI.LoadConfigData(configData.Menu);
			Hydra.modules.LoadConfigData(configData.Modules);
			Hydra.routines.LoadConfigData(configData.Routines);
			Anticheat.LoadConfigData(configData.Anticheat);

			currentConfig = configName;
			SaveLastActive();
			Hydra.Log.LogInfo($"Loaded config {configName}");
		}

		public void SaveConfig(string configName)
		{
			string configLocation = GetConfigPath(configName);

			ConfigData configData = new ConfigData();
			configData.Menu = Hydra.mainUI.GetConfigData();
			configData.Modules = Hydra.modules.GetConfigData();
			configData.Routines = Hydra.routines.GetConfigData();
			configData.Anticheat = Anticheat.GetConfigData();

			JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
			serializerOptions.WriteIndented = true;

			string configString = JsonSerializer.Serialize(configData, serializerOptions);
			File.WriteAllText(configLocation, configString);

			Hydra.Log.LogInfo($"Config {configName} has been saved to {configLocation}");
		}

		public string GetUnusedConfigName()
		{
			HashSet<string> configHashList = configList.ToHashSet();
			string match = null;

			// https://stackoverflow.com/a/27289807
			char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			string baseConfigName = currentConfig.TrimEnd(digits).Trim();

			for(int i = 1; i < 255; i++)
			{
				string configName = baseConfigName + " " + i;
				if(configHashList.Contains(configName)) continue;

				match = configName;
				break;
			}

			return match;
		}

		public void CreateNewConfig(string configName)
		{
			// First save our old config
			SaveConfig(currentConfig);

			// Then create our new config
			configList.Add(configName);
			SaveConfig(configName);
			currentConfig = configName;
			SaveLastActive();
		}

		// Cleans a user-entered name into something safe to use as a filename. Returns null if unusable.
		public string SanitizeConfigName(string name)
		{
			if(string.IsNullOrWhiteSpace(name)) return null;
			name = name.Trim();
			foreach(char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
			if(name.Length > 32) name = name.Substring(0, 32);
			return string.IsNullOrWhiteSpace(name) ? null : name;
		}

		// Creates a config with a user-provided name (the "add" half of the manager). Returns false if
		// the name is invalid or already taken.
		public bool CreateNamedConfig(string configName)
		{
			configName = SanitizeConfigName(configName);
			if(configName == null || configList.Contains(configName)) return false;

			// Save the config we're leaving, then create and switch to the new one
			SaveConfig(currentConfig);
			configList.Add(configName);
			SaveConfig(configName);
			currentConfig = configName;
			SaveLastActive();
			return true;
		}

		// Deletes a config file. Refuses to delete the very last one so there's always a config to use.
		// If the deleted config was current, switches to (and loads) another.
		public bool DeleteConfig(string configName)
		{
			if(!configList.Contains(configName) || configList.Count <= 1) return false;

			try
			{
				string path = GetConfigPath(configName);
				if(File.Exists(path)) File.Delete(path);
			}
			catch
			{
				Hydra.Log.LogError($"Failed to delete config {configName}");
				return false;
			}

			configList.Remove(configName);

			if(currentConfig == configName)
			{
				currentConfig = configList[0];
				LoadConfig(currentConfig);
			}

			return true;
		}

		// Renames a config file. Returns false if the new name is invalid/taken or the old one is missing.
		public bool RenameConfig(string oldName, string newName)
		{
			newName = SanitizeConfigName(newName);
			if(newName == null || !configList.Contains(oldName) || configList.Contains(newName)) return false;

			// Persist the latest state into the old file first if it's the active config
			if(currentConfig == oldName) SaveConfig(oldName);

			try
			{
				File.Move(GetConfigPath(oldName), GetConfigPath(newName));
			}
			catch
			{
				Hydra.Log.LogError($"Failed to rename config {oldName} to {newName}");
				return false;
			}

			configList.Remove(oldName);
			configList.Add(newName);
			if(currentConfig == oldName)
			{
				currentConfig = newName;
				SaveLastActive();
			}
			return true;
		}
	}
}