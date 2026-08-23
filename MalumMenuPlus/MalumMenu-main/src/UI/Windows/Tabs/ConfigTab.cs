using System;
using UnityEngine;

namespace MalumMenu;

public class ConfigTab : ITab
{
    public string name => "Config";

    private static bool _isListeningForKey = false;

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        // Left column
        GUILayout.BeginVertical(GUILayout.Width(230f));
        DrawProfile();
        GUILayout.Space(14);
        DrawMenu();
        GUILayout.EndVertical();

        GUILayout.Space(20);

        // Right column
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        DrawAccount();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(14);
        DrawModes();
    }

    private void DrawProfile()
    {
        GUILayout.Label("Profile", GUIStylePreset.TabSubtitle);

        CheatToggles.openConfig = GUILayout.Toggle(CheatToggles.openConfig, " Open Config");
        CheatToggles.reloadConfig = GUILayout.Toggle(CheatToggles.reloadConfig, " Reload Config");
        CheatToggles.saveProfile = GUILayout.Toggle(CheatToggles.saveProfile, " Save to Profile");
        CheatToggles.loadProfile = GUILayout.Toggle(CheatToggles.loadProfile, " Load from Profile");

        MalumMenu.autoLoadProfile.Value =
            GUILayout.Toggle(MalumMenu.autoLoadProfile.Value, " Auto Load on Startup");
    }

    private void DrawMenu()
    {
        GUILayout.Label("Menu", GUIStylePreset.TabSubtitle);

        GUILayout.Label("Menu Keybind:");

        string currentKey = string.IsNullOrEmpty(MalumMenu.menuKeybind.Value) ? "Delete" : MalumMenu.menuKeybind.Value;
        string btnText = _isListeningForKey ? "<color=yellow>Press any key...</color>" : $"Key: <b>{currentKey}</b>";

        if (GUILayout.Button(btnText, GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            _isListeningForKey = !_isListeningForKey;
        }

        if (_isListeningForKey)
        {
            if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
            {
                MalumMenu.menuKeybind.Value = Event.current.keyCode.ToString();
                _isListeningForKey = false;
            }
        }

        GUILayout.Space(4);
        GUILayout.Label("Scale:");
        MenuUI.uiScale = GUILayout.HorizontalSlider(MenuUI.uiScale, 0.5f, 2f);

        GUILayout.Label("Opacity:");
        MenuUI.uiOpacity = GUILayout.HorizontalSlider(MenuUI.uiOpacity, 0.1f, 1f);

        MalumMenu.menuOpenOnMouse.Value =
            GUILayout.Toggle(MalumMenu.menuOpenOnMouse.Value, " Open on Cursor");

        MalumMenu.menuKeepSubwindowsOpen.Value =
            GUILayout.Toggle(MalumMenu.menuKeepSubwindowsOpen.Value, " Keep Subwindows Open");

        MalumMenu.menuAllowClickThrough.Value =
            GUILayout.Toggle(MalumMenu.menuAllowClickThrough.Value, " Allow Clicks Through");

        MalumMenu.showVersionWarning.Value =
            GUILayout.Toggle(MalumMenu.showVersionWarning.Value, " Version Warning Popup");

        bool newOverlay = GUILayout.Toggle(CheatToggles.showWardrobeOverlay, " Wardrobe Overlay on Inventory");
        if (newOverlay != CheatToggles.showWardrobeOverlay)
        {
            CheatToggles.showWardrobeOverlay = newOverlay;
            if (MalumMenu.showWardrobeOverlay != null)
            {
                MalumMenu.showWardrobeOverlay.Value = newOverlay;
            }
        }
    }

    private void DrawAccount()
    {
        GUILayout.Label("Account", GUIStylePreset.TabSubtitle);

        CheatToggles.freeCosmetics = GUILayout.Toggle(CheatToggles.freeCosmetics, " Free Cosmetics");
        CheatToggles.avoidPenalties = GUILayout.Toggle(CheatToggles.avoidPenalties, " Avoid Penalties");
        CheatToggles.unlockFeatures = GUILayout.Toggle(CheatToggles.unlockFeatures, " Unlock Extra Features");
        CheatToggles.copyLobbyCodeOnDisconnect = GUILayout.Toggle(CheatToggles.copyLobbyCodeOnDisconnect, " Copy Lobby Code on Disconnect");
        CheatToggles.spoofAprilFoolsDate = GUILayout.Toggle(CheatToggles.spoofAprilFoolsDate, " Spoof Date to April 1st");
        CheatToggles.unlockFps = GUILayout.Toggle(CheatToggles.unlockFps, " Unlock FPS");

        FpsUnlocker.TargetFps = Mathf.RoundToInt(
            GUILayout.HorizontalSlider(FpsUnlocker.TargetFps, FpsUnlocker.MinFps, FpsUnlocker.MaxFps));

        GUILayout.Label($"FPS Limit: {FpsUnlocker.TargetFps}");
    }

    private void DrawModes()
    {
        GUILayout.Label("Modes", GUIStylePreset.TabSubtitle);

        if (GUILayout.Button("Eject", GUIStylePreset.NormalButton, GUILayout.Width(200)))
        {
            Utils.Eject();
        }

        GUILayout.Space(6);
        CheatToggles.showStuffTab = GUILayout.Toggle(CheatToggles.showStuffTab, " Are you sure you want to view this tab? its stupid...");
    }
}
