using System;
using UnityEngine;

namespace MalumMenu;

public class ConfigTab : ITab
{
    public string name => "Config";

    private static bool _isListeningForKey = false;
    private static float _pendingUiScale = 0f;

    // Advanced profile manager state
    private static int _selectedProfileIndex = 0;
    private static string _profileNameInput = "";
    private static bool _isTypingName = false;
    private static string _profileStatus = "";
    private static float _profileStatusUntil = 0f;

    // FPS type-in field state (IL2CPP-safe input, same approach as the profile name field)
    private static bool _isTypingFps = false;
    private static string _fpsInput = "";

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

    private void SetProfileStatus(string msg)
    {
        _profileStatus = msg;
        _profileStatusUntil = Time.unscaledTime + 4f;
    }

    // Builds the profile name from raw GUI key events (avoids GUILayout.TextField, which is unstripped
    // and crashes under IL2CPP). Enter/Escape stop editing, Backspace deletes, printable chars append.
    private void CaptureNameInput()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
        {
            _isTypingName = false;
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            if (!string.IsNullOrEmpty(_profileNameInput))
                _profileNameInput = _profileNameInput.Substring(0, _profileNameInput.Length - 1);
            e.Use();
            return;
        }

        char c = e.character;
        if (c != '\0' && !char.IsControl(c) && (_profileNameInput?.Length ?? 0) < 32)
        {
            _profileNameInput += c;
            e.Use();
        }
    }

    // Captures a typed FPS number from raw GUI key events (GUILayout.TextField crashes under IL2CPP).
    private void CaptureFpsInput()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
        {
            _isTypingFps = false;
            ApplyTypedFps();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.Backspace)
        {
            if (!string.IsNullOrEmpty(_fpsInput))
                _fpsInput = _fpsInput.Substring(0, _fpsInput.Length - 1);
            e.Use();
            return;
        }

        char c = e.character;
        if (c >= '0' && c <= '9' && (_fpsInput?.Length ?? 0) < 10)
        {
            _fpsInput += c;
            e.Use();
        }
    }

    // Applies the typed FPS value (clamped to 1 .. int.MaxValue) to the unlocker and saved config.
    private void ApplyTypedFps()
    {
        if (long.TryParse(_fpsInput, out long typed))
        {
            if (typed < 1) typed = 1;
            if (typed > int.MaxValue) typed = int.MaxValue;
            FpsUnlocker.TargetFps = (int)typed;
            if (MalumMenu.fpsLimit != null) MalumMenu.fpsLimit.Value = FpsUnlocker.TargetFps;
        }
        _fpsInput = "";
    }

    private void DrawProfile()
    {
        GUILayout.Label("Profiles", GUIStylePreset.TabSubtitle);

        var profiles = ProfileManager.Profiles;

        if (profiles.Count == 0)
        {
            // Shouldn't happen (Initialize seeds Default), but guard the UI anyway
            if (GUILayout.Button("Create Default Profile", GUIStylePreset.NormalButton))
            {
                ProfileManager.Create("Default");
                SetProfileStatus("Created Default");
            }
        }
        else
        {
            _selectedProfileIndex = Mathf.Clamp(_selectedProfileIndex, 0, profiles.Count - 1);
            string selected = profiles[_selectedProfileIndex];
            bool isActive = string.Equals(selected, ProfileManager.CurrentProfile, System.StringComparison.OrdinalIgnoreCase);

            // Selector row: ◀  <name> (active)  ▶
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUIStylePreset.NormalButton, GUILayout.Width(28)))
                _selectedProfileIndex = (_selectedProfileIndex - 1 + profiles.Count) % profiles.Count;

            GUILayout.FlexibleSpace();
            GUILayout.Label($"<b>{selected}</b>{(isActive ? " <color=#00d0ff>(active)</color>" : "")}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶", GUIStylePreset.NormalButton, GUILayout.Width(28)))
                _selectedProfileIndex = (_selectedProfileIndex + 1) % profiles.Count;
            GUILayout.EndHorizontal();

            GUILayout.Label($"<size=10><color=#888888>{profiles.Count} profile(s)</color></size>");

            // Load / Save(update)
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load", GUIStylePreset.NormalButton))
            {
                if (ProfileManager.Load(selected)) SetProfileStatus($"Loaded \"{selected}\"");
            }
            if (GUILayout.Button("Update", GUIStylePreset.NormalButton))
            {
                ProfileManager.SaveAs(selected);
                SetProfileStatus($"Saved current settings to \"{selected}\"");
            }
            GUILayout.EndHorizontal();

            // Delete
            if (GUILayout.Button("Delete", GUIStylePreset.NormalButton))
            {
                if (ProfileManager.Delete(selected))
                {
                    _selectedProfileIndex = Mathf.Clamp(_selectedProfileIndex, 0, ProfileManager.Profiles.Count - 1);
                    SetProfileStatus($"Deleted \"{selected}\"");
                }
            }
        }

        // Name field for New / Rename. GUILayout.TextField throws "Method unstripping failed" in
        // IL2CPP, so we capture typed characters from the GUI event stream ourselves instead (same
        // idea as the menu-keybind field above).
        GUILayout.Space(4);
        string nameLabel = _isTypingName
            ? $"<color=yellow>{(string.IsNullOrEmpty(_profileNameInput) ? "Type a name..." : _profileNameInput)}_</color>"
            : (string.IsNullOrEmpty(_profileNameInput) ? "Name: <i>click to type</i>" : $"Name: <b>{_profileNameInput}</b>");
        if (GUILayout.Button(nameLabel, GUIStylePreset.NormalButton, GUILayout.Height(24)))
            _isTypingName = !_isTypingName;

        if (_isTypingName) CaptureNameInput();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("New", GUIStylePreset.NormalButton))
        {
            if (ProfileManager.Create(_profileNameInput))
            {
                _selectedProfileIndex = ProfileManager.Profiles.IndexOf(ProfileManager.CurrentProfile);
                SetProfileStatus($"Created \"{ProfileManager.CurrentProfile}\"");
                _profileNameInput = "";
                _isTypingName = false;
            }
            else
            {
                SetProfileStatus("<color=#FFAA00>Name empty or already exists</color>");
            }
        }
        if (GUILayout.Button("Rename", GUIStylePreset.NormalButton))
        {
            if (ProfileManager.Profiles.Count > 0)
            {
                string sel = ProfileManager.Profiles[Mathf.Clamp(_selectedProfileIndex, 0, ProfileManager.Profiles.Count - 1)];
                if (ProfileManager.Rename(sel, _profileNameInput))
                {
                    _selectedProfileIndex = ProfileManager.Profiles.IndexOf(ProfileManager.Sanitize(_profileNameInput));
                    SetProfileStatus($"Renamed to \"{ProfileManager.Sanitize(_profileNameInput)}\"");
                    _profileNameInput = "";
                    _isTypingName = false;
                }
                else
                {
                    SetProfileStatus("<color=#FFAA00>Name empty or already exists</color>");
                }
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Open Profiles Folder", GUIStylePreset.NormalButton))
            ProfileManager.OpenFolder();

        MalumMenu.autoLoadProfile.Value =
            GUILayout.Toggle(MalumMenu.autoLoadProfile.Value, " Auto Load on Startup");

        if (!string.IsNullOrEmpty(_profileStatus) && Time.unscaledTime < _profileStatusUntil)
            GUILayout.Label($"<size=11>{_profileStatus}</size>");

        GUILayout.Space(6);
        GUILayout.Label("Config File", GUIStylePreset.TabSubtitle);
        CheatToggles.openConfig = GUILayout.Toggle(CheatToggles.openConfig, " Open Config");
        CheatToggles.reloadConfig = GUILayout.Toggle(CheatToggles.reloadConfig, " Reload Config");
    }

    private void DrawMenu()
    {
        GUILayout.Label("Menu", GUIStylePreset.TabSubtitle);

        CheatToggles.showWindowScales = GUILayout.Toggle(CheatToggles.showWindowScales, " Pop-up Window Scales");

        CheatToggles.showKeybindSettings = GUILayout.Toggle(CheatToggles.showKeybindSettings, " Keybind Settings");

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
        if (GUIUtility.hotControl == 0)
        {
            if (Math.Abs(MenuUI.uiScale - _pendingUiScale) > 0.001f && _pendingUiScale != 0f)
            {
                MenuUI.uiScale = _pendingUiScale;
                if (MalumMenu.menuScale != null)
                {
                    MalumMenu.menuScale.Value = MenuUI.uiScale;
                }
            }
            else
            {
                _pendingUiScale = MenuUI.uiScale;
            }
        }

        GUILayout.Label($"Scale: {_pendingUiScale:F2}x");
        _pendingUiScale = GUILayout.HorizontalSlider(_pendingUiScale, 0.5f, 2f);

        GUILayout.Label($"Opacity: {MenuUI.uiOpacity * 100:F0}%");
        float prevOpacity = MenuUI.uiOpacity;
        MenuUI.uiOpacity = GUILayout.HorizontalSlider(MenuUI.uiOpacity, 0.1f, 1f);
        if (Math.Abs(MenuUI.uiOpacity - prevOpacity) > 0.001f && MalumMenu.menuOpacity != null)
        {
            MalumMenu.menuOpacity.Value = MenuUI.uiOpacity;
        }


        MalumMenu.menuOpenOnMouse.Value =
            GUILayout.Toggle(MalumMenu.menuOpenOnMouse.Value, " Open on Cursor");

        MalumMenu.menuKeepSubwindowsOpen.Value =
            GUILayout.Toggle(MalumMenu.menuKeepSubwindowsOpen.Value, " Keep Subwindows Open");

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

        if (_isTypingFps) CaptureFpsInput();

        string fpsCurrent = FpsUnlocker.TargetFps == int.MaxValue ? "Unlimited" : FpsUnlocker.TargetFps.ToString();
        string fpsLabel = _isTypingFps
            ? $"<color=yellow>{(string.IsNullOrEmpty(_fpsInput) ? "Type FPS..." : _fpsInput)}_</color>"
            : $"FPS Limit: <b>{fpsCurrent}</b> <i>(click to type)</i>";
        if (GUILayout.Button(fpsLabel, GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            _isTypingFps = !_isTypingFps;
            _isTypingName = false; // avoid two key-capture fields active at once
            if (_isTypingFps) _fpsInput = "";
            else ApplyTypedFps();
        }
        if (GUILayout.Button("Unlock FPS", GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            FpsUnlocker.TargetFps = int.MaxValue; // effectively unlimited - render as fast as the device can
            CheatToggles.unlockFps = true;        // turn the feature on so it applies immediately
            _isTypingFps = false;
            if (MalumMenu.fpsLimit != null) MalumMenu.fpsLimit.Value = FpsUnlocker.TargetFps;
        }

        GUILayout.Space(6);
        GUILayout.Label("Time Format");
        GUILayout.BeginHorizontal();
        var prevTsBg = GUI.backgroundColor;
        GUI.backgroundColor = CheatToggles.chatTimestamp24hr ? new Color(0.2f, 0.85f, 0.5f) : prevTsBg;
        if (GUILayout.Button("24hr", GUIStylePreset.NormalButton, GUILayout.Width(90f))) CheatToggles.chatTimestamp24hr = true;
        GUI.backgroundColor = !CheatToggles.chatTimestamp24hr ? new Color(0.2f, 0.85f, 0.5f) : prevTsBg;
        if (GUILayout.Button("12hr", GUIStylePreset.NormalButton, GUILayout.Width(90f))) CheatToggles.chatTimestamp24hr = false;
        GUI.backgroundColor = prevTsBg;
        GUILayout.EndHorizontal();
    }

    private void DrawModes()
    {
        GUILayout.Label("Modes", GUIStylePreset.TabSubtitle);

        if (GUILayout.Button("Eject", GUIStylePreset.NormalButton, GUILayout.Width(200)))
        {
            Utils.Eject();
        }

        GUILayout.Space(6);
        CheatToggles.showStuffTab = GUILayout.Toggle(CheatToggles.showStuffTab, " Show \"Stuff\" Tab...... <size=11><color=#888888>(Contains Inappropriate Options)</color></size>");
    }
}
