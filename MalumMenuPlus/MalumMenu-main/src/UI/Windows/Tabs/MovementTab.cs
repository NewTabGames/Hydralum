using UnityEngine;
using System;

namespace MalumMenu;

public class MovementTab : ITab
{
    public string name => "Movement";

    public void Draw()
    {
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawGeneral();

        GUILayout.Space(15);

        DrawSpeed();

        GUILayout.Space(15);

        DrawTeleport();

        GUILayout.Space(15);

        DrawHandAnimations();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        DrawTeleportLocations();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawGeneral()
    {
        CheatToggles.noClip = GUILayout.Toggle(CheatToggles.noClip, " NoClip");

        CheatToggles.invertControls = GUILayout.Toggle(CheatToggles.invertControls, " Invert Controls");
    }

    private void DrawSpeed()
    {
        GUILayout.Label("Speed", GUIStylePreset.TabSubtitle);

        try
        {
            // Accessing these throws (and we fall to the catch) when there's no spawned
            // player yet, e.g. in the main menu.
            var local = PlayerControl.LocalPlayer;
            var physics = local.MyPhysics;
            var isGhost = local.Data.IsDead;

            if (isGhost)
                physics.GhostSpeed = GUILayout.HorizontalSlider(physics.GhostSpeed, 0f, 20f, GUILayout.Width(250f));
            else
                physics.Speed = GUILayout.HorizontalSlider(physics.Speed, 0f, 20f, GUILayout.Width(250f));

            // Snaps back to the exact default when you drag close to it
            Utils.SnapSpeedToDefault(0.05f, isGhost);

            var current = isGhost ? physics.GhostSpeed : physics.Speed;
            GUILayout.Label($"Current Speed: {current:F1} {(Utils.IsSpeedDefault(isGhost) ? "(Default)" : "")}");

            if (GUILayout.Button("Reset to Default", GUIStylePreset.NormalButton, GUILayout.Width(150f)))
            {
                if (isGhost)
                    physics.GhostSpeed = Utils.DefaultGhostSpeed;
                else
                    physics.Speed = Utils.DefaultSpeed;
            }
        }
        catch (NullReferenceException)
        {
            GUILayout.Label("Join a game to adjust your speed.");
        }
    }

    private void DrawTeleport()
    {
        GUILayout.Label("Teleport", GUIStylePreset.TabSubtitle);

        CheatToggles.teleportCursor = GUILayout.Toggle(CheatToggles.teleportCursor, " to Cursor");

        CheatToggles.teleportPlayer = GUILayout.Toggle(CheatToggles.teleportPlayer, " to Player");

        CheatToggles.useSnapToRpc = GUILayout.Toggle(CheatToggles.useSnapToRpc, " Use SnapTo RPC");
    }

    private static readonly string[] HandPatterns = new string[]
    {
        "Goon", "Orbit", "Figure 8", "Halo",
        "Wave", "Head Pat", "Shield", "Heart",
        "Boomerang", "Barrage", "Spiral", "Tornado",
        "Foot Tickle"
    };

    private void DrawHandAnimations()
    {
        GUILayout.Label("Hand Animations", GUIStylePreset.TabSubtitle);

        CheatToggles.handAnimEnabled = GUILayout.Toggle(CheatToggles.handAnimEnabled, " Enable Hand Animation");

        GUILayout.Space(4);
        GUILayout.Label("Target:", GUIStylePreset.Hint);
        
        var prevBgGrid = GUI.backgroundColor;
        GUILayout.BeginHorizontal();
        int pIndex = 0;
        
        bool isSelf = CheatToggles.handAnimTargetId == 255;
        GUI.backgroundColor = isSelf ? new Color(0.2f, 0.85f, 0.5f) : prevBgGrid;
        if (GUILayout.Button("Self", GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            CheatToggles.handAnimTargetId = 255;
        }
        pIndex++;

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.Disconnected || p.AmOwner) continue;

            if (pIndex % 4 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            bool isTarget = CheatToggles.handAnimTargetId == p.PlayerId;
            GUI.backgroundColor = isTarget ? new Color(0.2f, 0.85f, 0.5f) : prevBgGrid;
            
            string pName = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(p.Data.Color)}>{p.Data.PlayerName}</color>";
            if (GUILayout.Button(pName, GUIStylePreset.NormalButton, GUILayout.Height(22)))
            {
                CheatToggles.handAnimTargetId = p.PlayerId;
            }
            pIndex++;
        }
        GUILayout.EndHorizontal();
        GUI.backgroundColor = prevBgGrid;

        GUILayout.Space(4);
        int currentPattern = Mathf.Clamp(CheatToggles.handAnimPattern, 0, HandPatterns.Length - 1);
        GUILayout.Label($"Pattern: {HandPatterns[currentPattern]}", GUIStylePreset.Hint);

        var prevBg = GUI.backgroundColor;
        for (int i = 0; i < HandPatterns.Length; i++)
        {
            if (i % 4 == 0)
            {
                if (i > 0) GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }

            bool isSelected = CheatToggles.handAnimPattern == i;
            GUI.backgroundColor = isSelected ? new Color(0.2f, 0.85f, 0.5f) : prevBg;
            if (GUILayout.Button(HandPatterns[i], GUIStylePreset.NormalButton, GUILayout.Height(22)))
            {
                CheatToggles.handAnimPattern = i;
            }
        }
        GUILayout.EndHorizontal();
        GUI.backgroundColor = prevBg;

        GUILayout.Space(4);
        GUILayout.Label($"Speed: {CheatToggles.handAnimSpeed:F1}x");
        CheatToggles.handAnimSpeed = GUILayout.HorizontalSlider(CheatToggles.handAnimSpeed, 0.5f, 8.0f, GUILayout.Width(250f));

        GUILayout.Label($"Radius: {CheatToggles.handAnimRadius:F1}");
        CheatToggles.handAnimRadius = GUILayout.HorizontalSlider(CheatToggles.handAnimRadius, 0.2f, 3.5f, GUILayout.Width(250f));

        GUILayout.Space(6);
        GUILayout.Label("Note: Visible to all players (serversided), but network latency causes faster speeds to look choppy/unrecognizable on other screens. Recommended speed: 0.8x - 1.3x.", GUIStylePreset.Hint);
    }

    private void DrawTeleportLocations()
    {
        GUILayout.Label("Teleport to Location", GUIStylePreset.TabSubtitle);

        if (!Utils.isPlayer)
        {
            GUILayout.Label("Join a game to teleport.", GUIStylePreset.Hint);
            return;
        }

        // Two buttons per row. The location set is stable within a frame (it only changes with the
        // map), so the control count stays constant between the Layout and Repaint passes.
        var locations = MalumTeleport.GetTeleportLocations();
        var column = 0;

        foreach (var location in locations)
        {
            if (column == 0) GUILayout.BeginHorizontal();

            if (GUILayout.Button(location.Key, GUIStylePreset.NormalButton))
                MalumTeleport.TeleportTo(location.Value);

            column++;

            if (column == 2)
            {
                GUILayout.EndHorizontal();
                column = 0;
            }
        }

        // Close a dangling half-row when a map has an odd number of locations.
        if (column != 0) GUILayout.EndHorizontal();
    }
}
