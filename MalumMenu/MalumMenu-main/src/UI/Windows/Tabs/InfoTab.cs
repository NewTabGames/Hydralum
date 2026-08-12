using UnityEngine;

namespace MalumMenu;

public class InfoTab : ITab
{
    public string name => "Info";

    private const string MalumMenuUrl = "https://github.com/scp222thj/MalumMenu";
    private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";

    public void Draw()
    {
        GUILayout.Label($"MalumMenu+ v{MalumMenu.malumVersion}", GUIStylePreset.TabSubtitle);
        GUILayout.Label("A fork of MalumMenu, with features drawn from Hydra.");

        GUILayout.Space(12);
        GUILayout.Label("Credits", GUIStylePreset.TabSubtitle);

        DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumMenuUrl);
        DrawCredit("Hydra", "by MrDiamond64", HydraUrl);

        GUILayout.Space(12);
        GUILayout.Label("MalumMenu and Hydra are both licensed under GPL-3.0.", GUIStylePreset.Hint);
    }

    private static void DrawCredit(string title, string author, string url)
    {
        GUILayout.Space(6);
        GUILayout.Label($"<b>{title}</b> {author}");
        GUILayout.Label($"<color=#9A9A9A>{url}</color>");

        if (GUILayout.Button("Open GitHub", GUIStylePreset.NormalButton, GUILayout.Width(160)))
        {
            Application.OpenURL(url);
        }
    }
}
