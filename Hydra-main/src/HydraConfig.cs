using BepInEx.Configuration;
using HydraMenu.ui;
using UnityEngine;

namespace HydraMenu
{
    public static class HydraConfig
    {
        public static ConfigEntry<float> MenuScale;
        public static ConfigEntry<float> MenuOpacity;
        public static ConfigEntry<int> PrimaryColor;
        public static ConfigEntry<int> ThemeMode;
        public static ConfigEntry<int> GradientIndex;
        public static ConfigEntry<bool> OpenOnCursor;
        public static ConfigEntry<bool> DisableNotifications;
        public static ConfigEntry<float> WindowPosX;
        public static ConfigEntry<float> WindowPosY;
        public static ConfigEntry<bool> ColorSniperEnabled;
        public static ConfigEntry<int> ColorSniperTargetColor;

        public static void Init(ConfigFile config)
        {
            MenuScale = config.Bind("GUI", "Scale", 1.0f, "Menu UI scale factor (0.5 to 2.0)");
            MenuOpacity = config.Bind("GUI", "Opacity", 1.0f, "Menu opacity (0.0 to 1.0)");
            PrimaryColor = config.Bind("GUI", "PrimaryColor", 0, "Primary UI color scheme index");
            ThemeMode = config.Bind("GUI", "ThemeMode", 0, "Theme mode: 0 = Solid, 1 = RGB Wave, 2 = Wave Gradient");
            GradientIndex = config.Bind("GUI", "GradientIndex", 0, "Selected wave gradient preset index (0 to 23)");
            OpenOnCursor = config.Bind("GUI", "OpenOnCursor", true, "Open menu centered on mouse cursor position");
            DisableNotifications = config.Bind("GUI", "DisableNotifications", false, "Disable in-game Hydra notifications");
            WindowPosX = config.Bind("GUI", "WindowPosX", 250f, "Saved window X position");
            WindowPosY = config.Bind("GUI", "WindowPosY", 100f, "Saved window Y position");
            ColorSniperEnabled = config.Bind("Self", "ColorSniperEnabled", false, "Automatically grab your chosen color when available in lobby");
            ColorSniperTargetColor = config.Bind("Self", "ColorSniperTargetColor", 0, "Target color index for Color Sniper");

            // Apply loaded config values
            MainUI.scale = Mathf.Clamp(MenuScale.Value, 0.5f, 2.0f);
            Styles.menuOpacity = Mathf.Clamp(MenuOpacity.Value, 0f, 1f);
            Styles.primaryColor = (Styles.UIColors)Mathf.Clamp(PrimaryColor.Value, 0, Styles.ColorValues.Count - 1);
            Styles.activeThemeMode = (Styles.ThemeMode)Mathf.Clamp(ThemeMode.Value, 0, 2);
            Styles.selectedGradientIndex = Mathf.Clamp(GradientIndex.Value, 0, Styles.Gradients.Length - 1);
            MainUI.windowPosition = new Vector2(WindowPosX.Value, WindowPosY.Value);
            features.Self.ColorSniper.Enabled = ColorSniperEnabled.Value;
            features.Self.ColorSniper.TargetColor = (byte)Mathf.Clamp(ColorSniperTargetColor.Value, 0, (int)ui.Controls.PlayerColors.Fortegreen);
        }

        public static void Save()
        {
            if (MenuScale != null) MenuScale.Value = MainUI.scale;
            if (MenuOpacity != null) MenuOpacity.Value = Styles.menuOpacity;
            if (PrimaryColor != null) PrimaryColor.Value = (int)Styles.primaryColor;
            if (ThemeMode != null) ThemeMode.Value = (int)Styles.activeThemeMode;
            if (GradientIndex != null) GradientIndex.Value = Styles.selectedGradientIndex;
            if (OpenOnCursor != null) OpenOnCursor.Value = OpenOnCursor.Value;
            if (DisableNotifications != null && Hydra.notifications != null)
                DisableNotifications.Value = Hydra.notifications.DisableNotifications;
            if (WindowPosX != null) WindowPosX.Value = MainUI.windowPosition.x;
            if (WindowPosY != null) WindowPosY.Value = MainUI.windowPosition.y;
            if (ColorSniperEnabled != null) ColorSniperEnabled.Value = features.Self.ColorSniper.Enabled;
            if (ColorSniperTargetColor != null) ColorSniperTargetColor.Value = (int)features.Self.ColorSniper.TargetColor;
        }
    }
}
