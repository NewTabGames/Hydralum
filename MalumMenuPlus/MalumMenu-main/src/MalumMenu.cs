using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine.SceneManagement;
using System;
using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MalumMenu;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class MalumMenu : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);
    public static MalumMenu Plugin;
    public new static ManualLogSource Log;
    public static readonly string ProfilePath = Path.Combine(Paths.ConfigPath, "MalumProfile.txt");

    public static MenuUI menuUI;
    public static ConsoleUI consoleUI;
    public static DebugUI debugUI;
    public static RolesUI rolesUI;
    public static DoorsUI doorsUI;
    public static TasksUI tasksUI;
    public static ProtectUI protectUI;
    public static InventoryOutfitsUI inventoryOutfitsUI;
    public static KeybindListener keybindListener;

    public static string malumVersion = "3.3.0";
    public static List<string> supportedAU = new List<string> { "2026.8.18", "2026.8.18s", "2026.6.5", "2026.3.31" };
    public static bool isPanicked = false;

    public static ConfigEntry<string> menuKeybind;
    public static ConfigEntry<string> menuHtmlColor;
    public static ConfigEntry<float> menuScale;
    public static ConfigEntry<float> menuOpacity;
    public static ConfigEntry<bool> menuOpenOnMouse;
    public static ConfigEntry<bool> menuKeepSubwindowsOpen;
    public static ConfigEntry<bool> menuAllowClickThrough;
    public static ConfigEntry<bool> showVersionWarning;
    public static ConfigEntry<string> spoofLevel;
    public static ConfigEntry<string> spoofPlatform;
    public static ConfigEntry<bool> spoofDeviceId;
    public static ConfigEntry<bool> noTelemetry;
    public static ConfigEntry<string> guestFriendCode;
    public static ConfigEntry<bool> guestMode;
    public static ConfigEntry<bool> autoLoadProfile;
    public static ConfigEntry<string> configEditor;
    public static ConfigEntry<int> fpsLimit;
    public static ConfigEntry<byte> colorSniperTargetColor;
    public static ConfigEntry<bool> colorSniperEnabled;
    public static ConfigEntry<bool> showWardrobeOverlay;
    public static ConfigEntry<bool> hideMyGem;
    public static ConfigEntry<bool> hideAllGems;
    public static ConfigEntry<bool> useSnapToRpc;

    public override void Load()
    {
        Log = base.Log;
        Plugin = this;

        // Force Stuff tab to be always disabled on launch
        CheatToggles.showStuffTab = false;

        // Loads config settings
        menuKeybind = Config.Bind("MalumMenu.GUI",
                                "Keybind",
                                "Delete",
                                "The keyboard key used to toggle the GUI on and off. List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");

        menuHtmlColor = Config.Bind("MalumMenu.GUI",
                                "Color",
                                "",
                                "A custom color for your MalumMenu GUI. Supports html color codes");

        menuScale = Config.Bind("MalumMenu.GUI",
                                "Scale",
                                1.0f,
                                "Menu scale multiplier (0.5 to 2.0)");

        menuOpacity = Config.Bind("MalumMenu.GUI",
                                "Opacity",
                                1.0f,
                                "Menu opacity (0.1 to 1.0)");

        menuOpenOnMouse = Config.Bind("MalumMenu.GUI",
                                "OpenOnMouse",
                                false,
                                "When enabled, the MalumMenu GUI will always be opened at the current mouse position");

        menuKeepSubwindowsOpen = Config.Bind("MalumMenu.GUI",
                                "KeepSubwindowsOpen",
                                false,
                                "When enabled, closing the MalumMenu GUI will not automatically close its subwindows");

        menuAllowClickThrough = Config.Bind("MalumMenu.GUI",
                                "AllowClicksThrough",
                                false,
                                "When enabled, clicks pass through the MalumMenu GUI, letting you interact with Among Us GUI elements behind it");

        showVersionWarning = Config.Bind("MalumMenu.GUI",
                                "ShowVersionWarning",
                                true,
                                "When enabled, a warning popup will appear at main menu if your Among Us version is not in the supported list");

        autoLoadProfile = Config.Bind("MalumMenu.Profile",
                                "AutoLoadProfile",
                                false,
                                "When enabled, your saved keybind and toggle profile will be automatically loaded at game startup");

        configEditor = Config.Bind("MalumMenu.Config",
                                "ConfigEditor",
                                "notepad.exe",
                                "The program used to open the config file when using the Open Config toggle. Can be any executable, but using a text editor is recommended");

        fpsLimit = Config.Bind("MalumMenu.Account",
                                "FpsLimit",
                                60,
                                "Target FPS for Unlock FPS toggle (30 to 240)");

        useSnapToRpc = Config.Bind("MalumMenu.Movement",
                                "UseSnapToRpc",
                                true,
                                "Use SnapTo RPC for teleports");

        MenuUI.uiScale = menuScale.Value;
        MenuUI.uiOpacity = menuOpacity.Value;
        FpsUnlocker.TargetFps = fpsLimit.Value;
        CheatToggles.useSnapToRpc = useSnapToRpc.Value;

        // GuestMode config settings are commented out as the cheats are broken in latest updates

        // guestMode = Config.Bind("MalumMenu.GuestMode",
        //                         "GuestMode",
        //                         false,
        //                         "When enabled, a new guest account will generate every time you start the game, allowing you to bypass account bans and PUID detection");

        // guestFriendCode = Config.Bind("MalumMenu.GuestMode",
        //                         "FriendName",
        //                         "",
        //                         "The username that will be used when setting a friend code for your guest account. IMPORTANT: Can only be used with GuestMode, needs to be ≤ 10 characters, and cannot include special characters/discriminator (#1234)");

        spoofLevel = Config.Bind("MalumMenu.Spoofing",
                                "Level",
                                "",
                                "A custom player level to display to others in online games to hide your actual platform. IMPORTANT: Custom levels can only be within 1 and 100001. Decimal numbers will not work");

        spoofPlatform = Config.Bind("MalumMenu.Spoofing",
                                "Platform",
                                "",
                                "A custom gaming platform to display to others in online lobbies to hide your actual platform. List of supported platforms: https://skeld.js.org/enums/_skeldjs_constant.Platform.html");

        spoofDeviceId = Config.Bind("MalumMenu.Privacy",
                                "HideDeviceId",
                                true,
                                "When enabled, it will hide your unique deviceId from Among Us, which could potentially help bypass hardware bans in the future");

        noTelemetry = Config.Bind("MalumMenu.Privacy",
                                "NoTelemetry",
                                true,
                                "When enabled, it will stop Among Us from collecting analytics of your games and sending them to Innersloth using Unity Analytics");

        colorSniperTargetColor = Config.Bind("MalumMenu.Outfits",
                                "ColorSniperTargetColor",
                                (byte)0,
                                "The saved target color ID (0-17) for Color Sniper");

        colorSniperEnabled = Config.Bind("MalumMenu.Outfits",
                                "ColorSniperEnabled",
                                false,
                                "When enabled, Color Sniper will automatically snipe your target color in lobbies");

        showWardrobeOverlay = Config.Bind("MalumMenu.Outfits",
                                "ShowWardrobeOverlay",
                                true,
                                "When enabled, the outfit presets menu overlay will automatically appear when opening your Wardrobe/Inventory");

        hideMyGem = Config.Bind("MalumMenu.ESP",
                                "HideMyGem",
                                false,
                                "When enabled, your own gem emoji will be hidden on your screen (other Hydralum users can still see it)");

        hideAllGems = Config.Bind("MalumMenu.ESP",
                                "HideAllGems",
                                false,
                                "When enabled, all gem emojis will be hidden on your screen (other Hydralum users can still see them)");

        CheatToggles.colorSniperTargetColor = colorSniperTargetColor.Value;
        CheatToggles.colorSniper = colorSniperEnabled.Value;
        CheatToggles.showWardrobeOverlay = showWardrobeOverlay.Value;
        CheatToggles.hideMyGem = hideMyGem.Value;
        CheatToggles.hideAllGems = hideAllGems.Value;

        // Enabled by default
        CheatToggles.unlockFeatures = true;
        CheatToggles.freeCosmetics = true;
        CheatToggles.avoidPenalties = true;

        Harmony.PatchAll();

        // UI
        menuUI = AddComponent<MenuUI>();
        consoleUI = AddComponent<ConsoleUI>();
        debugUI = AddComponent<DebugUI>();
        doorsUI = AddComponent<DoorsUI>();
        tasksUI = AddComponent<TasksUI>();
        protectUI = AddComponent<ProtectUI>();
        inventoryOutfitsUI = AddComponent<InventoryOutfitsUI>();
        // rolesUI = AddComponent<RolesUI>();

        // Components
        keybindListener = AddComponent<KeybindListener>();

        // Disables Telemetry (haven't fully tested if it works, but according to Unity docs it should)
        if (noTelemetry.Value)
        {
            Analytics.enabled = false;
            Analytics.deviceStatsEnabled = false;
            PerformanceReporting.enabled = false;
        }

        // Create profile file if it is missing
        if (!File.Exists(ProfilePath))
        {
            CheatToggles.SaveTogglesToProfile();
        }

        // Auto load profile on start if needed
        if (autoLoadProfile.Value)
        {
            CheatToggles.LoadTogglesFromProfile();
        }

        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((scene, _) =>
        {
            if (scene.name == "MainMenu" && !isPanicked)
            {
                // Warns about unsupported AU versions if enabled
                if (showVersionWarning != null && showVersionWarning.Value && !supportedAU.Contains(Application.version))
                {
                    Utils.ShowPopup("\nThis version of MalumMenu and this version of Among Us are incompatible\n\nInstall the right version to avoid problems");
                }
            }
        }));

        PresenceTracker.Start();
    }

    public override bool Unload()
    {
        PresenceTracker.Stop();
        return base.Unload();
    }

    public void OnApplicationQuit()
    {
        PresenceTracker.Stop();
    }
}
