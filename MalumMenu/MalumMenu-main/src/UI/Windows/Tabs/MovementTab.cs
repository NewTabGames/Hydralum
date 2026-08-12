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

        MalumTeleport.UseSnapToRpc = GUILayout.Toggle(MalumTeleport.UseSnapToRpc, " Use SnapTo RPC");
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
