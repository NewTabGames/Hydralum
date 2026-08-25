using UnityEngine;
using Il2CppSystem.Collections.Generic;
using Sentry.Internal.Extensions;

namespace MalumMenu;
public static class PlayerPickMenu
{
    public static ShapeshifterMinigame playerpickMenu;
    public static bool isActive;
    public static NetworkedPlayerInfo targetPlayerData;
    public static Il2CppSystem.Action customAction;
    public static List<NetworkedPlayerInfo> customPlayerList;

    // Get ShapeshifterMenu prefab to instantiate it
    // Found here: https://github.com/AlchlcDvl/TownOfUsReworked/blob/9f3cede9d30bab2c11eb7c960007ab3979f09156/TownOfUsReworked/Custom/Menu.cs
    public static ShapeshifterMinigame GetShapeshifterMenu()
    {
        try
        {
            var rolePrefab = Utils.GetBehaviourByRoleType(AmongUs.GameOptions.RoleTypes.Shapeshifter);
            if (rolePrefab == null || GameData.Instance == null) return null;

            var ssRole = rolePrefab.Cast<ShapeshifterRole>();
            if (ssRole == null || ssRole.ShapeshifterMenu == null) return null;

            var inst = Object.Instantiate(ssRole, GameData.Instance.transform);
            return inst != null ? inst.ShapeshifterMenu : null;
        }
        catch
        {
            return null;
        }
    }

    // Open a PlayerPickMenu to pick a specific player to target
    public static void OpenPlayerPickMenu(List<NetworkedPlayerInfo> playerList, Il2CppSystem.Action action)
    {
        try
        {
            var menuPrefab = GetShapeshifterMenu();
            if (menuPrefab == null || Camera.main == null)
            {
                isActive = false;
                return;
            }

            isActive = true;
            customPlayerList = playerList;
            customAction = action;

            // The menu is based off the shapeshifting menu
            playerpickMenu = Object.Instantiate(menuPrefab, Camera.main.transform, false);
            if (playerpickMenu == null)
            {
                isActive = false;
                return;
            }

            playerpickMenu.transform.localPosition = new Vector3(0f, 0f, -50f);
            playerpickMenu.Begin(null);
        }
        catch
        {
            isActive = false;
        }
    }

    // Returns a custom NetworkedPlayerInfo that can be used as a PPM choice
    public static NetworkedPlayerInfo CustomPPMChoice(string name, NetworkedPlayerInfo.PlayerOutfit outfit, RoleBehaviour role = null)
    {
        NetworkedPlayerInfo customChoice = Object.Instantiate<NetworkedPlayerInfo>(GameData.Instance.PlayerInfoPrefab);

        outfit.PlayerName = name;

        customChoice.Outfits[PlayerOutfitType.Default] = outfit;

        if (!role.IsNull())
        {
            customChoice.Role = role;
        }

        return customChoice;
    }
}
