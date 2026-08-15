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

			GUILayout.Space(5);
			GUILayout.Label($"Theme / Primary Color: {Styles.primaryColor}");

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

			GUILayout.Space(5);
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
			if(GUILayout.Button("Apply & Save Config"))
			{
				Styles.ClearCache();
				HydraConfig.Save();
				Hydra.notifications.Send("Hydra Config", "Settings saved to config!", 2);
			}

			if(GUILayout.Button("Eject"))
			{
				Hydra.Eject();
			}
		}
	}
}