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

        CheatToggles.reactorSab = GUILayout.Toggle(CheatToggles.reactorSab, " Reactor");

        CheatToggles.oxygenSab = GUILayout.Toggle(CheatToggles.oxygenSab, " Oxygen");

        CheatToggles.elecSab = GUILayout.Toggle(CheatToggles.elecSab, " Lights");

        CheatToggles.unfixableLights = GUILayout.Toggle(CheatToggles.unfixableLights, " Unfixable Lights");

        var newCommsSab = GUILayout.Toggle(CheatToggles.commsSab, " Comms");
        if (newCommsSab && !CheatToggles.commsSab) CheatToggles.unfixableComms = false; // mutually exclusive with Unfixable Comms
        CheatToggles.commsSab = newCommsSab;

        var newUnfixComms = GUILayout.Toggle(CheatToggles.unfixableComms, " Unfixable Comms");
        if (newUnfixComms && !CheatToggles.unfixableComms) CheatToggles.commsSab = false; // mutually exclusive with the Comms sabotage
        CheatToggles.unfixableComms = newUnfixComms;

        CheatToggles.mushSab = GUILayout.Toggle(CheatToggles.mushSab, " Mushroom Mixup");

        CheatToggles.mushSpore = GUILayout.Toggle(CheatToggles.mushSpore, " Trigger Spores");

        CheatToggles.showDoorsMenu = GUILayout.Toggle(CheatToggles.showDoorsMenu, " Show Doors Menu");

        CheatToggles.sabotageMap = GUILayout.Toggle(CheatToggles.sabotageMap, " Open Sabotage Map");

        GUILayout.Space(8);

        CheatToggles.disableSabotage = GUILayout.Toggle(CheatToggles.disableSabotage, " Disable Sabotage");

        if (GUILayout.Button("Sabotage All", GUIStylePreset.NormalButton))
            CheatToggles.sabotageAll = true;

        CheatToggles.sabotageAllDoors = GUILayout.Toggle(CheatToggles.sabotageAllDoors, " Sabotage All: Spam Doors");
    }

    private void DrawVents()
    {
        GUILayout.Label("Vents", GUIStylePreset.TabSubtitle);

        CheatToggles.unlockVents = GUILayout.Toggle(CheatToggles.unlockVents, " Unlock Vents");

        CheatToggles.ventNetwork = GUILayout.Toggle(CheatToggles.ventNetwork, " Vent Network");

        GUILayout.Label("Arrow keys cycle vents (while inside one)", GUIStylePreset.Hint, GUILayout.Width(MenuUI.windowWidth * 0.34f));

        CheatToggles.kickVents = GUILayout.Toggle(CheatToggles.kickVents, " Kick All From Vents");

        CheatToggles.walkInVents = GUILayout.Toggle(CheatToggles.walkInVents, " Walk In Vents");

        CheatToggles.disableVents = GUILayout.Toggle(CheatToggles.disableVents, " Disable Vents");

        CheatToggles.ventsExcludeSelf = GUILayout.Toggle(CheatToggles.ventsExcludeSelf, " Exclude Yourself");
    }
}
