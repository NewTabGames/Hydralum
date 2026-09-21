using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MalumMenu;

public class SchizoTab : ITab
{
    public string name => "Schizo";

    // Selected target IDs (Ctrl-click to multi-select), same pattern as the Players tab.
    private static readonly HashSet<byte> _selectedIds = new();

    public void Draw()
    {
        var players = PlayerControl.AllPlayerControls;

        if (players == null || players.Count == 0)
        {
            GUILayout.Label("Join a lobby to see players");
            return;
        }

        GUILayout.BeginHorizontal();

        // Left: compact clickable list of targets (Hold Ctrl to multi-select)
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.26f));

        GUILayout.Label("<size=10><color=#888888>Hold Ctrl to multi-select</color></size>");

        bool isCtrlHeld = (Event.current != null && Event.current.control) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        List<PlayerControl> selected = new();

        foreach (PlayerControl player in players)
        {
            // Skip yourself and the host - the host is authoritative, so a fake sent to it goes global.
            if (player == null || player.AmOwner || player.Data == null || MalumSchizo.IsHost(player)) continue;

            bool isSelected = _selectedIds.Contains(player.Data.PlayerId);
            if (isSelected) selected.Add(player);

            var previous = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);

            var colorHex = ColorUtility.ToHtmlStringRGB(player.Data.Color);
            if (GUILayout.Button($"<color=#{colorHex}>{player.Data.PlayerName}</color>", GUIStylePreset.NormalButton, GUILayout.Height(24)))
            {
                if (isCtrlHeld)
                {
                    if (!_selectedIds.Remove(player.Data.PlayerId))
                        _selectedIds.Add(player.Data.PlayerId);
                }
                else
                {
                    _selectedIds.Clear();
                    _selectedIds.Add(player.Data.PlayerId);
                }
            }

            GUI.backgroundColor = previous;
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Recompute the selection AFTER the click loop so the right panel reflects the click made THIS frame -
        // building it mid-loop leaves it stale, which showed the wrong (often top) target for a frame and could
        // fire an action at the previously-selected player.
        selected.Clear();
        foreach (PlayerControl player in players)
        {
            if (player == null || player.AmOwner || player.Data == null || MalumSchizo.IsHost(player)) continue;
            if (_selectedIds.Contains(player.Data.PlayerId)) selected.Add(player);
        }

        // Right: actions for the selected target(s)
        GUILayout.BeginVertical();

        DrawActions(selected);

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private static void DrawActions(List<PlayerControl> targets)
    {
        try
        {
            if (targets == null || targets.Count == 0)
            {
                GUILayout.Label("Select a target on the left.\n<color=#888888>(Hold Ctrl to select multiple)</color>", GUIStylePreset.Hint);
                return;
            }

            // Header
            GUILayout.BeginHorizontal();
            if (targets.Count == 1)
            {
                var t = targets[0];
                GUILayout.Label($"Target: <color=#{ColorUtility.ToHtmlStringRGB(t.Data.Color)}>{t.Data.PlayerName}</color>", GUIStylePreset.TabSubtitle);
            }
            else
            {
                GUILayout.Label($"<b>{targets.Count} Targets Selected</b>", GUIStylePreset.TabSubtitle);
                if (GUILayout.Button("Deselect All", GUIStylePreset.NormalButton, GUILayout.Width(90), GUILayout.Height(22)))
                {
                    _selectedIds.Clear();
                    GUILayout.EndHorizontal();
                    return;
                }
            }
            GUILayout.EndHorizontal();

            if (targets.Count > 1)
            {
                string chips = string.Join(", ", targets.Where(p => p != null && p.Data != null)
                    .Select(p => $"<color=#{ColorUtility.ToHtmlStringRGB(p.Data.Color)}>{p.Data.PlayerName}</color>"));
                GUILayout.Label($"Targets: {chips}", GUIStylePreset.Hint);
            }

            GUILayout.Label("Only THEY see this.", GUIStylePreset.Hint);

            bool onShip = Utils.isShip;
            byte mapId = 0;
            try { mapId = GameOptionsManager.Instance.currentGameOptions.MapId; } catch { }

            GUILayout.Space(6);
            GUILayout.Label("Sabotages", GUIStylePreset.TabSubtitle);

            GUI.enabled = onShip;

            if (GUILayout.Button("Reactor / Meltdown", GUIStylePreset.NormalButton))
                foreach (var t in targets) MalumSchizo.FakeReactor(t);

            if (GUILayout.Button("Oxygen", GUIStylePreset.NormalButton))
                foreach (var t in targets) MalumSchizo.FakeOxygen(t);

            if (GUILayout.Button("Comms", GUIStylePreset.NormalButton))
                foreach (var t in targets) MalumSchizo.FakeComms(t);

            if (mapId == 5 && GUILayout.Button("Mushroom Mixup", GUIStylePreset.NormalButton))
                foreach (var t in targets) MalumSchizo.FakeMushroom(t);

            GUILayout.Space(6);
            GUILayout.Label("Doors", GUIStylePreset.TabSubtitle);

            if (GUILayout.Button("Close All Doors", GUIStylePreset.NormalButton))
                foreach (var t in targets) MalumSchizo.CloseAllDoors(t);

            // Specific doors: one button per unique room that has doors
            if (ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null)
            {
                var seen = new HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door == null || !seen.Add(door.Room)) continue;
                    var room = door.Room;
                    if (GUILayout.Button($"Close: {room}", GUIStylePreset.NormalButton))
                        foreach (var t in targets) MalumSchizo.CloseDoor(t, room);
                }
            }

            GUI.enabled = true;
        }
        catch { GUI.enabled = true; }
    }
}
