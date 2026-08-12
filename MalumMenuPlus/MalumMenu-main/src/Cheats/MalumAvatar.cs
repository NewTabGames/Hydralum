using System.Collections.Generic;
using AmongUs.Data;
using UnityEngine;

namespace MalumMenu;

// Avatar Controls, adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
public static class MalumAvatar
{
    // Picks a color id not currently used by anyone in the lobby (falls back to any of the 18 base
    // colors if they are all taken, e.g. modded lobbies with more than 18 players).
    public static int GetRandomUnusedColor()
    {
        var colors = new List<int>();
        for (var i = 0; i < 18; i++) colors.Add(i);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null)
                colors.Remove(player.Data.DefaultOutfit.ColorId);
        }

        return colors.Count > 0 ? colors[Random.Range(0, colors.Count)] : Random.Range(0, 18);
    }

    public static void RandomizeColor()
    {
        if (PlayerControl.LocalPlayer == null) return;

        PlayerControl.LocalPlayer.CmdCheckColor((byte)GetRandomUnusedColor());
    }

    // Random color plus a random hat / visor / skin / pet, applied through the normal cosmetic RPCs.
    // Reads the backing cosmetic arrays directly (the AllHats/... properties are IReadOnlyList with
    // no interop indexer), same as Hydra does.
    public static void RandomizeAvatar()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var manager = HatManager.Instance;
        if (localPlayer == null || manager == null) return;

        try
        {
            localPlayer.CmdCheckColor((byte)GetRandomUnusedColor());

            var hats = manager.allHats;
            var visors = manager.allVisors;
            var skins = manager.allSkins;
            var pets = manager.allPets;

            if (hats != null && hats.Length > 0) localPlayer.RpcSetHat(hats[Random.Range(0, hats.Length)].ProductId);
            if (visors != null && visors.Length > 0) localPlayer.RpcSetVisor(visors[Random.Range(0, visors.Length)].ProductId);
            if (skins != null && skins.Length > 0) localPlayer.RpcSetSkin(skins[Random.Range(0, skins.Length)].ProductId);
            if (pets != null && pets.Length > 0) localPlayer.RpcSetPet(pets[Random.Range(0, pets.Length)].ProductId);
        }
        catch { }
    }

    // Restores the cosmetics saved on the account (your real color/hat/visor/skin/pet)
    public static void RestoreAvatar()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        try
        {
            var customization = DataManager.Player.Customization;

            localPlayer.CmdCheckColor(customization.Color);
            localPlayer.RpcSetHat(customization.Hat);
            localPlayer.RpcSetVisor(customization.Visor);
            localPlayer.RpcSetSkin(customization.Skin);
            localPlayer.RpcSetPet(customization.Pet);
        }
        catch { }
    }
}
