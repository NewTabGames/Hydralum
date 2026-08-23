using System;
using UnityEngine;

namespace MalumMenu;

public class ThemesTab : ITab
{
    public string name => "Themes";

    private const float ButtonWidth = 150f;
    private const float ButtonGap = 8f;

    // Preset accent colors. Empty hex = restore default.
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
        GUILayout.Label("RGB Mode", GUIStylePreset.TabSubtitle);
        DrawRgbButton();

        GUILayout.Space(12);
        GUILayout.Label("Solid Themes", GUIStylePreset.TabSubtitle);
        DrawSolidThemes();

        GUILayout.Space(14);
        GUILayout.Label($"Gradients ({Gradients.Length})", GUIStylePreset.TabSubtitle);
        DrawGradients();
    }

    private static void DrawRgbButton()
    {
        var previous = GUI.backgroundColor;
        GUI.backgroundColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f, 1f), 1f, 1f); // live rainbow preview

        if (GUILayout.Button("RGB Mode (Animated Rainbow)", GUIStylePreset.NormalButton, GUILayout.Height(32)))
            CheatToggles.rgbMode = true;

        GUI.backgroundColor = previous;
    }

    private static void DrawSolidThemes()
    {
        for (var i = 0; i < Themes.Length; i += 3)
        {
            GUILayout.BeginHorizontal();
            ThemeButton(Themes[i]);
            if (i + 1 < Themes.Length)
            {
                GUILayout.Space(ButtonGap);
                ThemeButton(Themes[i + 1]);
            }
            if (i + 2 < Themes.Length)
            {
                GUILayout.Space(ButtonGap);
                ThemeButton(Themes[i + 2]);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }

    private static void DrawGradients()
    {
        for (var i = 0; i < Gradients.Length; i += 3)
        {
            GUILayout.BeginHorizontal();
            float t1 = (Mathf.Sin(Time.time * 2.2f + (i * 0.4f)) + 1f) * 0.5f;
            GradientButton(Gradients[i], t1);

            if (i + 1 < Gradients.Length)
            {
                GUILayout.Space(ButtonGap);
                float t2 = (Mathf.Sin(Time.time * 2.2f + ((i + 1) * 0.4f)) + 1f) * 0.5f;
                GradientButton(Gradients[i + 1], t2);
            }

            if (i + 2 < Gradients.Length)
            {
                GUILayout.Space(ButtonGap);
                float t3 = (Mathf.Sin(Time.time * 2.2f + ((i + 2) * 0.4f)) + 1f) * 0.5f;
                GradientButton(Gradients[i + 2], t3);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }

    private static void ThemeButton((string name, string hex) theme)
    {
        var previous = GUI.backgroundColor;
        if (!string.IsNullOrEmpty(theme.hex) && ColorUtility.TryParseHtmlString(theme.hex, out var swatch))
            GUI.backgroundColor = swatch;

        if (GUILayout.Button(theme.name, GUIStylePreset.NormalButton, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            ApplyTheme(theme.hex);

        GUI.backgroundColor = previous;
    }

    private static void GradientButton((string name, string a, string b) grad, float t)
    {
        var previous = GUI.backgroundColor;
        if (ColorUtility.TryParseHtmlString(grad.a, out var ca) && ColorUtility.TryParseHtmlString(grad.b, out var cb))
            GUI.backgroundColor = Color.Lerp(ca, cb, t);

        if (GUILayout.Button(grad.name, GUIStylePreset.NormalButton, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            ApplyGradient(grad.a, grad.b);

        GUI.backgroundColor = previous;
    }

    private static void ApplyTheme(string hex)
    {
        CheatToggles.rgbMode = false;
        MalumMenu.menuHtmlColor.Value = hex;
    }

    private static void ApplyGradient(string hexA, string hexB)
    {
        CheatToggles.rgbMode = false;
        MalumMenu.menuHtmlColor.Value = $"grad:{hexA},{hexB}";
    }
}
