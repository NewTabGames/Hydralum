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
        public static ConfigEntry<bool> DisableNotifications;
        public static ConfigEntry<float> WindowPosX;
        public static ConfigEntry<float> WindowPosY;

        public static void Init(ConfigFile config)
        {
            MenuScale = config.Bind("GUI", "Scale", 1.0f, "Menu UI scale factor (0.5 to 2.0)");
            MenuOpacity = config.Bind("GUI", "Opacity", 1.0f, "Menu opacity (0.0 to 1.0)");
            PrimaryColor = config.Bind("GUI", "PrimaryColor", 0, "Primary UI color scheme index");
            DisableNotifications = config.Bind("GUI", "DisableNotifications", false, "Disable in-game Hydra notifications");
            WindowPosX = config.Bind("GUI", "WindowPosX", 250f, "Saved window X position");
            WindowPosY = config.Bind("GUI", "WindowPosY", 100f, "Saved window Y position");

            // Apply loaded config values
            MainUI.scale = Mathf.Clamp(MenuScale.Value, 0.5f, 2.0f);
            Styles.menuOpacity = Mathf.Clamp(MenuOpacity.Value, 0f, 1f);
            Styles.primaryColor = (Styles.UIColors)Mathf.Clamp(PrimaryColor.Value, 0, Styles.ColorValues.Count - 1);
            MainUI.windowPosition = new Vector2(WindowPosX.Value, WindowPosY.Value);
        }

        public static void Save()
        {
            if (MenuScale != null) MenuScale.Value = MainUI.scale;
            if (MenuOpacity != null) MenuOpacity.Value = Styles.menuOpacity;
            if (PrimaryColor != null) PrimaryColor.Value = (int)Styles.primaryColor;
            if (DisableNotifications != null && Hydra.notifications != null)
                DisableNotifications.Value = Hydra.notifications.DisableNotifications;
            if (WindowPosX != null) WindowPosX.Value = MainUI.windowPosition.x;
            if (WindowPosY != null) WindowPosY.Value = MainUI.windowPosition.y;
        }
    }
}
