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

        DrawInfluencer();

        GUILayout.Space(15);

        DrawJudge();

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

        // Lets an Impostor interact with and play task consoles (Console.CanUse -> AllowImpostor). Completing
        // tasks only registers when you're the host; off-host completion stays blocked so the server anticheat
        // can't kick you. See Console_CanUse_ImpostorTasksPatch / PlayerControl_CompleteTask_ImpostorTasks.
        CheatToggles.impostorTasks = GUILayout.Toggle(CheatToggles.impostorTasks, " Allow Tasks");
    }

    private void DrawShapeshifter()
    {
        GUILayout.Label("Shapeshifter", GUIStylePreset.TabSubtitle);

        CheatToggles.noShapeshiftAnim = GUILayout.Toggle(CheatToggles.noShapeshiftAnim, " No Ss Animation");

        CheatToggles.endlessSsDuration = GUILayout.Toggle(CheatToggles.endlessSsDuration, " Endless Ss Duration");

        CheatToggles.noShapeshiftCooldown = GUILayout.Toggle(CheatToggles.noShapeshiftCooldown, " No Ss Cooldown");

        GUILayout.Label("<size=11><color=#888888>This is broken, but is a new kick method. If you shapeshift, unshift, and then try to shift into another user, it will kick them as long as the cooldown is still active. But this only works if you are Shapeshifter. (Works normally as Host)</color></size>");
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

        CheatToggles.noInterrogateCooldown = GUILayout.Toggle(CheatToggles.noInterrogateCooldown, " No Interrogate Cooldown");
    }

    private void DrawInfluencer()
    {
        GUILayout.Label("Influencer", GUIStylePreset.TabSubtitle);

        // "Reach" is KrushMenu's Infinite Message Range (SpiritGuideRole.FindClosestTarget bypass).
        CheatToggles.spiritGuideReach = GUILayout.Toggle(CheatToggles.spiritGuideReach, " Reach");
        CheatToggles.noMessageCooldown = GUILayout.Toggle(CheatToggles.noMessageCooldown, " No Message Cooldown");
        CheatToggles.noSelectionCooldown = GUILayout.Toggle(CheatToggles.noSelectionCooldown, " No Selection Cooldown");
        CheatToggles.noPhotoLimit = GUILayout.Toggle(CheatToggles.noPhotoLimit, " No Photo Limit");
        CheatToggles.noRefreshCooldown = GUILayout.Toggle(CheatToggles.noRefreshCooldown, " No Refresh Cooldown");
    }

    private void DrawGuardianAngel()
    {
        GUILayout.Label("Guardian Angel", GUIStylePreset.TabSubtitle);

        CheatToggles.gaInfiniteRange = GUILayout.Toggle(CheatToggles.gaInfiniteRange, " Infinite Protection Range");

        CheatToggles.gaIgnoreImpostors = GUILayout.Toggle(CheatToggles.gaIgnoreImpostors, " Ignore Impostors");
    }

    private void DrawJudge()
    {
        GUILayout.Label("Judge", GUIStylePreset.TabSubtitle);

        CheatToggles.judgeNoTasks = GUILayout.Toggle(CheatToggles.judgeNoTasks,
            " Enable Judge Overrule <size=11><color=#888888>No Tasks</color></size>");

        CheatToggles.judgeImmune = GUILayout.Toggle(CheatToggles.judgeImmune, " Judge Immune");
    }

    private RoleTypes selectedRole = RoleTypes.Crewmate;

    private void DrawChangeRole()
    {
        GUILayout.Label("Change Role", GUIStylePreset.TabSubtitle);

        // Resolve the display name live from the game (codename -> display name), falling back to the
        // codename if the game can't name it. No per-role hardcoding, so new roles work automatically.
        GUILayout.Label($"Change role to: {Utils.GetRoleDisplayName(selectedRole)}");

        var index = System.Array.IndexOf(MalumHost.AssignableRoles, selectedRole);
        if (index < 0) index = 0;
        index = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(index, 0, MalumHost.AssignableRoles.Length - 1)), 0, MalumHost.AssignableRoles.Length - 1);
        selectedRole = MalumHost.AssignableRoles[index];

        if (GUILayout.Button("Apply Role" + (Utils.isHost ? "" : " (Local)"), GUIStylePreset.NormalButton))
            MalumRoles.ChangeRole(selectedRole);
    }
}
