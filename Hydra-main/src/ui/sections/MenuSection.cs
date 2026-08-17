using System;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class MenuSection : ISection
	{
		public MenuSection() : base("Menu") { }

		public override void Render()
		{
			Hydra.notifications.DisableNotifications = GUILayout.Toggle(Hydra.notifications.DisableNotifications, "Disable Notifications");

			GUILayout.Space(6);
			GUILayout.Label("<b>Theme Mode:</b>");

			GUILayout.BeginHorizontal();
			Color prevBg = GUI.backgroundColor;

			if (Styles.activeThemeMode == Styles.ThemeMode.Solid) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
			if (GUILayout.Button("Solid"))
			{
				Styles.activeThemeMode = Styles.ThemeMode.Solid;
				Styles.ClearCache();
			}
			GUI.backgroundColor = prevBg;

			if (Styles.activeThemeMode == Styles.ThemeMode.RGB) GUI.backgroundColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.35f, 1f), 1f, 1f);
			if (GUILayout.Button("RGB Wave"))
			{
				Styles.activeThemeMode = Styles.ThemeMode.RGB;
				Styles.ClearCache();
			}
			GUI.backgroundColor = prevBg;

			if (Styles.activeThemeMode == Styles.ThemeMode.Gradient) GUI.backgroundColor = Styles.GetActiveColor();
			if (GUILayout.Button("Wave Gradients"))
			{
				Styles.activeThemeMode = Styles.ThemeMode.Gradient;
				Styles.ClearCache();
			}
			GUI.backgroundColor = prevBg;
			GUILayout.EndHorizontal();

			GUILayout.Space(6);

			// Render controls depending on selected Theme Mode
			if (Styles.activeThemeMode == Styles.ThemeMode.Solid)
			{
				GUILayout.Label($"Primary Color: <b>{Styles.primaryColor}</b>");

				int currentColorIndex = (int)Styles.primaryColor;
				int maxColorIndex = Styles.ColorValues.Count - 1;

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", GUILayout.Width(35 * MainUI.scale)))
				{
					currentColorIndex = currentColorIndex > 0 ? currentColorIndex - 1 : maxColorIndex;
					Styles.primaryColor = (Styles.UIColors)currentColorIndex;
					Styles.ClearCache();
				}

				float newSliderVal = GUILayout.HorizontalSlider(currentColorIndex, 0, maxColorIndex);
				int newColorIndex = Mathf.Clamp((int)Math.Round(newSliderVal), 0, maxColorIndex);
				if (newColorIndex != (int)Styles.primaryColor)
				{
					Styles.primaryColor = (Styles.UIColors)newColorIndex;
					Styles.ClearCache();
				}

				if (GUILayout.Button(">", GUILayout.Width(35 * MainUI.scale)))
				{
					currentColorIndex = currentColorIndex < maxColorIndex ? currentColorIndex + 1 : 0;
					Styles.primaryColor = (Styles.UIColors)currentColorIndex;
					Styles.ClearCache();
				}
				GUILayout.EndHorizontal();
			}
			else if (Styles.activeThemeMode == Styles.ThemeMode.RGB)
			{
				GUILayout.Label("<color=#888888>RGB Wave mode smoothly ripples full-spectrum rainbow colors across the menu.</color>");
			}
			else if (Styles.activeThemeMode == Styles.ThemeMode.Gradient)
			{
				int maxGrad = Styles.Gradients.Length - 1;
				int curGrad = Mathf.Clamp(Styles.selectedGradientIndex, 0, maxGrad);

				GUILayout.Label($"Active Gradient: <b>{Styles.Gradients[curGrad].name}</b> ({curGrad + 1}/{Styles.Gradients.Length})");

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", GUILayout.Width(35 * MainUI.scale)))
				{
					Styles.selectedGradientIndex = curGrad > 0 ? curGrad - 1 : maxGrad;
					Styles.ClearCache();
				}

				float newGradVal = GUILayout.HorizontalSlider(curGrad, 0, maxGrad);
				int newGradIndex = Mathf.Clamp((int)Math.Round(newGradVal), 0, maxGrad);
				if (newGradIndex != Styles.selectedGradientIndex)
				{
					Styles.selectedGradientIndex = newGradIndex;
					Styles.ClearCache();
				}

				if (GUILayout.Button(">", GUILayout.Width(35 * MainUI.scale)))
				{
					Styles.selectedGradientIndex = curGrad < maxGrad ? curGrad + 1 : 0;
					Styles.ClearCache();
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(6);
				GUILayout.Label("Gradient Presets (Click to Equip):");

				// 2-column grid of all 24 gradients with live moving wave previews
				for (int i = 0; i < Styles.Gradients.Length; i += 2)
				{
					GUILayout.BeginHorizontal();

					// Left button
					float tLeft = (Mathf.Sin((Time.time * 2.2f) + (i * 0.4f)) + 1f) * 0.5f;
					Color colLeft = Color.Lerp(Styles.Gradients[i].a, Styles.Gradients[i].b, tLeft);
					GUI.backgroundColor = colLeft;
					if (GUILayout.Button(Styles.Gradients[i].name, GUILayout.Height(24 * MainUI.scale)))
					{
						Styles.selectedGradientIndex = i;
						Styles.activeThemeMode = Styles.ThemeMode.Gradient;
						Styles.ClearCache();
					}

					// Right button
					if (i + 1 < Styles.Gradients.Length)
					{
						float tRight = (Mathf.Sin((Time.time * 2.2f) + ((i + 1) * 0.4f)) + 1f) * 0.5f;
						Color colRight = Color.Lerp(Styles.Gradients[i + 1].a, Styles.Gradients[i + 1].b, tRight);
						GUI.backgroundColor = colRight;
						if (GUILayout.Button(Styles.Gradients[i + 1].name, GUILayout.Height(24 * MainUI.scale)))
						{
							Styles.selectedGradientIndex = i + 1;
							Styles.activeThemeMode = Styles.ThemeMode.Gradient;
							Styles.ClearCache();
						}
					}

					GUI.backgroundColor = prevBg;
					GUILayout.EndHorizontal();
				}
			}

			GUILayout.Space(8);
			GUILayout.Label($"Menu Opacity: {Styles.menuOpacity * 100:F0}%");
			float newOpacity = (float)Math.Round(GUILayout.HorizontalSlider(Styles.menuOpacity, 0.2f, 1.0f), 2);
			if (Math.Abs(newOpacity - Styles.menuOpacity) > 0.001f)
			{
				Styles.menuOpacity = newOpacity;
				Styles.ClearCache();
			}

			GUILayout.Space(5);
			GUILayout.Label($"UI Scale: {MainUI.scale:F2}x");
			MainUI.scale = (float)Math.Round(GUILayout.HorizontalSlider(MainUI.scale, 0.5f, 2.0f), 2);

			GUILayout.Space(10);
			if (GUILayout.Button("Apply & Save Config"))
			{
				Styles.ClearCache();
				HydraConfig.Save();
				Hydra.notifications.Send("Hydra Config", "Theme and settings saved to config!", 2);
			}

			if (GUILayout.Button("Eject"))
			{
				Hydra.Eject();
			}
		}
	}
}