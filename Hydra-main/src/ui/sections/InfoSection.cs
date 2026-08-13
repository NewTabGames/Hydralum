using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class InfoSection : ISection
	{
		public InfoSection() : base("Info") { }

		private const string MalumUrl = "https://github.com/scp222thj/MalumMenu";
		private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";
		private const string HydralumUrl = "https://github.com/NewTabGames/Hydralum";

		public override void Render()
		{
			GUILayout.Label("Hydralum Mod System");
			GUILayout.Label("Pairing customized versions of MalumMenu & HydraMenu.");

			GUILayout.Space(10);
			GUILayout.Label("Credits & Links:");

			DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumUrl);
			GUILayout.Space(5);
			DrawCredit("Hydra", "by MrDiamond64", HydraUrl);
			GUILayout.Space(5);
			DrawCredit("Hydralum", "by NewTabGames", HydralumUrl);

			GUILayout.Space(10);
			GUILayout.Label("Licensed under GNU General Public License v3.0.");
		}

		private static void DrawCredit(string title, string author, string url)
		{
			GUILayout.Label($"<b>{title}</b> ({author})");
			if(GUILayout.Button($"Open {title} GitHub"))
			{
				Application.OpenURL(url);
			}
		}
	}
}
