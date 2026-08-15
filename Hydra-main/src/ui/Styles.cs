using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui
{
	internal class Styles
	{
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

		public static float menuOpacity = 0.85f;
		public static UIColors primaryColor = UIColors.Azure;

		private static Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();

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
				// The product of the font size and the UI scale will result in a float value with decimal values
				// which would get truncated if we cast this into an int
				// however this is rather insignificant as the font size would be at most one unit off
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

				Texture2D background = CreateColoredTexture($"SectionBoxActive_{primaryColor}", ColorValues[primaryColor]);
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

				Texture2D background = CreateColoredTexture($"PlayerBoxActive_{primaryColor}", ColorValues[primaryColor]);
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

			Hydra.Log.LogInfo($"Cache lookup for texture {textureName} returned a miss, creating the required texture...");

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