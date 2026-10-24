using UnityEngine;

namespace MalumMenu;

public class ShipTab : ITab
{
    public string name => "Ship";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawSabotage();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        DrawVents();

        GUILayout.Space(15);

        DrawAnimations();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawAnimations()
    {
        GUILayout.Label("Animations", GUIStylePreset.TabSubtitle);

        CheatToggles.animShields = GUILayout.Toggle(CheatToggles.animShields, " Shields");

        CheatToggles.animAsteroids = GUILayout.Toggle(CheatToggles.animAsteroids, " Asteroids");

        CheatToggles.animEmptyGarbage = GUILayout.Toggle(CheatToggles.animEmptyGarbage, " Empty Garbage");

        CheatToggles.animMedScan = GUILayout.Toggle(CheatToggles.animMedScan, " Medbay Scan");

        CheatToggles.enableMedScan = GUILayout.Toggle(CheatToggles.enableMedScan, " Enable Med Scan");

        CheatToggles.animCamsInUse = GUILayout.Toggle(CheatToggles.animCamsInUse, " Cams In Use");

        CheatToggles.animPet = GUILayout.Toggle(CheatToggles.animPet, " Pet Animation");

        CheatToggles.moonWalk = GUILayout.Toggle(CheatToggles.moonWalk, " Moonwalk (Client-Sided)");
    }

    private void DrawGeneral()
    {
        GUILayout.Label("General", GUIStylePreset.TabSubtitle);

        CheatToggles.callMeeting = GUILayout.Toggle(CheatToggles.callMeeting, " Call Meeting");

        CheatToggles.closeMeeting = GUILayout.Toggle(CheatToggles.closeMeeting, " Close Meeting");

        CheatToggles.autoReportBodies = GUILayout.Toggle(CheatToggles.autoReportBodies, " Auto-Report Dead Bodies");

        CheatToggles.autoOpenDoorsOnUse = GUILayout.Toggle(CheatToggles.autoOpenDoorsOnUse, " Auto-Open Doors On Use");
    }

    private void DrawSabotage()
    {
        GUILayout.Label("Sabotage", GUIStylePreset.TabSubtitle);

        // The sabotage options now live in their own pop-up window (SabotageUI), like the Doors menu.
        CheatToggles.showSabotageMenu = GUILayout.Toggle(CheatToggles.showSabotageMenu, " Show Sabotage Menu");
    }

    private void DrawVents()
    {
        GUILayout.Label("Vents", GUIStylePreset.TabSubtitle);

        CheatToggles.unlockVents = GUILayout.Toggle(CheatToggles.unlockVents, " Unlock Vents");

        CheatToggles.ventNetwork = GUILayout.Toggle(CheatToggles.ventNetwork, " Vent Network");

        GUILayout.Label("Vent arrows connect every vent; arrow keys cycle, or click any vent to enter / go to it", GUIStylePreset.Hint);

        if (CheatToggles.ventNetwork)
            CheatToggles.ventNetworkHideLines = GUILayout.Toggle(CheatToggles.ventNetworkHideLines, " Hide Guide Lines");

        CheatToggles.ventRange = GUILayout.Toggle(CheatToggles.ventRange, " Vent Interaction Range");

        if (CheatToggles.ventRange)
        {
            GUILayout.Label($"Range: {CheatToggles.ventRangeMult:0.0}x normal", GUIStylePreset.Hint);
            CheatToggles.ventRangeMult = (float)System.Math.Round(
                GUILayout.HorizontalSlider(CheatToggles.ventRangeMult, 1f, 8f), 1);
        }

        CheatToggles.kickVents = GUILayout.Toggle(CheatToggles.kickVents, " Kick All From Vents");

        CheatToggles.disableVents = GUILayout.Toggle(CheatToggles.disableVents, " Disable Vents");

        CheatToggles.ventsExcludeSelf = GUILayout.Toggle(CheatToggles.ventsExcludeSelf, " Exclude Yourself");
    }
}
