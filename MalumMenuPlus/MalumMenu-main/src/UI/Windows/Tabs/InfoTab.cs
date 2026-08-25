using UnityEngine;

namespace MalumMenu;

public class InfoTab : ITab
{
    public string name => "Info";

    private const string HydralumUrl = "https://github.com/NewTabGames/Hydralum";
    private const string HydralumDiscordUrl = "https://discord.gg/GBg7hp7qAX";
    private const string MalumMenuUrl = "https://github.com/scp222thj/MalumMenu";
    private const string MalumDiscordUrl = "https://discord.gg/MMg8W7T3Cy";
    private const string HydraUrl = "https://github.com/MrDiamond64/Hydra";
    private const string HydraDiscordUrl = "https://discord.gg/Yd4WVvxsm6";

    public void Draw()
    {
        GUILayout.Label($"<b>Hydralum</b> v{PresenceTracker.CurrentHydralumVersion} (Malum Menu v{MalumMenu.malumVersion} | Hydra Menu v1.9.0)", GUIStylePreset.TabSubtitle);
        GUILayout.Label("A fork of MalumMenu, with features drawn from Hydra.");
        GUILayout.Space(6);
        int online = PresenceTracker.GetOnlineCount();
        GUILayout.Label($"<b>Live Users Online:</b> <color=#00FF88>{online} {(online == 1 ? "player" : "players")}</color>", GUIStylePreset.Hint);

        GUILayout.Space(12);
        GUILayout.Label("Credits & Community", GUIStylePreset.TabSubtitle);

        DrawCredit("Hydralum", "Official Discord & GitHub", HydralumUrl, HydralumDiscordUrl);
        DrawCredit("MalumMenu", "by scp222thj & astra1dev", MalumMenuUrl, MalumDiscordUrl);
        DrawCredit("Hydra", "by MrDiamond64", HydraUrl, HydraDiscordUrl);

        GUILayout.Space(12);
        GUILayout.Label("MalumMenu and Hydra are both licensed under GPL-3.0.", GUIStylePreset.Hint);
        GUILayout.Space(8);
        GUILayout.Label("<b>Note:</b> These menus were combined using AI. If you don't like it, don't use it.", GUIStylePreset.Hint);
    }

    private static void DrawCredit(string title, string author, string githubUrl, string discordUrl = null)
    {
        GUILayout.Space(6);
        GUILayout.Label($"<b>{title}</b> {author}");
        GUILayout.Label($"<color=#9A9A9A>{githubUrl}</color>");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open GitHub", GUIStylePreset.NormalButton, GUILayout.Width(130)))
        {
            Application.OpenURL(githubUrl);
        }

        if (!string.IsNullOrEmpty(discordUrl))
        {
            if (GUILayout.Button("Open Discord", GUIStylePreset.NormalButton, GUILayout.Width(130)))
            {
                Application.OpenURL(discordUrl);
            }
        }
        GUILayout.EndHorizontal();
    }
}
