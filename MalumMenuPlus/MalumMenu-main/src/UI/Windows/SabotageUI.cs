using UnityEngine;

namespace MalumMenu;

// Submenu (opened from Ship > Sabotage > "Show Sabotage Menu") holding all the sabotage toggles and
// actions that used to live inline on the Ship tab. Behaves like every other pop-up window: gated on
// showSabotageMenu, scalable via WindowScales ("Sabotage"), and honours Keep Subwindows Open / panic.
public class SabotageUI : MonoBehaviour
{
    // Laid out in two columns (sabotages | controls) so it's a wide landscape box rather than a tall
    // skinny one, and sized to fit every row without a scrollbar even with the timer slider expanded.
    public static int windowHeight = 310;
    public static int windowWidth = 500;
    public static Rect windowRect;

    private void Start()
    {
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (!CheatToggles.showSabotageMenu || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        var prevMatrix = GUI.matrix;
        float scale = CheatToggles.GetWindowScale("Sabotage");
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), windowRect.position);
        windowRect = GUI.Window((int)WindowId.SabotageUI, windowRect, (GUI.WindowFunction)SabotageWindow, "Sabotage");
        GUI.matrix = prevMatrix;
    }

    private void SabotageWindow(int windowID)
    {
        try
        {
            GUILayout.BeginHorizontal();

            // Left column: the sabotage toggles.
            GUILayout.BeginVertical(GUILayout.Width(windowWidth * 0.46f));

            GUILayout.Label("Sabotages", GUIStylePreset.TabSubtitle);

            CheatToggles.reactorSab = GUILayout.Toggle(CheatToggles.reactorSab, " Reactor");

            CheatToggles.oxygenSab = GUILayout.Toggle(CheatToggles.oxygenSab, " Oxygen");

            CheatToggles.elecSab = GUILayout.Toggle(CheatToggles.elecSab, " Lights");

            CheatToggles.unfixableLights = GUILayout.Toggle(CheatToggles.unfixableLights, " Unfixable Lights");

            CheatToggles.autoMovingSwitches = GUILayout.Toggle(CheatToggles.autoMovingSwitches, " Disable Lights (Moving Switches)");

            var newCommsSab = GUILayout.Toggle(CheatToggles.commsSab, " Comms");
            if (newCommsSab && !CheatToggles.commsSab) CheatToggles.unfixableComms = false; // mutually exclusive with Unfixable Comms
            CheatToggles.commsSab = newCommsSab;

            var newUnfixComms = GUILayout.Toggle(CheatToggles.unfixableComms, " Unfixable Comms");
            if (newUnfixComms && !CheatToggles.unfixableComms) CheatToggles.commsSab = false; // mutually exclusive with the Comms sabotage
            CheatToggles.unfixableComms = newUnfixComms;

            CheatToggles.mushSab = GUILayout.Toggle(CheatToggles.mushSab, " Mushroom Mixup");

            CheatToggles.mushSpore = GUILayout.Toggle(CheatToggles.mushSpore, " Trigger Spores");

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Right column: menus, fixes and actions.
            GUILayout.BeginVertical();

            GUILayout.Label("Controls", GUIStylePreset.TabSubtitle);

            CheatToggles.showDoorsMenu = GUILayout.Toggle(CheatToggles.showDoorsMenu, " Show Doors Menu");

            CheatToggles.sabotageMap = GUILayout.Toggle(CheatToggles.sabotageMap, " Open Sabotage Map");

            CheatToggles.disableSabotage = GUILayout.Toggle(CheatToggles.disableSabotage, " Disable Sabotage");

            CheatToggles.autoFixCriticalSab = GUILayout.Toggle(CheatToggles.autoFixCriticalSab, " Auto-Fix Reactor/O2 (Last 3s)");

            // Host-only, server-side custom countdown for Reactor / Oxygen (and Airship reactor). Only shown
            // when you're the host (it's host-authoritative and does nothing otherwise).
            if (Utils.isHost)
            {
                CheatToggles.customSabTimer = GUILayout.Toggle(CheatToggles.customSabTimer, " Custom Sabotage Timer");
                if (CheatToggles.customSabTimer)
                {
                    GUILayout.Label($"Timer: {CheatToggles.customSabTimerSeconds:0}s", GUIStylePreset.Hint);
                    CheatToggles.customSabTimerSeconds = Mathf.Round(
                        GUILayout.HorizontalSlider(CheatToggles.customSabTimerSeconds, 5f, 999f));
                }
            }
            else
            {
                CheatToggles.customSabTimer = false;
            }

            if (GUILayout.Button("Sabotage All", GUIStylePreset.NormalButton))
                CheatToggles.sabotageAll = true;

            if (GUILayout.Button("Random Sabotage", GUIStylePreset.NormalButton))
                CheatToggles.randomSabotage = true;

            if (GUILayout.Button("Fix Sabotages", GUIStylePreset.NormalButton))
                CheatToggles.fixSabotage = true;

            CheatToggles.sabotageAllDoors = GUILayout.Toggle(CheatToggles.sabotageAllDoors, " Sabotage All: Pin Doors");

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        catch { }

        GUI.DragWindow();
    }
}
