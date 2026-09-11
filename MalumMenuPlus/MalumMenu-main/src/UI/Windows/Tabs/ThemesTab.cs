using System;
using UnityEngine;

namespace MalumMenu;

public class ThemesTab : ITab
{
    public string name => "Themes";

    // 4 uniform columns so everything fits without the content scroll bar (which, right at the
    // overflow threshold, was flickering on/off and reflowing the buttons — the "wobble").
    private const int Columns = 4;
    private const float RowHeight = 28f;
    private const float Gap = 6f;

    // Fixed, identical width for every button (ExpandWidth made longer-named buttons wider). Derived
    // from the known window width — the content column is ~0.74 of it after the tab list/separator —
    // rather than a runtime measurement, which fed back inside the scroll view and blew the layout up.
    private static float CellWidth()
    {
        float contentW = MenuUI.windowWidth * 0.74f;
        return Mathf.Max(40f, (contentW - (Columns - 1) * Gap) / Columns);
    }

    // Preset accent colors. Empty hex = restore default.
    private static readonly (string name, string hex)[] Themes =
    {
        ("Malum", ""),
        ("Violet", "#8A2BE2"),
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

        GUILayout.Space(10);
        GUILayout.Label("Solid Themes", GUIStylePreset.TabSubtitle);
        DrawGrid(Themes.Length, ThemeButton);

        GUILayout.Space(10);
        GUILayout.Label($"Gradients ({Gradients.Length})", GUIStylePreset.TabSubtitle);
        DrawGrid(Gradients.Length, GradientButton);
    }

    // Lays out `count` cells in fixed columns. Incomplete final rows are padded so every button keeps
    // the same width (no wobble between rows).
    private static void DrawGrid(int count, Action<int> drawCell)
    {
        for (var i = 0; i < count; i += Columns)
        {
            GUILayout.BeginHorizontal();
            for (var c = 0; c < Columns; c++)
            {
                if (c > 0) GUILayout.Space(Gap);

                var idx = i + c;
                if (idx < count)
                    drawCell(idx);
                else
                    GUILayout.Label("", GUILayout.Width(CellWidth()), GUILayout.Height(RowHeight));
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }

    private static void DrawRgbButton()
    {
        var previous = GUI.backgroundColor;
        GUI.backgroundColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f, 1f), 1f, 1f); // live rainbow preview

        if (GUILayout.Button("RGB Mode (Animated Rainbow)", GUIStylePreset.NormalButton, GUILayout.Height(30)))
            CheatToggles.rgbMode = true;

        GUI.backgroundColor = previous;
    }

    private static void ThemeButton(int i)
    {
        var theme = Themes[i];
        var previous = GUI.backgroundColor;
        if (!string.IsNullOrEmpty(theme.hex) && ColorUtility.TryParseHtmlString(theme.hex, out var swatch))
            GUI.backgroundColor = swatch;
        else if (string.IsNullOrEmpty(theme.hex))
            GUI.backgroundColor = Color.white;

        if (GUILayout.Button(theme.name, GUIStylePreset.NormalButton, GUILayout.Width(CellWidth()), GUILayout.Height(RowHeight)))
            ApplyTheme(theme.hex);

        GUI.backgroundColor = previous;
    }

    private static void GradientButton(int i)
    {
        var grad = Gradients[i];
        var t = (Mathf.Sin(Time.time * 2.2f + (i * 0.4f)) + 1f) * 0.5f;

        var previous = GUI.backgroundColor;
        if (ColorUtility.TryParseHtmlString(grad.a, out var ca) && ColorUtility.TryParseHtmlString(grad.b, out var cb))
            GUI.backgroundColor = Color.Lerp(ca, cb, t);

        if (GUILayout.Button(grad.name, GUIStylePreset.NormalButton, GUILayout.Width(CellWidth()), GUILayout.Height(RowHeight)))
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
