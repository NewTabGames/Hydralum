using System;
using UnityEngine;

namespace MalumMenu;

public class ConfigTab : ITab
{
    public string name => "Config";

    private const float ButtonWidth = 105f;
    private const float ButtonGap = 6f;
    private Vector2 _themesScroll = Vector2.zero;
    private static bool _isListeningForKey = false;

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
        ("Cyberpunk", "#FF007F", "#00F0FF"),
        ("Vaporwave", "#FF71CE", "#01CDFE"),
        ("Solar Flare", "#FF0844", "#FFB199"),
        ("Matrix", "#00FF87", "#60EFFF"),
        ("Midnight", "#0F2027", "#2C5364"),
        ("Amethyst", "#8E2DE2", "#4A00E0"),
        ("Blood Orange", "#F12711", "#F5AF19"),
        ("Neon Lime", "#F9D423", "#A8FF78"),
        ("Lavender", "#A18CD1", "#FBC2EB"),
        ("Iceberg", "#56CCF2", "#2F80ED"),
        ("Sakura", "#EE9CA7", "#FFDDE1"),
        ("Synthwave", "#833AB4", "#FD1D1D"),
        ("Cosmic", "#3A1C71", "#D76D77"),
        ("Emerald Forest", "#0BA360", "#3CBA92"),
        ("Electric Rose", "#F857A6", "#FF5858"),
        ("Gold Mirage", "#FFE259", "#FFA751"),
        ("Abyss", "#000428", "#004E92"),
        ("Tropical", "#00F260", "#0575E6"),
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

        GUILayout.Space(10);

        GUILayout.BeginVertical();
        _themesScroll = GUILayout.BeginScrollView(_themesScroll);
        DrawThemes();
        GUILayout.EndScrollView();
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

        // Keybind selector without stripped GUI.DoTextField
        GUILayout.Label("Menu Keybind:");

        string currentKey = string.IsNullOrEmpty(MalumMenu.menuKeybind.Value) ? "Delete" : MalumMenu.menuKeybind.Value;
        string btnText = _isListeningForKey ? "<color=yellow>Press any key...</color>" : $"Key: <b>{currentKey}</b>";

        if (GUILayout.Button(btnText, GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            _isListeningForKey = !_isListeningForKey;
        }

        if (_isListeningForKey)
        {
            if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                MalumMenu.menuKeybind.Value = Event.current.keyCode.ToString();
                _isListeningForKey = false;
            }
        }

        GUILayout.Space(4);
        GUILayout.Label("Scale:");

        MenuUI.uiScale = GUILayout.HorizontalSlider(MenuUI.uiScale, 0.5f, 2f);

        GUILayout.Label("Opacity:");

        MenuUI.uiOpacity = GUILayout.HorizontalSlider(MenuUI.uiOpacity, 0.1f, 1f);

        MalumMenu.menuOpenOnMouse.Value =
            GUILayout.Toggle(MalumMenu.menuOpenOnMouse.Value, " Open on Cursor");

        MalumMenu.menuKeepSubwindowsOpen.Value =
            GUILayout.Toggle(MalumMenu.menuKeepSubwindowsOpen.Value, " Keep Subwindows Open");

        MalumMenu.showVersionWarning.Value =
            GUILayout.Toggle(MalumMenu.showVersionWarning.Value, " Version Warning Popup");
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
        CenteredLabel($"Gradients ({Gradients.Length})");

        // Live traveling wave preview across the grid
        for (var i = 0; i < Gradients.Length; i += 2)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            float tLeft = (Mathf.Sin(Time.time * 2.2f + (i * 0.4f)) + 1f) * 0.5f;
            GradientButton(Gradients[i], tLeft);
            if (i + 1 < Gradients.Length)
            {
                GUILayout.Space(ButtonGap);
                float tRight = (Mathf.Sin(Time.time * 2.2f + ((i + 1) * 0.4f)) + 1f) * 0.5f;
                GradientButton(Gradients[i + 1], tRight);
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
