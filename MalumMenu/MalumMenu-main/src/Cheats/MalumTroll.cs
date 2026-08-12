using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

// Troll features adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
public static class MalumTroll
{
    // Picks a random player in the lobby (excluding yourself by default), mirroring Hydra's helper.
    public static PlayerControl GetRandomPlayer(bool excludeSelf = true, bool excludeDead = false)
    {
        var valid = new List<PlayerControl>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null) continue;
            if (excludeSelf && player.OwnerId == AmongUsClient.Instance.ClientId) continue;
            if (excludeDead && player.Data.IsDead) continue;

            valid.Add(player);
        }

        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    // Copies a random other player's outfit onto yourself.
    public static void CopyRandomPlayer()
    {
        CopyPlayerOutfit(GetRandomPlayer());
    }

    // Copies a specific player's outfit onto yourself (color/hat/visor/skin/pet/nameplate) via the
    // normal cosmetic RPCs. The name is deliberately skipped - mid-game name changes are rejected by
    // the anticheat, same reason Hydra leaves it out in server-authoritative lobbies.
    public static void CopyPlayerOutfit(PlayerControl target)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || target == null) return;

        try
        {
            var outfit = target.CurrentOutfit;

            localPlayer.CmdCheckColor((byte)outfit.ColorId);
            localPlayer.RpcSetHat(outfit.HatId);
            localPlayer.RpcSetVisor(outfit.VisorId);
            localPlayer.RpcSetSkin(outfit.SkinId);
            localPlayer.RpcSetPet(outfit.PetId);
            localPlayer.RpcSetNamePlate(outfit.NamePlateId);
        }
        catch { }
    }
}
