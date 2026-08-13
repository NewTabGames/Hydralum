using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class InfoSection : ISection
	{
		public InfoSection() : base("Info") { }

		private const string MalumUrl = "https://github.com/scp222thj/MalumMenu";
		private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";

		public override void Render()
		{
			GUILayout.Label("HydraMenu");
			GUILayout.Label("A fork of Hydra, with features drawn from MalumMenu.");

			GUILayout.Space(12);
			GUILayout.Label("Credits");

			DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumUrl);
			DrawCredit("Hydra", "by MrDiamond64", HydraUrl);

			GUILayout.Space(12);
			GUILayout.Label("MalumMenu and Hydra are both licensed under GPL-3.0.");
		}

		private static void DrawCredit(string title, string author, string url)
		{
			GUILayout.Space(6);
			GUILayout.Label($"<b>{title}</b> {author}");
			GUILayout.Label($"<color=#9A9A9A>{url}</color>");

			if (GUILayout.Button("Open GitHub", GUILayout.Width(160)))
			{
				Application.OpenURL(url);
			}
		}
	}
}
