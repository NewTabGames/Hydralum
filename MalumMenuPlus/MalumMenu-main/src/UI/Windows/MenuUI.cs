using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace MalumMenu;

public class MenuUI : MonoBehaviour
{
    public static int windowHeight = 590;
    public static int windowWidth = 700;
    public static Rect _windowRect;
    public static Rect windowRect => _windowRect;

    public static bool isGUIActive = false;
    public static bool lastOpenedWasHydra = false;
    private List<ITab> _tabs = new();
    private int _selectedTab;
    private Vector2 _contentScroll = Vector2.zero; // vertical scroll for the active tab's body
    private Vector2 _tabScroll = Vector2.zero;     // vertical scroll for the tab-selector column
    public static float hue; // For RGB mode
    public static float uiScale = 1f;   // menu font scaling (Menu Scale slider)
    public static float uiOpacity = 1f; // menu transparency (Menu Opacity slider)

    private void Start()
    {
        // Add all tabs on start
        _tabs.Add(new MovementTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new ShipTab());
        _tabs.Add(new PlayersTab());
        _tabs.Add(new OutfitsTab());
        _tabs.Add(new ConsoleTab());
        _tabs.Add(new HostOnlyTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new ThemesTab());
        _tabs.Add(new InfoTab());
        _tabs.Add(new DebugTab());
        _tabs.Add(new PhysicsTab());

        // Instantiate 2D area of MenuUI
        _windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    public void InitStyles()
    {
        var size = Mathf.RoundToInt(GUIStylePreset.BaseSkinFont * uiScale);
        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = size;
        GUIStylePreset.ApplyFontScale(uiScale);
    }

    private void Update()
    {
        FpsUnlocker.Apply();

        MalumColorSniper.Update();

        // Vent Network hop keys (Right arrow = next / Left arrow = previous), polled here for GetKeyDown
        MalumCheats.VentNetworkInput();

        // Keep the Become Immortal fake-vent state in sync with its toggle
        MalumImmortality.Sync();

        // Disco Party recolors every player on an interval while hosting
        MalumHost.DiscoParty();

        // Spam Report Bodies forces a meeting on an interval while hosting
        MalumHost.ReportBodySpam();

        if (Input.GetKeyDown(Utils.StringToKeycode(MalumMenu.menuKeybind.Value)))
        {
            bool hydraOpen = IsHydraOpen();
            if (isGUIActive || hydraOpen)
            {
                lastOpenedWasHydra = hydraOpen;
                isGUIActive = false;
                CloseHydraMenu();
            }
            else
            {
                if (lastOpenedWasHydra)
                {
                    OpenHydraOnKeybind();
                }
                else
                {
                    isGUIActive = true;

                    if (MalumMenu.menuOpenOnMouse.Value)
                    {
                        Vector2 mousePosition = Input.mousePosition;
                        float x = Mathf.Clamp(mousePosition.x, 0, Mathf.Max(0, Screen.width - _windowRect.width));
                        float y = Mathf.Clamp(Screen.height - mousePosition.y, 0, Mathf.Max(0, Screen.height - _windowRect.height));
                        _windowRect.position = new Vector2(x, y);
                    }
                }
            }
        }

        if (CheatToggles.rgbMode)
        {
            hue += Time.deltaTime * 0.3f; // Adjust speed of color change, higher multiplier = faster
            if (hue > 1f) hue -= 1f; // Loop hue back to 0 when it exceeds 1
        }

        if (CheatToggles.panicMode) Utils.Panic();

        if (ModManager.Instance != null && ModManager.Instance.ModStamp != null)
        {
            ModManager.Instance.ModStamp.enabled = false;
        }

        if (CheatToggles.openConfig)
        {
            Utils.OpenConfigFile();
            CheatToggles.openConfig = false;
        }

        if (CheatToggles.reloadConfig)
        {
            MalumMenu.Plugin.Config.Reload();
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false; // Disable first to avoid saving it to profile
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }

        // Some cheats only work if the LocalPlayer exists, so they are turned off if it does not
        if(!Utils.isPlayer)
        {
            CheatToggles.setFakeRole = false;
            CheatToggles.setFakeAlive = false;
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.teleportPlayer = false;
            CheatToggles.spectate = false;
            CheatToggles.freecam = false;
            CheatToggles.killPlayer = false;
            CheatToggles.callMeeting = false;
        }

        // Some cheats only work if the ship exists, so they are turned off if it does not
        if(!Utils.isShip)
        {
            CheatToggles.sabotageMap = false;
            CheatToggles.sabotageAll = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.unfixableComms = false;
            CheatToggles.completeMyTasks = false;
            CheatToggles.kickVents = false;
            CheatToggles.disableVents = false;
            CheatToggles.reportBody = false;
            CheatToggles.closeMeeting = false;
            CheatToggles.reactorSab = false;
            CheatToggles.oxygenSab = false;
            CheatToggles.commsSab = false;
            CheatToggles.elecSab = false;
            CheatToggles.mushSab = false;
            CheatToggles.closeAllDoors = false;
            CheatToggles.openAllDoors = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;
            CheatToggles.mushSpore = false;

            MalumCheats.StopShipAnimCheats();
        }

        if(!Utils.isHost && !Utils.isFreePlay)
        {
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.killPlayer = false;
            CheatToggles.ejectPlayer = false;
            CheatToggles.noKillCd = false;
            CheatToggles.killAnyone = false;
            CheatToggles.killVanished = false;
            CheatToggles.forceStartGame = false;
            CheatToggles.skipMeeting = false;
            CheatToggles.voteImmune = false;
            CheatToggles.noGameEnd = false;
            CheatToggles.showProtectMenu = false;
            CheatToggles.showRolesMenu = false;
            CheatToggles.noOptionsLimits = false;
            CheatToggles.disableCloseDoors = false;
            CheatToggles.disableMeetings = false;
            CheatToggles.banMidGame = false;
            CheatToggles.disableSecurityCameras = false;
            CheatToggles.assignRolesNextRound = false;
            CheatToggles.discoParty = false;
            CheatToggles.spamReportBodies = false;
        }

        // Some cheats only work if in a meeting, so they are turned off if it does not
        if (!Utils.isMeeting)
        {
            CheatToggles.skipMeeting = false;
            CheatToggles.ejectPlayer = false;
        }

        AnnouncementManager.Update();
        PresenceTracker.UpdateMainThread();
    }

    public void OnGUI()
    {
        AnnouncementManager.RenderToastGUI();

        if (!isGUIActive || MalumMenu.isPanicked) return;

        InitStyles();

        UIHelpers.ApplyUIColor();

        var previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, uiOpacity);

        _windowRect = GUI.Window((int)WindowId.MenuUI, _windowRect, (GUI.WindowFunction)WindowFunction, $"Hydralum v1.1.0 - Malum Menu v{MalumMenu.malumVersion}  |  Online: {PresenceTracker.GetOnlineCount()}");

        GUI.color = previousColor;
    }

    public void WindowFunction(int windowID)
    {
        GUILayout.BeginHorizontal();

        // Left tab selector (18% width), scrollable so a long tab list can't run off the bottom
        GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.18f));
        // Vertical-only: hide the horizontal scrollbar (GUIStyle.none) so the vertical bar eating a
        // few px of width can't trip a phantom horizontal bar. Vertical shows on demand.
        _tabScroll = GUILayout.BeginScrollView(_tabScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i] is PhysicsTab && !CheatToggles.showStuffTab)
            {
                if (_selectedTab == i) _selectedTab = 0;
                continue;
            }

            Color standardColor = GUI.backgroundColor;

            if (_selectedTab == i)
            {
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            }
            else
            {
                GUI.backgroundColor = UIHelpers.GetGradientColor(i * 35f);
            }

            if (GUILayout.Button(_tabs[i].name, GUIStylePreset.TabButton, GUILayout.Height(30)))
                _selectedTab = i;

            GUI.backgroundColor = standardColor;
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // Vertical separator line + invisible space to create gap between the tab selector and the content
        GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
        GUILayout.Space(10f);

        // Right tab content and controls: fill the remaining width (rather than a fixed 0.85 that
        // overflowed the window's right edge and pushed the scroll bar off-screen).
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // Tab-specific content
        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            if (_tabs[_selectedTab] is PhysicsTab && !CheatToggles.showStuffTab)
            {
                _selectedTab = 0;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            GUILayout.FlexibleSpace();
            Color defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.1f, 0.5f, 0.9f);
            if (GUILayout.Button("Switch", GUIStylePreset.NormalButton, GUILayout.Width(90), GUILayout.Height(24)))
            {
                SwitchToHydra();
            }
            GUI.backgroundColor = defaultBg;
            GUILayout.EndHorizontal();

            // Scroll the tab body so long tabs (or a smaller menu scale) don't run off the bottom.
            // The title above stays fixed; only the controls scroll. A vertical scrollbar appears
            // automatically when needed, and the flexible content reflows to keep width in-bounds.
            _contentScroll = GUILayout.BeginScrollView(_contentScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));

            try
            {
                _tabs[_selectedTab].Draw();
            }
            catch (System.Exception ex)
            {
                GUILayout.Label($"<color=red>Error rendering {_tabs[_selectedTab].name} tab:</color>\n<size=11>{ex.Message}</size>", GUIStylePreset.Hint);
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        // Make the window draggable
        GUI.DragWindow();
    }

    private static System.Type _cachedHydraUIType;
    public static System.Type GetHydraUIType()
    {
        if (_cachedHydraUIType != null) return _cachedHydraUIType;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            _cachedHydraUIType = asm.GetType("HydraMenu.ui.MainUI");
            if (_cachedHydraUIType != null) break;
        }
        return _cachedHydraUIType;
    }

    public static void SwitchToHydra()
    {
        lastOpenedWasHydra = true;
        isGUIActive = false;
        try
        {
            System.Type hydraType = GetHydraUIType();

            if (hydraType != null)
            {
                // Seamless in-place switch: match MalumMenu's current window position
                var posField = hydraType.GetField("windowPosition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (posField != null)
                {
                    posField.SetValue(null, new Vector2(_windowRect.x, _windowRect.y));
                }

                var visField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visField != null)
                {
                    visField.SetValue(null, true);
                    return;
                }
            }

            Utils.ShowPopup("\nHydraMenu DLL is not loaded in game");
        }
        catch (System.Exception ex)
        {
            MalumMenu.Log?.LogError($"Error switching to Hydra: {ex}");
        }
    }

    public static void OpenHydraOnKeybind()
    {
        lastOpenedWasHydra = true;
        isGUIActive = false;
        try
        {
            System.Type hydraType = GetHydraUIType();

            if (hydraType != null)
            {
                if (MalumMenu.menuOpenOnMouse.Value)
                {
                    var posField = hydraType.GetField("windowPosition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var sizeProp = hydraType.GetProperty("WindowSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (posField != null)
                    {
                        Vector2 mousePosition = Input.mousePosition;
                        Vector2 windowSize = new Vector2(500f, 470f);
                        if (sizeProp != null)
                        {
                            windowSize = (Vector2)sizeProp.GetValue(null, null);
                        }
                        float x = Mathf.Clamp(mousePosition.x, 0, Mathf.Max(0, Screen.width - windowSize.x));
                        float y = Mathf.Clamp(Screen.height - mousePosition.y, 0, Mathf.Max(0, Screen.height - windowSize.y));
                        posField.SetValue(null, new Vector2(x, y));
                    }
                }

                var visField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visField != null)
                {
                    visField.SetValue(null, true);
                    return;
                }
            }

            Utils.ShowPopup("\nHydraMenu DLL is not loaded in game");
        }
        catch (System.Exception ex)
        {
            MalumMenu.Log?.LogError($"Error opening Hydra: {ex}");
        }
    }

    public static void CloseHydraMenu()
    {
        try
        {
            System.Type hydraType = GetHydraUIType();

            if (hydraType != null)
            {
                var visField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visField != null)
                {
                    visField.SetValue(null, false);
                }
            }
        }
        catch { }
    }

    public static bool IsHydraOpen()
    {
        try
        {
            System.Type hydraType = GetHydraUIType();

            if (hydraType != null)
            {
                var visField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visField != null)
                {
                    return (bool)visField.GetValue(null);
                }
            }
        }
        catch { }
        return false;
    }

    public static Rect GetHydraRect()
    {
        try
        {
            System.Type hydraType = GetHydraUIType();
            if (hydraType != null)
            {
                var visField = hydraType.GetField("visible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (visField != null && (bool)visField.GetValue(null))
                {
                    var posField = hydraType.GetField("windowPosition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var sizeProp = hydraType.GetProperty("WindowSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    Vector2 pos = posField != null ? (Vector2)posField.GetValue(null) : new Vector2(250, 100);
                    Vector2 size = sizeProp != null ? (Vector2)sizeProp.GetValue(null, null) : new Vector2(500, 470);
                    return new Rect(pos.x, pos.y, size.x, size.y);
                }
            }
        }
        catch { }
        return Rect.zero;
    }
}
