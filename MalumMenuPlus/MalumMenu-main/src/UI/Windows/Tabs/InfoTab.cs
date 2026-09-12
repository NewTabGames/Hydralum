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

    private bool _showKeybindInfo = false;

    public void Draw()
    {
        GUILayout.Label($"<b>Hydralum</b> v{PresenceTracker.CurrentHydralumVersion} (Malum Menu v{MalumMenu.malumVersion} | Hydra Menu v2.0.0)", GUIStylePreset.TabSubtitle);
        GUILayout.Label("A fork of MalumMenu, with features drawn from Hydra.");
        GUILayout.Space(6);
        int online = PresenceTracker.GetOnlineCount();
        GUILayout.Label($"<b>Live Users Online:</b> <color=#00FF88>{online} {(online == 1 ? "player" : "players")}</color>", GUIStylePreset.Hint);

        GUILayout.Space(8);
        _showKeybindInfo = GUILayout.Toggle(_showKeybindInfo, " Show Keybind Info", GUIStylePreset.NormalToggle);

        if (_showKeybindInfo)
        {
            GUILayout.Space(4);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b>Keybind Reference & Shortcuts:</b>", GUIStylePreset.TabSubtitle);
            GUILayout.Space(4);

            DrawKeybindRow("Delete", "Toggle Active Menu (Customizable in Config Tab)");
            DrawKeybindRow("Switch (Button)", "Switch between MalumMenu and HydraMenu in-place");
            DrawKeybindRow("Escape (Important)", "Dismiss Match Info Guide, dialogs, and fix menu softlocks");
            DrawKeybindRow("Left / Right Arrow", "Cycle Previous / Next Vent (with Vent Cheats)");

            GUILayout.EndVertical();
        }

        GUILayout.Space(12);
        GUILayout.Label("Invisible Name Method", GUIStylePreset.TabSubtitle);
        GUILayout.Label("Give yourself a blank in-game name by editing your local player file.", GUIStylePreset.Hint);
        GUILayout.Space(4);

        GUILayout.Label("1. Fully close Among Us, then open your Among Us data folder:");
        if (GUILayout.Button("Open player.amogus Folder", GUIStylePreset.NormalButton, GUILayout.Width(240)))
        {
            OpenAmongUsDataFolder();
        }

        GUILayout.Space(4);
        GUILayout.Label("2. Open <b>player.amogus</b> with Notepad.");
        GUILayout.Label("3. Change your name to one of these, then Save:");

        DrawInvisibleNameRow("\\u00AD", "soft hyphen");
        DrawInvisibleNameRow("\\u2060", "word joiner");

        GUILayout.Space(2);
        GUILayout.Label("4. Save the file, then launch Among Us.", GUIStylePreset.Hint);

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

    private static void DrawKeybindRow(string key, string description)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"<color=#00FFAA><b>[{key}]</b></color>", GUILayout.Width(170));
        GUILayout.Label(description);
        GUILayout.EndHorizontal();
    }

    // "escape" is the literal escape text (e.g. \u00AD) shown in the label and copied to the clipboard.
    // Pasted into player.amogus, Among Us parses it into the invisible character.
    private static void DrawInvisibleNameRow(string escape, string label)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"<color=#00FFAA><b>{escape}</b></color>  <color=#9A9A9A>({label})</color>", GUILayout.Width(220));
        if (GUILayout.Button("Copy", GUIStylePreset.NormalButton, GUILayout.Width(90)))
        {
            GUIUtility.systemCopyBuffer = escape;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static void OpenAmongUsDataFolder()
    {
        try
        {
            // persistentDataPath resolves to C:\Users\<user>\AppData\LocalLow\Innersloth\Among Us for the
            // actual logged-in user, so this works for everyone without hardcoding a username.
            string path = Application.persistentDataPath.Replace('/', '\\');
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
        }
        catch { }
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
