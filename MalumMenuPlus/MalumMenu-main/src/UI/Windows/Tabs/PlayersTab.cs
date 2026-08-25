using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MalumMenu;

public class PlayersTab : ITab
{
    public string name => "Players";

    // Set of selected player IDs for multi-selection via Ctrl-click
    private static readonly HashSet<byte> _selectedPlayerIds = new();

    public void Draw()
    {
        var players = PlayerControl.AllPlayerControls;

        if (players == null || players.Count == 0)
        {
            GUILayout.Label("Join a lobby to see players");
            return;
        }

        GUILayout.BeginHorizontal();

        // Left: compact clickable list of names (Hold Ctrl to multi-select)
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.26f));

        GUILayout.Label("<size=10><color=#888888>Hold Ctrl to multi-select</color></size>");

        bool isCtrlHeld = (Event.current != null && Event.current.control) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        List<PlayerControl> selectedList = new();
        PlayerControl firstAvailable = null;

        foreach (var player in players)
        {
            if (player == null || player.Data == null) continue;
            firstAvailable ??= player;

            bool isSelected = _selectedPlayerIds.Contains(player.Data.PlayerId);
            if (isSelected) selectedList.Add(player);

            var previous = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);

            var colorHex = ColorUtility.ToHtmlStringRGB(player.Data.Color);
            if (GUILayout.Button($"<color=#{colorHex}>{player.Data.PlayerName}</color>", GUIStylePreset.NormalButton, GUILayout.Height(24)))
            {
                if (isCtrlHeld)
                {
                    if (_selectedPlayerIds.Contains(player.Data.PlayerId))
                    {
                        _selectedPlayerIds.Remove(player.Data.PlayerId);
                    }
                    else
                    {
                        _selectedPlayerIds.Add(player.Data.PlayerId);
                    }
                }
                else
                {
                    _selectedPlayerIds.Clear();
                    _selectedPlayerIds.Add(player.Data.PlayerId);
                }
            }

            GUI.backgroundColor = previous;
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Right: details or multi-player controls
        GUILayout.BeginVertical();

        // Auto-select first if none picked
        if (_selectedPlayerIds.Count == 0 && firstAvailable != null)
        {
            _selectedPlayerIds.Add(firstAvailable.Data.PlayerId);
            selectedList.Add(firstAvailable);
        }

        if (selectedList.Count > 1)
        {
            DrawMultiDetails(selectedList);
        }
        else if (selectedList.Count == 1)
        {
            DrawDetails(selectedList[0]);
        }
        else
        {
            GUILayout.Label("Select a player on the left.\n<color=#888888>(Hold Ctrl to select multiple)</color>", GUIStylePreset.Hint);
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private static void DrawDetails(PlayerControl player)
    {
        try
        {
            if (player == null || player.Data == null)
            {
                GUILayout.Label("Select a player");
                return;
            }

            var data = player.Data;
            var colorHex = ColorUtility.ToHtmlStringRGB(data.Color);

            var tags = "";
            if (player.AmOwner) tags += " <color=#00d0ff>(You)</color>";
            if (IsHost(data)) tags += " <color=#ffcc00>(Host)</color>";
            if (data.Disconnected) tags += " <color=#ff5555>(Disconnected)</color>";
            else if (data.IsDead) tags += " <color=#ff5555>(Dead)</color>";

            GUILayout.Label($"<color=#{colorHex}>{data.PlayerName}</color>{tags}", GUIStylePreset.TabSubtitle);

            GUILayout.Label($"Player ID: {data.PlayerId}      Client ID: {data.ClientId}");
            GUILayout.Label($"Color: {Blank(data.ColorName)}      Level: {data.PlayerLevel + 1}");
            GUILayout.Label($"Platform: {GetPlatform(data)}");

            // Location only exists once in a game (a ship is loaded)
            if (Utils.isShip)
            {
                GUILayout.Label($"Role: {GetRole(data)}");

                var pos = player.GetTruePosition();
                var room = Utils.GetRoomFromPosition(pos);
                var roomName = room != null ? room.RoomId.ToString() : "Unknown";
                GUILayout.Label($"Location: {roomName}   ({pos.x:F1}, {pos.y:F1})");
            }

            GUILayout.Label($"Friend Code: {Blank(data.FriendCode)}");

            GUILayout.Space(8);

            // Teleport onto the player.
            var canTeleport = Utils.isPlayer && !player.AmOwner && !data.Disconnected;
            GUI.enabled = canTeleport;
            var teleportClicked = GUILayout.Button("Teleport to Player", GUIStylePreset.NormalButton);
            GUI.enabled = true;

            if (teleportClicked && canTeleport)
            {
                MalumTeleport.TeleportTo(player.GetTruePosition());
            }

            // Copy this player's outfit onto yourself (client-side cosmetic RPCs).
            var canCopy = Utils.isPlayer && !player.AmOwner;
            GUI.enabled = canCopy;
            var copyClicked = GUILayout.Button("Copy Avatar", GUIStylePreset.NormalButton);
            GUI.enabled = true;

            if (copyClicked && canCopy)
            {
                MalumTroll.CopyPlayerOutfit(player);
            }

            // Restore your original avatar.
            var canRestore = Utils.isPlayer;
            GUI.enabled = canRestore;
            var restoreClicked = GUILayout.Button("Restore Avatar", GUIStylePreset.NormalButton);
            GUI.enabled = true;

            if (restoreClicked && canRestore)
            {
                MalumTroll.RestoreOriginalOutfit();
            }

            // Murder the player.
            var canMurder = Utils.isShip && !player.AmOwner && !data.IsDead && !data.Disconnected;
            GUI.enabled = canMurder;
            var murderClicked = GUILayout.Button("Murder", GUIStylePreset.NormalButton);
            GUI.enabled = true;

            if (murderClicked && canMurder)
            {
                PlayerControl.LocalPlayer.CmdCheckMurder(player);
            }
        }
        catch { }
    }

    private static void DrawMultiDetails(List<PlayerControl> targets)
    {
        try
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{targets.Count} Players Selected</b>", GUIStylePreset.TabSubtitle);
            if (GUILayout.Button("Deselect All", GUIStylePreset.NormalButton, GUILayout.Width(90), GUILayout.Height(22)))
            {
                _selectedPlayerIds.Clear();
                GUILayout.EndHorizontal();
                return;
            }
            GUILayout.EndHorizontal();

            // Render compact player chips
            string playerChips = string.Join(", ", targets.Where(p => p != null && p.Data != null).Select(p => $"<color=#{ColorUtility.ToHtmlStringRGB(p.Data.Color)}>{p.Data.PlayerName}</color>"));
            GUILayout.Label($"Targets: {playerChips}", GUIStylePreset.Hint);

            GUILayout.Space(8);
            GUILayout.Label("Multi-Target Actions", GUIStylePreset.TabSubtitle);

            // Teleport to first target
            var firstTarget = targets.FirstOrDefault(p => p != null && !p.AmOwner && p.Data != null && !p.Data.Disconnected);
            var canTeleport = Utils.isPlayer && firstTarget != null;
            GUI.enabled = canTeleport;
            if (GUILayout.Button($"Teleport to First Selected ({firstTarget?.Data?.PlayerName ?? "None"})", GUIStylePreset.NormalButton) && canTeleport)
            {
                MalumTeleport.TeleportTo(firstTarget.GetTruePosition());
            }
            GUI.enabled = true;

            // Copy avatar from first
            var canCopy = Utils.isPlayer && firstTarget != null;
            GUI.enabled = canCopy;
            if (GUILayout.Button($"Copy Avatar ({firstTarget?.Data?.PlayerName ?? "None"})", GUIStylePreset.NormalButton) && canCopy)
            {
                MalumTroll.CopyPlayerOutfit(firstTarget);
            }
            GUI.enabled = true;

            // Restore avatar
            var canRestore = Utils.isPlayer;
            GUI.enabled = canRestore;
            if (GUILayout.Button("Restore Original Avatar", GUIStylePreset.NormalButton) && canRestore)
            {
                MalumTroll.RestoreOriginalOutfit();
            }
            GUI.enabled = true;

            // Murder all valid selected targets
            var murderableTargets = targets.Where(p => !p.AmOwner && !p.Data.IsDead && !p.Data.Disconnected).ToList();
            var canMurder = Utils.isShip && murderableTargets.Count > 0;
            GUI.enabled = canMurder;
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button($"Murder Selected ({murderableTargets.Count} Players)", GUIStylePreset.NormalButton) && canMurder)
            {
                foreach (var target in murderableTargets)
                {
                    PlayerControl.LocalPlayer.CmdCheckMurder(target);
                }
            }
            GUI.backgroundColor = prevBg;
            GUI.enabled = true;
        }
        catch { }
    }

    private static bool IsHost(NetworkedPlayerInfo data)
    {
        try
        {
            if (Utils.isLocalGame) return false;
            var client = AmongUsClient.Instance.GetClientFromPlayerInfo(data);
            return client != null && client == AmongUsClient.Instance.GetHost();
        }
        catch { return false; }
    }

    private static string GetPlatform(NetworkedPlayerInfo data)
    {
        try
        {
            if (Utils.isLocalGame) return "Local";
            var client = AmongUsClient.Instance.GetClientFromPlayerInfo(data);
            return client != null ? Utils.PlatformTypeToString(client.PlatformData.Platform) : "Unknown";
        }
        catch { return "Unknown"; }
    }

    private static string GetRole(NetworkedPlayerInfo data)
    {
        try { return Utils.GetRoleName(data); }
        catch { return "-"; }
    }

    private static string Blank(string value) => string.IsNullOrEmpty(value) ? "-" : value;
}
