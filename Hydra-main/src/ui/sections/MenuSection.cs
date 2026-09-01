using System;
using System.Diagnostics;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class MenuSection : Section
	{
		public MenuSection() : base("Menu") { }

		private byte configIndex = 0;

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

			GUILayout.Label($"Selected Config: {Hydra.config.configList[configIndex]}");
			configIndex = (byte)GUILayout.HorizontalSlider(configIndex, 0, Hydra.config.configList.Count - 1);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Save"))
			{
				Hydra.config.SaveConfig(Hydra.config.configList[configIndex]);
			}

			if(GUILayout.Button("Load"))
			{
				Hydra.config.LoadConfig(Hydra.config.configList[configIndex]);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("New Config"))
			{
				string configName = Hydra.config.GetUnusedConfigName();
				// I doubt anyone will actually have 255 configs with the pattern of "Hydra [1-255]", but just in case...
				if(configName == null)
				{
					Hydra.notifications.Send("Config", "Failed to find an unused config name.");
					return;
				}

				Hydra.config.CreateNewConfig(configName);
			}

			if(GUILayout.Button("Open Config Folder"))
			{
				Process.Start("explorer.exe", Hydra.config.CONFIG_PATH);
			}
			GUILayout.EndHorizontal();
		}
	}
}