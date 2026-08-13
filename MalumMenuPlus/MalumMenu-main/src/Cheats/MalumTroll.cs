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

    public static NetworkedPlayerInfo.PlayerOutfit OriginalOutfit = null;

    // Copies a specific player's outfit onto yourself (color/hat/visor/skin/pet/nameplate) via the
    // normal cosmetic RPCs. The name is deliberately skipped - mid-game name changes are rejected by
    // the anticheat, same reason Hydra leaves it out in server-authoritative lobbies.
    public static void CopyPlayerOutfit(PlayerControl target)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || target == null) return;

        if (OriginalOutfit == null && localPlayer.CurrentOutfit != null)
        {
            var cur = localPlayer.CurrentOutfit;
            OriginalOutfit = new NetworkedPlayerInfo.PlayerOutfit
            {
                PlayerName = cur.PlayerName,
                ColorId = cur.ColorId,
                HatId = cur.HatId,
                VisorId = cur.VisorId,
                SkinId = cur.SkinId,
                PetId = cur.PetId,
                NamePlateId = cur.NamePlateId,
                HatSequenceId = cur.HatSequenceId,
                VisorSequenceId = cur.VisorSequenceId,
                SkinSequenceId = cur.SkinSequenceId,
                PetSequenceId = cur.PetSequenceId,
                NamePlateSequenceId = cur.NamePlateSequenceId
            };
        }

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

    public static void RestoreOriginalOutfit()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        try
        {
            byte colorId = AmongUs.Data.DataManager.Player.Customization.Color;
            string hatId = AmongUs.Data.DataManager.Player.Customization.Hat;
            string visorId = AmongUs.Data.DataManager.Player.Customization.Visor;
            string skinId = AmongUs.Data.DataManager.Player.Customization.Skin;
            string petId = AmongUs.Data.DataManager.Player.Customization.Pet;
            string namePlateId = AmongUs.Data.DataManager.Player.Customization.NamePlate;

            if (OriginalOutfit != null)
            {
                colorId = (byte)OriginalOutfit.ColorId;
                hatId = OriginalOutfit.HatId;
                visorId = OriginalOutfit.VisorId;
                skinId = OriginalOutfit.SkinId;
                petId = OriginalOutfit.PetId;
                namePlateId = OriginalOutfit.NamePlateId;
            }

            localPlayer.CmdCheckColor(colorId);
            localPlayer.RpcSetHat(hatId);
            localPlayer.RpcSetVisor(visorId);
            localPlayer.RpcSetSkin(skinId);
            localPlayer.RpcSetPet(petId);
            localPlayer.RpcSetNamePlate(namePlateId);
        }
        catch { }
    }
}
