using HydraMenu.modules;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class InfoSection : Section
	{
		public InfoSection() : base("Info") { }

		private const string HydralumUrl = "https://github.com/NewTabGames/Hydralum";
		private const string HydralumDiscordUrl = "https://discord.gg/GBg7hp7qAX";
		private const string MalumMenuUrl = "https://github.com/scp222thj/MalumMenu";
		private const string MalumDiscordUrl = "https://discord.gg/MMg8W7T3Cy";
		private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";
		private const string HydraDiscordUrl = "https://discord.gg/Yd4WVvxsm6";

		private bool _showKeybindInfo = false;

		public override void Render()
		{
			GUILayout.Label($"<b>Hydralum</b> v{PresenceTracker.CurrentHydralumVersion} (Malum Menu v3.3.0 | Hydra Menu v2.0.0)");
			GUILayout.Label("A fork of Hydra, with features drawn from MalumMenu.");
			GUILayout.Space(6);
			int online = PresenceTracker.GetOnlineCount();
			GUILayout.Label($"<b>Live Users Online:</b> <color=#00FF88>{online} {(online == 1 ? "player" : "players")}</color>");

			GUILayout.Space(8);
			_showKeybindInfo = GUILayout.Toggle(_showKeybindInfo, " Show Keybind Info");

			if (_showKeybindInfo)
			{
				GUILayout.Space(4);
				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.Label("<b>Keybind Reference & Shortcuts:</b>");
				GUILayout.Space(4);

				DrawKeybindRow("Menu Key", "Toggle Active Menu (Customizable in Menu Tab)");
				DrawKeybindRow("Switch (Button)", "Switch between MalumMenu and HydraMenu in-place");
				DrawKeybindRow("Escape (Important)", "Dismiss Match Info Guide, dialogs, and fix menu softlocks");

				GUILayout.EndVertical();
			}

			GUILayout.Space(12);
			GUILayout.Label("<b>Credits & Community</b>");

			DrawCredit("Hydralum", "Official Discord & GitHub", HydralumUrl, HydralumDiscordUrl);
			DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumMenuUrl, MalumDiscordUrl);
			DrawCredit("Hydra", "by MrDiamond64", HydraUrl, HydraDiscordUrl);

			GUILayout.Space(12);
			GUILayout.Label("<color=#9A9A9A>MalumMenu and Hydra are both licensed under GPL-3.0.</color>");
			GUILayout.Space(8);
			GUILayout.Label("<b>Note:</b> These menus were combined using AI. If you don't like it, don't use it.");
		}

		private static void DrawKeybindRow(string key, string description)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label($"<color=#00FFAA><b>[{key}]</b></color>", GUILayout.Width(170));
			GUILayout.Label(description);
			GUILayout.EndHorizontal();
		}

		private static void DrawCredit(string title, string author, string githubUrl, string discordUrl = null)
		{
			GUILayout.Space(6);
			GUILayout.Label($"<b>{title}</b> {author}");
			GUILayout.Label($"<color=#9A9A9A>{githubUrl}</color>");

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open GitHub", GUILayout.Width(130)))
			{
				Application.OpenURL(githubUrl);
			}

			if (!string.IsNullOrEmpty(discordUrl))
			{
				if (GUILayout.Button("Open Discord", GUILayout.Width(130)))
				{
					Application.OpenURL(discordUrl);
				}
			}
			GUILayout.EndHorizontal();
		}
	}
}
