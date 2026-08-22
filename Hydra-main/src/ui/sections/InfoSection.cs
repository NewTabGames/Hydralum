using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class InfoSection : ISection
	{
		public InfoSection() : base("Info") { }

		private const string MalumUrl = "https://github.com/scp222thj/MalumMenu";
		private const string MalumDiscordUrl = "https://discord.gg/MMg8W7T3Cy";
		private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";
		private const string HydraDiscordUrl = "https://discord.gg/Yd4WVvxsm6";

		public override void Render()
		{
			GUILayout.Label("HydraMenu");
			GUILayout.Label("A fork of Hydra, with features drawn from MalumMenu.");

			GUILayout.Space(12);
			GUILayout.Label("Credits & Community");

			DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumUrl, MalumDiscordUrl);
			DrawCredit("Hydra", "by MrDiamond64", HydraUrl, HydraDiscordUrl);

			GUILayout.Space(12);
			GUILayout.Label("MalumMenu and Hydra are both licensed under GPL-3.0.");
			GUILayout.Space(8);
			GUILayout.Label("<b>Note:</b> These menus were combined using AI. If you don't like it, don't use it.");
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
