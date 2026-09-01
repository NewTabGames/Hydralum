using UnityEngine;

namespace HydraMenu.ui
{
	public static class UIHelpers
	{
		public static void ApplyUIColor(float offset = 0f)
		{
			GUI.backgroundColor = GetGradientColor(offset);
		}

		public static Color GetGradientColor(float spatialOffset = 0f, float speed = 2.2f, float frequency = 0.02f)
		{
			if (Hydra.mainUI.GetConfigData()?.RgbMode ?? false)
			{
				float hue = Mathf.Repeat((Time.time * 0.35f) + (spatialOffset * 0.004f), 1f);
				return Color.HSVToRGB(hue, 1f, 1f);
			}

			var configHtmlColor = Hydra.mainUI.GetConfigData()?.ThemeColor;
			if (string.IsNullOrEmpty(configHtmlColor))
			{
				// Fallback to legacy primary color enum if empty
				return Styles.ColorValues.ContainsKey(Styles.primaryColor) ? Styles.ColorValues[Styles.primaryColor] : new Color(0.0f, 0.50f, 1f);
			}

			if (configHtmlColor.StartsWith("grad:"))
			{
				var parts = configHtmlColor.Substring(5).Split(',');
				if (parts.Length == 2
					&& ColorUtility.TryParseHtmlString(parts[0], out var a)
					&& ColorUtility.TryParseHtmlString(parts[1], out var b))
				{
					float wave = (Mathf.Sin((Time.time * speed) + (spatialOffset * frequency)) + 1f) * 0.5f;
					return Color.Lerp(a, b, wave);
				}
			}

			if (ColorUtility.TryParseHtmlString(configHtmlColor, out var uiColor))
			{
				return uiColor;
			}
			if (!configHtmlColor.StartsWith("#") && ColorUtility.TryParseHtmlString("#" + configHtmlColor, out uiColor))
			{
				return uiColor;
			}

			return new Color(0.0f, 0.50f, 1f);
		}
	}
}
