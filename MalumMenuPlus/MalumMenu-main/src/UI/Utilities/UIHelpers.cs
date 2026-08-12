using UnityEngine;

namespace MalumMenu;

public static class UIHelpers
{
    public static void ApplyUIColor()
    {
        // RGB mode cycles the full hue spectrum and overrides any theme
        if (CheatToggles.rgbMode)
        {
            GUI.backgroundColor = Color.HSVToRGB(MenuUI.hue, 1f, 1f);
            return;
        }

        var configHtmlColor = MalumMenu.menuHtmlColor.Value;

        // Gradient theme: "grad:#AAAAAA,#BBBBBB" — smoothly pulses between the two colors
        if (configHtmlColor.StartsWith("grad:"))
        {
            var parts = configHtmlColor.Substring(5).Split(',');
            if (parts.Length == 2
                && ColorUtility.TryParseHtmlString(parts[0], out var a)
                && ColorUtility.TryParseHtmlString(parts[1], out var b))
            {
                GUI.backgroundColor = Color.Lerp(a, b, Mathf.PingPong(Time.time * 0.5f, 1f));
            }
            return;
        }

        // Solid theme / custom color (html code, with or without a leading '#')
        if (!ColorUtility.TryParseHtmlString(configHtmlColor, out var uiColor))
        {
            if (!configHtmlColor.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString("#" + configHtmlColor, out uiColor))
                {
                    GUI.backgroundColor = uiColor;
                }
            }
        }
        else
        {
            GUI.backgroundColor = uiColor;
        }
    }
}
