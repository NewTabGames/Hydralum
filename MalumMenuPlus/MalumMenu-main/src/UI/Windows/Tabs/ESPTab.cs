using UnityEngine;

namespace MalumMenu;

public class ESPTab : ITab
{
    public string name => "ESP";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawCamera();
        
        GUILayout.Space(15);

        DrawHydralumUsers();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        DrawTracers();

        GUILayout.Space(15);

        DrawMinimap();

        GUILayout.Space(15);

        DrawRadar();
        
        GUILayout.Space(15);
        DrawReplay();
        
        GUILayout.Space(15); // Padding for bottom of scroll view

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        
        GUILayout.Space(15); // Extra padding for the whole tab
    }

    private void DrawHydralumUsers()
    {
        GUILayout.Label("Hydralum Users <size=11><color=#888888>(Client-sided)</color></size>", GUIStylePreset.TabSubtitle);

        bool newHideMy = GUILayout.Toggle(CheatToggles.hideMyGem, " Disable My Gem");
        if (newHideMy != CheatToggles.hideMyGem)
        {
            CheatToggles.hideMyGem = newHideMy;
            if (MalumMenu.hideMyGem != null) MalumMenu.hideMyGem.Value = newHideMy;
        }

        bool newHideAll = GUILayout.Toggle(CheatToggles.hideAllGems, " Disable All Gems");
        if (newHideAll != CheatToggles.hideAllGems)
        {
            CheatToggles.hideAllGems = newHideAll;
            if (MalumMenu.hideAllGems != null) MalumMenu.hideAllGems.Value = newHideAll;
        }
    }

    private void DrawGeneral()
    {
        GUILayout.Label("ESP", GUIStylePreset.TabSubtitle);
        
        CheatToggles.seePlayerInfo = GUILayout.Toggle(CheatToggles.seePlayerInfo, " See Player Info");

        CheatToggles.showFriendCode = GUILayout.Toggle(CheatToggles.showFriendCode, " See Friend Code");

        CheatToggles.seeRoles = GUILayout.Toggle(CheatToggles.seeRoles, " See Roles");

        CheatToggles.seeGhosts = GUILayout.Toggle(CheatToggles.seeGhosts, " See Ghosts");

        CheatToggles.noShadows = GUILayout.Toggle(CheatToggles.noShadows, " No Shadows");

        CheatToggles.taskArrows = GUILayout.Toggle(CheatToggles.taskArrows, " Task Arrows");

        CheatToggles.revealVotes = GUILayout.Toggle(CheatToggles.revealVotes, " Reveal Votes");

        CheatToggles.chatColorTags = GUILayout.Toggle(CheatToggles.chatColorTags, " Chat Color Tags");

        CheatToggles.ventEsp = GUILayout.Toggle(CheatToggles.ventEsp, " Vent ESP");

        CheatToggles.killCooldownEsp = GUILayout.Toggle(CheatToggles.killCooldownEsp, " Impostor Kill Cooldown");

        CheatToggles.seeLobbyInfo = GUILayout.Toggle(CheatToggles.seeLobbyInfo, " See Lobby Info");

        CheatToggles.showPing = GUILayout.Toggle(CheatToggles.showPing, " Show Ping");

        CheatToggles.showFps = GUILayout.Toggle(CheatToggles.showFps, " Show FPS");
    }

    private void DrawCamera()
    {
        GUILayout.Label("Camera", GUIStylePreset.TabSubtitle);

        CheatToggles.zoomOut = GUILayout.Toggle(CheatToggles.zoomOut, " Zoom Out");

        CheatToggles.spectate = GUILayout.Toggle(CheatToggles.spectate, " Spectate");

        CheatToggles.freecam = GUILayout.Toggle(CheatToggles.freecam, " Freecam");
    }

    private void DrawTracers()
    {
        GUILayout.Label("Tracers", GUIStylePreset.TabSubtitle);

        CheatToggles.tracersCrew = GUILayout.Toggle(CheatToggles.tracersCrew, " Crewmates");

        CheatToggles.tracersImps = GUILayout.Toggle(CheatToggles.tracersImps, " Impostors");

        CheatToggles.tracersGhosts = GUILayout.Toggle(CheatToggles.tracersGhosts, " Ghosts");

        CheatToggles.tracersBodies = GUILayout.Toggle(CheatToggles.tracersBodies, " Dead Bodies");

        CheatToggles.colorBasedTracers = GUILayout.Toggle(CheatToggles.colorBasedTracers, " Color-based");

        CheatToggles.distanceBasedTracers = GUILayout.Toggle(CheatToggles.distanceBasedTracers, " Distance-based");
    }

    private void DrawMinimap()
    {
        GUILayout.Label("Minimap", GUIStylePreset.TabSubtitle);

        CheatToggles.mapCrew = GUILayout.Toggle(CheatToggles.mapCrew, " Crewmates");

        CheatToggles.mapImps = GUILayout.Toggle(CheatToggles.mapImps, " Impostors");

        CheatToggles.mapGhosts = GUILayout.Toggle(CheatToggles.mapGhosts, " Ghosts");

        CheatToggles.colorBasedMap = GUILayout.Toggle(CheatToggles.colorBasedMap, " Color-based");
    }

    private void DrawRadar()
    {
        GUILayout.Label("Radar", GUIStylePreset.TabSubtitle);

        CheatToggles.radar = GUILayout.Toggle(CheatToggles.radar, " Show Radar");
        CheatToggles.radarBodies = GUILayout.Toggle(CheatToggles.radarBodies, " Bodies on Radar");
        CheatToggles.radarGhosts = GUILayout.Toggle(CheatToggles.radarGhosts, " Ghosts on Radar");
        CheatToggles.radarDoors = GUILayout.Toggle(CheatToggles.radarDoors, " Doors on Radar");

        GUILayout.Label("<size=11><color=#888888>Right-click radar = teleport. Door: click=shut, double-click=pin, click=unpin.</color></size>");

        GUILayout.Label($"Size: {CheatToggles.radarSize}%");
        CheatToggles.radarSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.radarSize, 60f, 180f));

        GUILayout.Label($"Opacity: {CheatToggles.radarOpacity}%");
        CheatToggles.radarOpacity = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.radarOpacity, 30f, 100f));
    }

    private void DrawReplay()
    {
        GUILayout.Label("Replay Console", GUIStylePreset.TabSubtitle);

        CheatToggles.replay = GUILayout.Toggle(CheatToggles.replay, " Show Replay");
        CheatToggles.replayRecording = GUILayout.Toggle(CheatToggles.replayRecording, " Record");
        CheatToggles.replayClearAfterMeeting = GUILayout.Toggle(CheatToggles.replayClearAfterMeeting, " Clear After Meeting");

        GUILayout.Label($"Size: {CheatToggles.replaySize}%");
        CheatToggles.replaySize = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.replaySize, 60f, 180f));

        GUILayout.Label($"Opacity: {CheatToggles.replayOpacity}%");
        CheatToggles.replayOpacity = Mathf.RoundToInt(GUILayout.HorizontalSlider(CheatToggles.replayOpacity, 30f, 100f));
    }
}
