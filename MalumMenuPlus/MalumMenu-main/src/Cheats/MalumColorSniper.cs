using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

public static class MalumColorSniper
{
    private static float _checkTimer = 0f;

    public static void Update()
    {
        if (!CheatToggles.colorSniper) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer >= 0.25f)
        {
            _checkTimer = 0f;
            TrySnipeColor();
        }
    }

    public static void TrySnipeColor()
    {
        if (!CheatToggles.colorSniper) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmConnected) return;

        var localData = PlayerControl.LocalPlayer.Data;
        if (localData.DefaultOutfit == null) return;

        byte target = CheatToggles.colorSniperTargetColor;

        // If we already have the target color, nothing to do
        if (localData.DefaultOutfit.ColorId == target) return;

        // Check if target color is currently held by someone else in the lobby
        bool isTaken = false;
        var players = PlayerControl.AllPlayerControls;
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p != null && !p.AmOwner && p.Data != null && p.Data.DefaultOutfit != null)
                {
                    if (p.Data.DefaultOutfit.ColorId == target)
                    {
                        isTaken = true;
                        break;
                    }
                }
            }
        }

        // If the color is free, immediately claim it
        if (!isTaken)
        {
            PlayerControl.LocalPlayer.CmdCheckColor(target);
        }
    }
}

// Harmony patch: Whenever any player's color changes, check if our target color was freed
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetColor))]
public static class PlayerControl_RpcSetColor_SniperPatch
{
    public static void Postfix()
    {
        MalumColorSniper.TrySnipeColor();
    }
}

// Harmony patch: Whenever a player leaves the lobby, check if our target color was freed
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy))]
public static class PlayerControl_OnDestroy_SniperPatch
{
    public static void Postfix()
    {
        MalumColorSniper.TrySnipeColor();
    }
}
