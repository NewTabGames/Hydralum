using System;
using System.Diagnostics;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class MenuSection : Section
	{
		public MenuSection() : base("Menu") { }

		private byte configIndex = 0;
		private string configNameInput = "";
		private bool typingConfigName = false;

		// Builds the config name from raw GUI key events. GUILayout.TextField is unstripped under IL2CPP
		// and crashes ("Method unstripping failed"), so we capture typed characters ourselves instead.
		private void CaptureConfigNameInput()
		{
			var e = Event.current;
			if(e == null || e.type != EventType.KeyDown) return;

			if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
			{
				typingConfigName = false;
				e.Use();
				return;
			}

			if(e.keyCode == KeyCode.Backspace)
			{
				if(!string.IsNullOrEmpty(configNameInput))
					configNameInput = configNameInput.Substring(0, configNameInput.Length - 1);
				e.Use();
				return;
			}

			char c = e.character;
			if(c != '\0' && !char.IsControl(c) && configNameInput.Length < 32)
			{
				configNameInput += c;
				e.Use();
			}
		}

		public override void Render()
		{
			// GUILayout.Label($"Texture 2D memory usage: {Texture2D.currentTextureMemory}");
			Hydra.notifications.disableNotifications = GUILayout.Toggle(Hydra.notifications.disableNotifications, "Disable Notifications");

			if(GUILayout.Button("Eject"))
			{
				Hydra.Eject();
			}

			GUILayout.Space(5);
			GUILayout.Label($"Config:\nCurrent Config: {Hydra.config.currentConfig}");

			// Keep the selection in range (a delete can shrink the list beneath the old index)
			if(configIndex >= Hydra.config.configList.Count) configIndex = (byte)(Hydra.config.configList.Count - 1);

			GUILayout.Label($"Selected Config: {Hydra.config.configList[configIndex]}");
			configIndex = (byte)GUILayout.HorizontalSlider(configIndex, 0, Hydra.config.configList.Count - 1);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Save"))
			{
				Hydra.config.SaveConfig(Hydra.config.configList[configIndex]);
				Hydra.notifications.Send("Config", $"Saved '{Hydra.config.configList[configIndex]}'.");
			}

			if(GUILayout.Button("Load"))
			{
				Hydra.config.LoadConfig(Hydra.config.configList[configIndex]);
				Hydra.notifications.Send("Config", $"Loaded '{Hydra.config.configList[configIndex]}'.");
			}

			if(GUILayout.Button("Delete"))
			{
				string toDelete = Hydra.config.configList[configIndex];
				if(Hydra.config.DeleteConfig(toDelete))
				{
					if(configIndex >= Hydra.config.configList.Count) configIndex = (byte)(Hydra.config.configList.Count - 1);
					Hydra.notifications.Send("Config", $"Deleted '{toDelete}'.");
				}
				else
				{
					Hydra.notifications.Send("Config", "Can't delete the last config.");
				}
			}
			GUILayout.EndHorizontal();

			// Name field used by New (named) and Rename. TextField is unstripped under IL2CPP, so this
			// is a click-to-type button backed by manual key capture instead.
			string nameLabel = typingConfigName
				? (string.IsNullOrEmpty(configNameInput) ? "Type a name..." : configNameInput + "_")
				: (string.IsNullOrEmpty(configNameInput) ? "Name: (click to type)" : "Name: " + configNameInput);
			if(GUILayout.Button(nameLabel)) typingConfigName = !typingConfigName;
			if(typingConfigName) CaptureConfigNameInput();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("New"))
			{
				// Use the typed name if given, otherwise auto-generate one ("Hydra 1", "Hydra 2", ...)
				if(!string.IsNullOrWhiteSpace(configNameInput))
				{
					if(Hydra.config.CreateNamedConfig(configNameInput))
					{
						configIndex = (byte)Hydra.config.configList.IndexOf(Hydra.config.currentConfig);
						Hydra.notifications.Send("Config", $"Created '{Hydra.config.currentConfig}'.");
						configNameInput = "";
						typingConfigName = false;
					}
					else
					{
						Hydra.notifications.Send("Config", "Name is empty or already exists.");
					}
				}
				else
				{
					string configName = Hydra.config.GetUnusedConfigName();
					// I doubt anyone will actually have 255 configs with the pattern of "Hydra [1-255]", but just in case...
					if(configName == null)
					{
						Hydra.notifications.Send("Config", "Failed to find an unused config name.");
						return;
					}

					Hydra.config.CreateNewConfig(configName);
					configIndex = (byte)Hydra.config.configList.IndexOf(Hydra.config.currentConfig);
				}
			}

			if(GUILayout.Button("Rename"))
			{
				string oldName = Hydra.config.configList[configIndex];
				if(Hydra.config.RenameConfig(oldName, configNameInput))
				{
					configIndex = (byte)Hydra.config.configList.IndexOf(Hydra.config.SanitizeConfigName(configNameInput));
					Hydra.notifications.Send("Config", $"Renamed '{oldName}'.");
					configNameInput = "";
					typingConfigName = false;
				}
				else
				{
					Hydra.notifications.Send("Config", "Name is empty or already exists.");
				}
			}
			GUILayout.EndHorizontal();

			if(GUILayout.Button("Open Config Folder"))
			{
				Process.Start("explorer.exe", Hydra.config.CONFIG_PATH);
			}
		}
	}
}