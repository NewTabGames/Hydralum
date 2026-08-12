using UnityEngine;

namespace MalumMenu;

public class ConfigTab : ITab
{
    public string name => "Config";

    private const float ButtonWidth = 105f;
    private const float ButtonGap = 6f;

    // Preset accent colors. Empty hex = restore the default (unset) color. Applied through the same
    // menuHtmlColor config that UIHelpers.ApplyUIColor reads, so a picked theme persists across
    // restarts. RGB Mode overrides a theme while it's on.
    private static readonly (string name, string hex)[] Themes =
    {
        ("Default", ""),
        ("Malum", "#8A2BE2"),
        ("Ocean", "#1E90FF"),
        ("Emerald", "#2ECC71"),
        ("Crimson", "#E74C3C"),
        ("Sunset", "#FF8C42"),
        ("Gold", "#FFC107"),
        ("Bubblegum", "#FF6FB5"),
    };

    private static readonly (string name, string a, string b)[] Gradients =
    {
        ("Fire", "#FF4E00", "#FFC400"),
        ("Aurora", "#00C9FF", "#92FE9D"),
        ("Galaxy", "#7F00FF", "#E100FF"),
        ("Ocean", "#2E3192", "#1BFFFF"),
        ("Sunset", "#FF5F6D", "#FFC371"),
        ("Mint", "#11998E", "#38EF7D"),
    };

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(270f));
        DrawProfile();
        GUILayout.Space(12);
        DrawMenu();
        GUILayout.Space(12);
        DrawAccount();
        GUILayout.Space(12);
        DrawModes();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        DrawThemes();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawProfile()
    {
        GUILayout.Label("Profile", GUIStylePreset.TabSubtitle);

        CheatToggles.openConfig = GUILayout.Toggle(CheatToggles.openConfig, " Open Config");

        CheatToggles.reloadConfig = GUILayout.Toggle(CheatToggles.reloadConfig, " Reload Config");

        CheatToggles.saveProfile = GUILayout.Toggle(CheatToggles.saveProfile, " Save to Profile");

        CheatToggles.loadProfile = GUILayout.Toggle(CheatToggles.loadProfile, " Load from Profile");

        // Auto-load the saved profile at game startup. Backed by the AutoLoadProfile config entry,
        // which BepInEx auto-saves on change (and only when the value actually flips, so this is
        // safe to assign every frame). Covers toggles, keybinds, scale, opacity and the menu theme.
        MalumMenu.autoLoadProfile.Value =
            GUILayout.Toggle(MalumMenu.autoLoadProfile.Value, " Auto Load on Startup");
    }

    private void DrawMenu()
    {
        GUILayout.Label("Menu", GUIStylePreset.TabSubtitle);

        GUILayout.Label($"Scale: {MenuUI.uiScale:F2}x");
        MenuUI.uiScale = GUILayout.HorizontalSlider(MenuUI.uiScale, 0.8f, 1.5f, GUILayout.Width(250f));

        GUILayout.Label($"Opacity: {Mathf.RoundToInt(MenuUI.uiOpacity * 100f)}%");
        MenuUI.uiOpacity = GUILayout.HorizontalSlider(MenuUI.uiOpacity, 0.3f, 1f, GUILayout.Width(250f));
    }

    private void DrawAccount()
    {
        GUILayout.Label("Account", GUIStylePreset.TabSubtitle);

        CheatToggles.freeCosmetics = GUILayout.Toggle(CheatToggles.freeCosmetics, " Free Cosmetics");

        CheatToggles.avoidPenalties = GUILayout.Toggle(CheatToggles.avoidPenalties, " Avoid Penalties");

        CheatToggles.unlockFeatures = GUILayout.Toggle(CheatToggles.unlockFeatures, " Unlock Extra Features");

        CheatToggles.copyLobbyCodeOnDisconnect = GUILayout.Toggle(CheatToggles.copyLobbyCodeOnDisconnect, " Copy Lobby Code on Disconnect");

        CheatToggles.spoofAprilFoolsDate = GUILayout.Toggle(CheatToggles.spoofAprilFoolsDate, " Spoof Date to April 1st");

        CheatToggles.unlockFps = GUILayout.Toggle(CheatToggles.unlockFps, " Unlock FPS");

        FpsUnlocker.TargetFps = Mathf.RoundToInt(
            GUILayout.HorizontalSlider(FpsUnlocker.TargetFps, FpsUnlocker.MinFps, FpsUnlocker.MaxFps, GUILayout.Width(250f)));

        GUILayout.Label($"FPS Limit: {FpsUnlocker.TargetFps}");
    }

    private void DrawModes()
    {
        GUILayout.Label("Modes", GUIStylePreset.TabSubtitle);

        if (GUILayout.Button("Eject", GUIStylePreset.NormalButton, GUILayout.Width(200)))
        {
            Utils.Eject();
        }
    }

    private void DrawThemes()
    {
        CenteredLabel("Themes");

        // RGB is a theme too - a wide button that previews itself by cycling the hue. Selecting it
        // turns RGB on; picking any solid/gradient theme turns it back off.
        DrawRgbButton();

        for (var i = 0; i < Themes.Length; i += 2)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            ThemeButton(Themes[i]);
            if (i + 1 < Themes.Length)
            {
                GUILayout.Space(ButtonGap);
                ThemeButton(Themes[i + 1]);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        CenteredLabel("Gradients");

        // Same pulse the menu uses, so each button previews its gradient in motion
        var t = Mathf.PingPong(Time.time * 0.5f, 1f);

        for (var i = 0; i < Gradients.Length; i += 2)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GradientButton(Gradients[i], t);
            if (i + 1 < Gradients.Length)
            {
                GUILayout.Space(ButtonGap);
                GradientButton(Gradients[i + 1], t);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    private static void DrawRgbButton()
    {
        var previous = GUI.backgroundColor;
        GUI.backgroundColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f, 1f), 1f, 1f); // live rainbow preview

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("RGB", GUIStylePreset.NormalButton, GUILayout.Width(ButtonWidth * 2f + ButtonGap), GUILayout.Height(28)))
            CheatToggles.rgbMode = true;

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUI.backgroundColor = previous;
    }

    private static void CenteredLabel(string text)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(text, GUIStylePreset.TabSubtitle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static void ThemeButton((string name, string hex) theme)
    {
        var previous = GUI.backgroundColor;
        if (!string.IsNullOrEmpty(theme.hex) && ColorUtility.TryParseHtmlString(theme.hex, out var swatch))
            GUI.backgroundColor = swatch;

        if (GUILayout.Button(theme.name, GUIStylePreset.NormalButton, GUILayout.Width(ButtonWidth), GUILayout.Height(28)))
            ApplyTheme(theme.hex);

        GUI.backgroundColor = previous;
    }

    private static void GradientButton((string name, string a, string b) grad, float t)
    {
        var previous = GUI.backgroundColor;
        if (ColorUtility.TryParseHtmlString(grad.a, out var ca) && ColorUtility.TryParseHtmlString(grad.b, out var cb))
            GUI.backgroundColor = Color.Lerp(ca, cb, t);

        if (GUILayout.Button(grad.name, GUIStylePreset.NormalButton, GUILayout.Width(ButtonWidth), GUILayout.Height(28)))
            ApplyGradient(grad.a, grad.b);

        GUI.backgroundColor = previous;
    }

    private static void ApplyTheme(string hex)
    {
        CheatToggles.rgbMode = false;          // themes and RGB are mutually exclusive
        MalumMenu.menuHtmlColor.Value = hex;   // read by UIHelpers.ApplyUIColor; persists to config
    }

    private static void ApplyGradient(string hexA, string hexB)
    {
        CheatToggles.rgbMode = false;
        MalumMenu.menuHtmlColor.Value = $"grad:{hexA},{hexB}";
    }
}
