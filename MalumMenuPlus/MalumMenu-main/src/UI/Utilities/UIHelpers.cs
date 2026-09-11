using UnityEngine;

namespace MalumMenu;

public static class UIHelpers
{
    public static void ApplyUIColor(float offset = 0f)
    {
        GUI.backgroundColor = GetGradientColor(offset);
    }

    // Pop-up windows are drawn with GUIUtility.ScaleAroundPivot using their top-left as the pivot, so
    // on screen they cover width*scale by height*scale from that corner. Mouse-over / click-through
    // checks must test against this scaled area, not the raw window rect.
    public static Rect ScaledWindowRect(Rect r, float scale)
    {
        return new Rect(r.x, r.y, r.width * scale, r.height * scale);
    }

    public static Color GetGradientColor(float spatialOffset = 0f, float speed = 2.2f, float frequency = 0.02f)
    {
        // RGB mode cycles the full hue spectrum in a continuous wave
        if (CheatToggles.rgbMode)
        {
            float hue = Mathf.Repeat((Time.time * 0.35f) + (spatialOffset * 0.004f), 1f);
            return Color.HSVToRGB(hue, 1f, 1f);
        }

        var configHtmlColor = MalumMenu.menuHtmlColor?.Value;
        if (string.IsNullOrEmpty(configHtmlColor))
        {
            return Color.white; // Default untinted IMGUI color
        }

        // Gradient theme: "grad:#AAAAAA,#BBBBBB" — smooth continuous traveling wave
        if (configHtmlColor.StartsWith("grad:"))
        {
            var parts = configHtmlColor.Substring(5).Split(',');
            if (parts.Length == 2
                && ColorUtility.TryParseHtmlString(parts[0], out var a)
                && ColorUtility.TryParseHtmlString(parts[1], out var b))
            {
                // Smooth traveling wave: (sin(time * speed + spatialOffset * frequency) + 1) / 2
                float wave = (Mathf.Sin((Time.time * speed) + (spatialOffset * frequency)) + 1f) * 0.5f;
                return Color.Lerp(a, b, wave);
            }
        }

        // Solid theme / custom color (html code, with or without a leading '#')
        if (ColorUtility.TryParseHtmlString(configHtmlColor, out var uiColor))
        {
            return uiColor;
        }
        if (!configHtmlColor.StartsWith("#") && ColorUtility.TryParseHtmlString("#" + configHtmlColor, out uiColor))
        {
            return uiColor;
        }

        return Color.white; // Default untinted IMGUI color
    }
}
