using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui
{
	internal class Styles
	{
		public enum ThemeMode
		{
			Solid = 0,
			RGB = 1,
			Gradient = 2
		}

		public enum UIColors
		{
			Azure,
			Carbon,
			Cardinal,
			Pesto,
			Pumpkin,
			White,
			Violet,
			Cyan,
			Emerald,
			Rose,
			Gold,
			Midnight,
			Ruby,
			Amethyst,
			Teal,
			Coral,
			Mint,
			Solar,
			Matrix,
			Synthwave
		}

		public static Dictionary<UIColors, Color> ColorValues = new Dictionary<UIColors, Color>()
		{
			{ UIColors.Azure, new Color(0.0f, 0.50f, 1f) },          // #007FFF (Electric Azure)
			{ UIColors.Carbon, new Color(0.07f, 0.07f, 0.07f) },     // #222222 (Stealth Carbon)
			{ UIColors.Cardinal, new Color(0.77f, 0.12f, 0.23f) },   // #C41E3A (Cardinal Red)
			{ UIColors.Pesto, new Color(0.05f, 0.5f, 0.13f) },       // #119922 (Pesto Green)
			{ UIColors.Pumpkin, new Color(1.0f, 0.18f, 0.04f) },     // #FF7518 (Vibrant Pumpkin)
			{ UIColors.White, new Color(0.95f, 0.95f, 0.97f) },       // #F0EFDF (Pure White)
			{ UIColors.Violet, new Color(0.5f, 0f, 1f) },            // #7F00FF (Royal Violet)
			{ UIColors.Cyan, new Color(0.0f, 0.90f, 1.0f) },         // #00E5FF (Hydra Cyan)
			{ UIColors.Emerald, new Color(0.06f, 0.72f, 0.51f) },    // #10B981 (Emerald Green)
			{ UIColors.Rose, new Color(1.0f, 0.08f, 0.58f) },        // #FF1493 (Neon Rose)
			{ UIColors.Gold, new Color(1.0f, 0.84f, 0.0f) },         // #FFD700 (Luminous Gold)
			{ UIColors.Midnight, new Color(0.12f, 0.23f, 0.54f) },   // #1E3A8A (Midnight Blue)
			{ UIColors.Ruby, new Color(0.88f, 0.11f, 0.28f) },       // #E11D48 (Blood Ruby)
			{ UIColors.Amethyst, new Color(0.66f, 0.33f, 0.97f) },   // #A855F7 (Amethyst Purple)
			{ UIColors.Teal, new Color(0.05f, 0.58f, 0.53f) },       // #0D9488 (Deep Ocean Teal)
			{ UIColors.Coral, new Color(1.0f, 0.42f, 0.42f) },       // #FF6B6B (Sunset Coral)
			{ UIColors.Mint, new Color(0.31f, 0.80f, 0.64f) },       // #4ECCA3 (Pastel Mint)
			{ UIColors.Solar, new Color(1.0f, 0.90f, 0.0f) },        // #FFE600 (Solar Yellow)
			{ UIColors.Matrix, new Color(0.0f, 1.0f, 0.25f) },       // #00FF41 (Terminal Matrix)
			{ UIColors.Synthwave, new Color(1.0f, 0.0f, 0.50f) }     // #FF007F (Synthwave Magenta)
		};

		public static readonly (string name, Color a, Color b)[] Gradients =
		{
			("Fire", new Color(1.0f, 0.31f, 0.0f), new Color(1.0f, 0.77f, 0.0f)),
			("Aurora", new Color(0.0f, 0.79f, 1.0f), new Color(0.57f, 1.0f, 0.62f)),
			("Galaxy", new Color(0.50f, 0.0f, 1.0f), new Color(0.88f, 0.0f, 1.0f)),
			("Ocean", new Color(0.18f, 0.19f, 0.57f), new Color(0.11f, 1.0f, 1.0f)),
			("Sunset", new Color(1.0f, 0.37f, 0.43f), new Color(1.0f, 0.76f, 0.44f)),
			("Mint", new Color(0.07f, 0.60f, 0.56f), new Color(0.22f, 0.94f, 0.49f)),
			("Cyberpunk", new Color(1.0f, 0.0f, 0.50f), new Color(0.0f, 0.94f, 1.0f)),
			("Vaporwave", new Color(1.0f, 0.44f, 0.81f), new Color(0.0f, 0.80f, 1.0f)),
			("Solar Flare", new Color(1.0f, 0.03f, 0.27f), new Color(1.0f, 0.69f, 0.60f)),
			("Matrix", new Color(0.0f, 1.0f, 0.53f), new Color(0.38f, 0.94f, 1.0f)),
			("Midnight", new Color(0.06f, 0.13f, 0.15f), new Color(0.17f, 0.33f, 0.39f)),
			("Amethyst", new Color(0.56f, 0.18f, 0.89f), new Color(0.29f, 0.0f, 0.88f)),
			("Blood Orange", new Color(0.95f, 0.15f, 0.07f), new Color(0.96f, 0.69f, 0.10f)),
			("Neon Lime", new Color(0.98f, 0.83f, 0.14f), new Color(0.66f, 1.0f, 0.47f)),
			("Lavender", new Color(0.63f, 0.55f, 0.82f), new Color(0.98f, 0.76f, 0.92f)),
			("Iceberg", new Color(0.34f, 0.80f, 0.95f), new Color(0.18f, 0.50f, 0.93f)),
			("Sakura", new Color(0.93f, 0.61f, 0.65f), new Color(1.0f, 0.87f, 0.88f)),
			("Synthwave", new Color(0.51f, 0.23f, 0.71f), new Color(0.99f, 0.11f, 0.11f)),
			("Cosmic", new Color(0.23f, 0.11f, 0.44f), new Color(0.84f, 0.43f, 0.47f)),
			("Emerald Forest", new Color(0.04f, 0.64f, 0.38f), new Color(0.24f, 0.73f, 0.57f)),
			("Electric Rose", new Color(0.97f, 0.34f, 0.65f), new Color(1.0f, 0.35f, 0.35f)),
			("Gold Mirage", new Color(1.0f, 0.89f, 0.35f), new Color(1.0f, 0.65f, 0.32f)),
			("Abyss", new Color(0.0f, 0.02f, 0.16f), new Color(0.0f, 0.31f, 0.57f)),
			("Tropical", new Color(0.0f, 0.95f, 0.38f), new Color(0.02f, 0.46f, 0.90f))
		};

		public static float menuOpacity = 0.85f;
		public static UIColors primaryColor = UIColors.Azure;
		public static ThemeMode activeThemeMode = ThemeMode.Solid;
		public static int selectedGradientIndex = 0;

		private static Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();

		public static Color GetActiveColor(float spatialOffset = 0f)
		{
			if (activeThemeMode == ThemeMode.RGB)
			{
				float hue = Mathf.Repeat((Time.time * 0.35f) + (spatialOffset * 0.004f), 1f);
				return Color.HSVToRGB(hue, 1f, 1f);
			}
			if (activeThemeMode == ThemeMode.Gradient)
			{
				int idx = Mathf.Clamp(selectedGradientIndex, 0, Gradients.Length - 1);
				var grad = Gradients[idx];
				float wave = (Mathf.Sin((Time.time * 2.2f) + (spatialOffset * 0.02f)) + 1f) * 0.5f;
				return Color.Lerp(grad.a, grad.b, wave);
			}
			return ColorValues.TryGetValue(primaryColor, out var col) ? col : ColorValues[UIColors.Azure];
		}

		public static GUIStyle MainBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("MainBox", ColorValues[UIColors.Carbon], menuOpacity);
				style.normal.background = background;

				style.normal.textColor = Color.white;
				style.alignment = TextAnchor.UpperCenter;
				style.padding.top = 5;
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle SectionBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.padding.bottom = 1;
				style.padding.left = (int)(8 * MainUI.scale);
				style.fontSize = (int)(14 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle SectionBoxActive
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("BoxActive_Base", Color.white);
				style.normal.background = background;

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.padding.bottom = 1;
				style.padding.left = (int)(13 * MainUI.scale);
				style.fontSize = (int)(MainUI.scale * 14);

				return style;
			}
		}

		public static GUIStyle PlayerBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.clipping = TextClipping.Clip;
				style.padding.left = (int)(10 * MainUI.scale);
				style.richText = true;
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle PlayerBoxActive
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("BoxActive_Base", Color.white);
				style.normal.background = background;

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.clipping = TextClipping.Clip;
				style.padding.left = (int)(10 * MainUI.scale);
				style.richText = true;
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle CreateCrewmateColorBox(string colorName, Color color)
		{
			GUIStyle style = new GUIStyle();

			Texture2D background = CreateColoredTexture(colorName, color);
			style.normal.background = background;

			return style;
		}

		private static Texture2D CreateColoredTexture(string textureName, Color color, float opacity = 1.0f)
		{
			CachedTextures.TryGetValue(textureName, out Texture2D background);
			if(background != null) return background;

			background = new Texture2D(1, 1);
			background.SetPixel(0, 0, color.SetAlpha(opacity));
			background.Apply();

			CachedTextures[textureName] = background;
			return background;
		}

		public static void ClearCache()
		{
			foreach(Texture2D texture in CachedTextures.Values)
			{
				Object.Destroy(texture);
			}
			CachedTextures.Clear();
		}
	}
}