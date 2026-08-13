using UnityEngine;

namespace MalumMenu;

public class PlayersTab : ITab
{
    public string name => "Players";

    // Which player's details are shown on the right. byte.MaxValue = none picked yet (defaults to first).
    private static byte _selectedPlayerId = byte.MaxValue;

    public void Draw()
    {
        var players = PlayerControl.AllPlayerControls;

        if (players == null || players.Count == 0)
        {
            GUILayout.Label("Join a lobby to see players");
            return;
        }

        GUILayout.BeginHorizontal();

        // Left: compact clickable list of names; click one to load its info on the right
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.24f));

        PlayerControl selected = null;
        PlayerControl first = null;

        foreach (var player in players)
        {
            if (player == null || player.Data == null) continue;
            first ??= player;

            var isSelected = player.Data.PlayerId == _selectedPlayerId;
            if (isSelected) selected = player;

            var previous = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.32f, 0.32f, 0.32f);

            var colorHex = ColorUtility.ToHtmlStringRGB(player.Data.Color);
            if (GUILayout.Button($"<color=#{colorHex}>{player.Data.PlayerName}</color>", GUIStylePreset.NormalButton, GUILayout.Height(24)))
            {
                _selectedPlayerId = player.Data.PlayerId;
                selected = player;
            }

            GUI.backgroundColor = previous;
        }

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Right: details for the selected player (fall back to the first if nothing is picked yet)
        GUILayout.BeginVertical();
        DrawDetails(selected ?? first);
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
            GUILayout.Label($"PUID: {Blank(data.Puid)}");

            GUILayout.Space(8);

            // Per-player actions. Each button is always drawn and enabled/disabled via GUI.enabled so
            // the control count stays constant between IMGUI's Layout and Repaint passes as the
            // selection changes. GUI.enabled is reset right after each button, before any risky call,
            // so a disabled state can't leak and grey out the rest of the menu.

            // Teleport onto the player.
            var canTeleport = Utils.isPlayer && !player.AmOwner && !data.Disconnected;
            GUI.enabled = canTeleport;
            var teleportClicked = GUILayout.Button("Teleport to Player", GUIStylePreset.NormalButton);
            GUI.enabled = true;

            if (teleportClicked && canTeleport)
            {
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(player.GetTruePosition());
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

            // Murder the player. Routed through CmdCheckMurder so the existing patch picks the right
            // path: a direct kill as host, or a check-murder request as a non-host impostor.
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
