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

    // Custom-color type-in state. GUILayout.TextField is unstripped and crashes under IL2CPP, so
    // (as in ConfigTab) we capture typed characters ourselves. Only one field is active at a time.
    private enum HexField { None, Solid, GradA, GradB, Name }
    private static HexField _typing = HexField.None;
    private static string _solidInput = "";
    private static string _gradAInput = "";
    private static string _gradBInput = "";
    private static string _nameInput = "";
    private static string _status = "";
    private static float _statusUntil = 0f;

    // Saved custom themes come from the shared CThemes folder (see CustomThemeStore). Cache the disk
    // listing and refresh at most once a second so drawing this tab every frame doesn't hit disk each time.
    private static System.Collections.Generic.List<(string name, string value)> _savedCache = new();
    private static float _savedCacheUntil = 0f;

    private static void RefreshSaved(bool force = false)
    {
        if (!force && Time.unscaledTime < _savedCacheUntil) return;
        _savedCache = CustomThemeStore.LoadAll();
        _savedCacheUntil = Time.unscaledTime + 1f;
    }

    private static float ContentWidth() => CellWidth() * Columns + Gap * (Columns - 1);

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
        GUILayout.Label("Custom Color", GUIStylePreset.TabSubtitle);
        DrawCustom();

        GUILayout.Space(10);
        GUILayout.Label("Solid Themes", GUIStylePreset.TabSubtitle);
        DrawGrid(Themes.Length, ThemeButton);

        GUILayout.Space(10);
        GUILayout.Label($"Gradients ({Gradients.Length})", GUIStylePreset.TabSubtitle);
        DrawGrid(Gradients.Length, GradientButton);
    }

    // Custom hex + custom gradient entry. The applied color flows through the same menuHtmlColor config
    // entry as the presets (so it persists), and named themes go to the shared CThemes folder so they
    // also appear in Hydra. Widths are pinned to the preset grid's ContentWidth so nothing clips under
    // the scrollbar.
    private void DrawCustom()
    {
        if (_typing != HexField.None) CaptureActiveField();

        float total = ContentWidth();
        const float applyW = 64f;

        // --- Solid custom color: [ #hex field ] [Apply] — the Apply button previews the chosen color ---
        GUILayout.BeginHorizontal(GUILayout.Width(total));
        string solidLabel = _typing == HexField.Solid
            ? $"<color=yellow>{(string.IsNullOrEmpty(_solidInput) ? "#RRGGBB" : _solidInput)}_</color>"
            : (string.IsNullOrEmpty(_solidInput) ? "Hex: <i>click to type</i>" : $"Hex: <b>{_solidInput}</b>");
        if (GUILayout.Button(solidLabel, GUIStylePreset.NormalButton, GUILayout.Width(total - applyW - Gap), GUILayout.Height(24)))
            SetTyping(HexField.Solid);
        GUILayout.Space(Gap);
        bool solidOk = NormalizeHex(_solidInput, out var solidColor, out var solidNorm);
        var prevBg = GUI.backgroundColor;
        if (solidOk) GUI.backgroundColor = solidColor;
        if (GUILayout.Button("Apply", GUIStylePreset.NormalButton, GUILayout.Width(applyW), GUILayout.Height(24)))
        {
            if (solidOk) { ApplyTheme(solidNorm); SetStatus("Applied custom color"); }
            else SetStatus("<color=#FF6B6B>Invalid hex</color>");
        }
        GUI.backgroundColor = prevBg;
        GUILayout.EndHorizontal();

        // --- Custom gradient: [ Start ] [ End ] then [swatch preview] [Apply Gradient] ---
        GUILayout.Space(6);
        GUILayout.Label("Custom Gradient", GUIStylePreset.TabSubtitle);

        float halfW = (total - Gap) / 2f;
        GUILayout.BeginHorizontal(GUILayout.Width(total));
        string aLabel = _typing == HexField.GradA
            ? $"<color=yellow>{(string.IsNullOrEmpty(_gradAInput) ? "#Start" : _gradAInput)}_</color>"
            : (string.IsNullOrEmpty(_gradAInput) ? "Start: <i>click</i>" : $"Start: <b>{_gradAInput}</b>");
        if (GUILayout.Button(aLabel, GUIStylePreset.NormalButton, GUILayout.Width(halfW), GUILayout.Height(24)))
            SetTyping(HexField.GradA);
        GUILayout.Space(Gap);
        string bLabel = _typing == HexField.GradB
            ? $"<color=yellow>{(string.IsNullOrEmpty(_gradBInput) ? "#End" : _gradBInput)}_</color>"
            : (string.IsNullOrEmpty(_gradBInput) ? "End: <i>click</i>" : $"End: <b>{_gradBInput}</b>");
        if (GUILayout.Button(bLabel, GUIStylePreset.NormalButton, GUILayout.Width(halfW), GUILayout.Height(24)))
            SetTyping(HexField.GradB);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.Width(total));
        bool aOk = NormalizeHex(_gradAInput, out var ga, out var gaNorm);
        bool bOk = NormalizeHex(_gradBInput, out var gb, out var gbNorm);
        var prevBgG = GUI.backgroundColor;
        if (aOk && bOk)
        {
            float wave = (Mathf.Sin(Time.time * 2.2f) + 1f) * 0.5f;
            GUI.backgroundColor = Color.Lerp(ga, gb, wave); // Apply Gradient button animates the gradient
        }
        if (GUILayout.Button("Apply Gradient", GUIStylePreset.NormalButton, GUILayout.Width(total), GUILayout.Height(24)))
        {
            if (aOk && bOk) { ApplyGradient(gaNorm, gbNorm); SetStatus("Applied custom gradient"); }
            else SetStatus("<color=#FF6B6B>Enter two valid hex colors</color>");
        }
        GUI.backgroundColor = prevBgG;
        GUILayout.EndHorizontal();

        // --- Save current theme to the shared CThemes folder: [ Name field ] [Save] ---
        GUILayout.Space(8);
        GUILayout.Label("Saved Themes <size=10><color=#888888>(shared with Hydra)</color></size>", GUIStylePreset.TabSubtitle);

        GUILayout.BeginHorizontal(GUILayout.Width(total));
        string nameLabel = _typing == HexField.Name
            ? $"<color=yellow>{(string.IsNullOrEmpty(_nameInput) ? "Name..." : _nameInput)}_</color>"
            : (string.IsNullOrEmpty(_nameInput) ? "Name: <i>click to type</i>" : $"Name: <b>{_nameInput}</b>");
        if (GUILayout.Button(nameLabel, GUIStylePreset.NormalButton, GUILayout.Width(total - applyW - Gap), GUILayout.Height(24)))
            SetTyping(HexField.Name);
        GUILayout.Space(Gap);
        if (GUILayout.Button("Save", GUIStylePreset.NormalButton, GUILayout.Width(applyW), GUILayout.Height(24)))
            SaveCurrentTheme();
        GUILayout.EndHorizontal();

        DrawSavedList(total);

        if (!string.IsNullOrEmpty(_status) && Time.unscaledTime < _statusUntil)
            GUILayout.Label($"<size=11>{_status}</size>");
    }

    // The current accent, as stored in menuHtmlColor: "#RRGGBB", "grad:#a,#b", or "" for default.
    private static string CurrentThemeValue() => MalumMenu.menuHtmlColor?.Value ?? "";

    private void SaveCurrentTheme()
    {
        if (CheatToggles.rgbMode) { SetStatus("<color=#FF6B6B>Turn off RGB Mode first</color>"); return; }
        string value = CurrentThemeValue();
        if (string.IsNullOrEmpty(value)) { SetStatus("<color=#FF6B6B>Apply a color first</color>"); return; }
        string name = CustomThemeStore.Sanitize(_nameInput);
        if (name == null) { SetStatus("<color=#FF6B6B>Enter a name</color>"); return; }

        if (CustomThemeStore.Save(name, value))
        {
            SetStatus($"Saved \"{name}\"");
            _nameInput = "";
            _typing = HexField.None;
            RefreshSaved(true);
        }
        else SetStatus("<color=#FF6B6B>Save failed</color>");
    }

    // Two-column list of saved themes: click the name to apply, ✕ to delete. Colors preview the value.
    private void DrawSavedList(float total)
    {
        RefreshSaved();
        if (_savedCache.Count == 0)
        {
            GUILayout.Label("<size=11><color=#888888>None yet — apply a color, name it, then Save.</color></size>");
            return;
        }

        const float delW = 22f;
        float groupW = (total - Gap) / 2f;
        float nameW = groupW - delW - Gap;

        for (int i = 0; i < _savedCache.Count; i += 2)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(total));
            DrawSavedEntry(_savedCache[i], nameW, delW);
            if (i + 1 < _savedCache.Count)
            {
                GUILayout.Space(Gap);
                DrawSavedEntry(_savedCache[i + 1], nameW, delW);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }

    private void DrawSavedEntry((string name, string value) theme, float nameW, float delW)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = PreviewColor(theme.value);
        if (GUILayout.Button(theme.name, GUIStylePreset.NormalButton, GUILayout.Width(nameW), GUILayout.Height(RowHeight)))
        {
            ApplySavedValue(theme.value);
            SetStatus($"Applied \"{theme.name}\"");
        }
        GUI.backgroundColor = prev;

        GUILayout.Space(Gap);
        if (GUILayout.Button("✕", GUIStylePreset.NormalButton, GUILayout.Width(delW), GUILayout.Height(RowHeight)))
        {
            CustomThemeStore.Delete(theme.name);
            SetStatus($"Deleted \"{theme.name}\"");
            RefreshSaved(true);
        }
    }

    // Applies a stored value (hex or "grad:#a,#b") via the same paths the preset buttons use.
    private static void ApplySavedValue(string value)
    {
        if (!string.IsNullOrEmpty(value) && value.StartsWith("grad:"))
        {
            var parts = value.Substring(5).Split(',');
            if (parts.Length == 2) { ApplyGradient(parts[0], parts[1]); return; }
        }
        ApplyTheme(value);
    }

    // Static preview color for a stored value (animated for gradients).
    private static Color PreviewColor(string value)
    {
        if (string.IsNullOrEmpty(value)) return Color.white;
        if (value.StartsWith("grad:"))
        {
            var parts = value.Substring(5).Split(',');
            if (parts.Length == 2
                && ColorUtility.TryParseHtmlString(parts[0], out var a)
                && ColorUtility.TryParseHtmlString(parts[1], out var b))
            {
                float wave = (Mathf.Sin(Time.time * 2.2f) + 1f) * 0.5f;
                return Color.Lerp(a, b, wave);
            }
            return Color.white;
        }
        return ColorUtility.TryParseHtmlString(value, out var c) ? c : Color.white;
    }

    private static void SetTyping(HexField field)
    {
        _typing = _typing == field ? HexField.None : field;
    }

    private static void SetStatus(string msg)
    {
        _status = msg;
        _statusUntil = Time.unscaledTime + 4f;
    }

    // Adds '#' if missing and validates. Overloads: +Color, +normalized string.
    private static bool NormalizeHex(string raw, out Color color) => NormalizeHex(raw, out color, out _);
    private static bool NormalizeHex(string raw, out Color color, out string normalized)
    {
        color = Color.white;
        normalized = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        string h = raw.Trim();
        if (!h.StartsWith("#")) h = "#" + h;
        if (ColorUtility.TryParseHtmlString(h, out color)) { normalized = h; return true; }
        return false;
    }

    // Manual key capture (IL2CPP-safe). Hex fields accept 0-9 a-f A-F # (cap 9). The name field accepts
    // any printable char (cap 24).
    private void CaptureActiveField()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
        {
            _typing = HexField.None;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            ref string buf = ref ActiveBuffer();
            if (!string.IsNullOrEmpty(buf)) buf = buf.Substring(0, buf.Length - 1);
            e.Use();
            return;
        }

        char c = e.character;
        if (_typing == HexField.Name)
        {
            if (c != '\0' && !char.IsControl(c))
            {
                ref string buf = ref ActiveBuffer();
                if ((buf?.Length ?? 0) < 24) { buf += c; e.Use(); }
            }
            return;
        }

        bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == '#';
        if (isHex)
        {
            ref string buf = ref ActiveBuffer();
            if ((buf?.Length ?? 0) < 9) { buf += c; e.Use(); }
        }
    }

    private ref string ActiveBuffer()
    {
        switch (_typing)
        {
            case HexField.GradA: return ref _gradAInput;
            case HexField.GradB: return ref _gradBInput;
            case HexField.Name: return ref _nameInput;
            default: return ref _solidInput;
        }
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
