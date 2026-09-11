using AmongUs.GameOptions;
using UnityEngine;

namespace MalumMenu;

public class RolesTab : ITab
{
    public string name => "Roles";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawImpostor();

        GUILayout.Space(15);

        DrawShapeshifter();

        GUILayout.Space(15);

        DrawCrewmate();

        GUILayout.Space(15);

        DrawTracker();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        DrawEngineer();

        GUILayout.Space(15);

        DrawScientist();

        GUILayout.Space(15);

        DrawDetective();

        GUILayout.Space(15);

        DrawGuardianAngel();

        GUILayout.Space(15);

        DrawChangeRole();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawGeneral()
    {
        CheatToggles.setFakeRole = GUILayout.Toggle(CheatToggles.setFakeRole, " Set Fake Role");

        CheatToggles.setFakeAlive = GUILayout.Toggle(CheatToggles.setFakeAlive, " Set Fake Alive");
    }

    private void DrawImpostor()
    {
        GUILayout.Label("Impostor", GUIStylePreset.TabSubtitle);

        CheatToggles.killReach = GUILayout.Toggle(CheatToggles.killReach, " Kill Reach");

        // Sabotaging in vents is provided by Hydra's Roles tab (single source of truth), so Malum no longer duplicates it.
        // CheatToggles.impostorTasks = GUILayout.Toggle(CheatToggles.impostorTasks, " Allow Tasks");
    }

    private void DrawShapeshifter()
    {
        GUILayout.Label("Shapeshifter", GUIStylePreset.TabSubtitle);

        CheatToggles.noShapeshiftAnim = GUILayout.Toggle(CheatToggles.noShapeshiftAnim, " No Ss Animation");

        CheatToggles.endlessSsDuration = GUILayout.Toggle(CheatToggles.endlessSsDuration, " Endless Ss Duration");
    }

    private void DrawCrewmate()
    {
        GUILayout.Label("Crewmate", GUIStylePreset.TabSubtitle);

        CheatToggles.showTasksMenu = GUILayout.Toggle(CheatToggles.showTasksMenu, " Show Tasks Menu");

        if (GUILayout.Button("Complete All Tasks"))
        {
            CheatToggles.completeMyTasks = true;
        }

        CheatToggles.autoCompleteTasks = GUILayout.Toggle(CheatToggles.autoCompleteTasks, " Auto Complete Tasks");
        CheatToggles.autoCompleteNoAlwaysUpdates = GUILayout.Toggle(CheatToggles.autoCompleteNoAlwaysUpdates, " Disable if Taskbar Updates = Always");
        
        GUILayout.Label($"Auto Complete Start Delay: {CheatToggles.autoCompleteDelay}s");
        CheatToggles.autoCompleteDelay = (float)System.Math.Round(GUILayout.HorizontalSlider(CheatToggles.autoCompleteDelay, 0f, 60f), 1);
        
        GUILayout.Label($"Time Between Tasks: {CheatToggles.autoCompleteInterval}s");
        CheatToggles.autoCompleteInterval = (float)System.Math.Round(GUILayout.HorizontalSlider(CheatToggles.autoCompleteInterval, 0f, 20f), 1);
    }

    private void DrawTracker()
    {
        GUILayout.Label("Tracker", GUIStylePreset.TabSubtitle);

        CheatToggles.endlessTracking = GUILayout.Toggle(CheatToggles.endlessTracking, " Endless Tracking");

        CheatToggles.noTrackingDelay = GUILayout.Toggle(CheatToggles.noTrackingDelay, " No Track Delay");

        CheatToggles.noTrackingCooldown = GUILayout.Toggle(CheatToggles.noTrackingCooldown, " No Track Cooldown");

        CheatToggles.trackReach = GUILayout.Toggle(CheatToggles.trackReach, " Track Reach");
    }

    private void DrawEngineer()
    {
        GUILayout.Label("Engineer", GUIStylePreset.TabSubtitle);

        CheatToggles.endlessVentTime = GUILayout.Toggle(CheatToggles.endlessVentTime, " Endless Vent Time");

        CheatToggles.noVentCooldown = GUILayout.Toggle(CheatToggles.noVentCooldown, " No Vent Cooldown");
    }

    private void DrawScientist()
    {
        GUILayout.Label("Scientist", GUIStylePreset.TabSubtitle);

        CheatToggles.endlessBattery = GUILayout.Toggle(CheatToggles.endlessBattery, " Endless Battery");

        CheatToggles.noVitalsCooldown = GUILayout.Toggle(CheatToggles.noVitalsCooldown, " No Vitals Cooldown");
    }

    private void DrawDetective()
    {
        GUILayout.Label("Detective", GUIStylePreset.TabSubtitle);

        CheatToggles.interrogateReach = GUILayout.Toggle(CheatToggles.interrogateReach, " Interrogate Reach");
    }

    private void DrawGuardianAngel()
    {
        GUILayout.Label("Guardian Angel", GUIStylePreset.TabSubtitle);

        CheatToggles.gaInfiniteRange = GUILayout.Toggle(CheatToggles.gaInfiniteRange, " Infinite Protection Range");

        CheatToggles.gaIgnoreImpostors = GUILayout.Toggle(CheatToggles.gaIgnoreImpostors, " Ignore Impostors");
    }

    private RoleTypes selectedRole = RoleTypes.Crewmate;

    private void DrawChangeRole()
    {
        GUILayout.Label("Change Role", GUIStylePreset.TabSubtitle);

        GUILayout.Label($"Change role to: {selectedRole}");

        var index = System.Array.IndexOf(MalumHost.AssignableRoles, selectedRole);
        if (index < 0) index = 0;
        index = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(index, 0, MalumHost.AssignableRoles.Length - 1)), 0, MalumHost.AssignableRoles.Length - 1);
        selectedRole = MalumHost.AssignableRoles[index];

        if (GUILayout.Button("Apply Role" + (Utils.isHost ? "" : " (Local)"), GUIStylePreset.NormalButton))
            MalumRoles.ChangeRole(selectedRole);
    }
}
