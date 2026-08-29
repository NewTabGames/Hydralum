using HarmonyLib;
using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using UnityEngine;
using System;
using System.Security.Cryptography;
using InnerNet;
using System.Collections.Generic;

namespace MalumMenu;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetPlatformData))]
public static class Constants_GetPlatformData
{
    // Postfix patch of Constants.GetPlatformData to spoof the user's platform type
    public static void Postfix(ref PlatformSpecificData __result)
    {
        if (Utils.StringToPlatformType(MalumMenu.spoofPlatform.Value, out Platforms? platformType))
        {
            __result = new PlatformSpecificData
            {
                Platform = (Platforms)platformType,
                PlatformName = Constants.GetPlatformName()
            };
        }
    }
}

[HarmonyPatch(typeof(AmongUs.InnerNet.GameDataMessages.RpcSetLevelMessage), nameof(AmongUs.InnerNet.GameDataMessages.RpcSetLevelMessage.SerializeRpcValues))]
public static class RpcSetLevelMessage_SerializeRpcValues
{
    // Prefix patch of RpcSetLevelMessage.SerializeRpcValues to spoof player level from Malum config
    public static bool Prefix(Hazel.MessageWriter msg)
    {
        if (uint.TryParse(MalumMenu.spoofLevel.Value, out uint level) && level >= 1 && level <= 100001)
        {
            msg.WritePacked(level - 1);
            if (PlayerControl.LocalPlayer != null)
            {
                PlayerControl.LocalPlayer.SetLevel(level - 1);
            }
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
public static class PlayerControl_Start_LevelSpoof
{
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != null && __instance.AmOwner)
        {
            if (uint.TryParse(MalumMenu.spoofLevel.Value, out uint level) && level >= 1 && level <= 100001)
            {
                __instance.SetLevel(level - 1);
                if (__instance.Data != null)
                {
                    __instance.Data.PlayerLevel = level - 1;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
public static class FreeChatInputField_UpdateCharCount
{
    // Postfix patch of FreeChatInputField.UpdateCharCount to change how charCountText displays
    public static void Postfix(FreeChatInputField __instance)
    {
        // Only works if CheatToggles.longerMsgs is enabled
        if (!CheatToggles.longerMessages || __instance == null || __instance.textArea == null || __instance.textArea.text == null || __instance.charCountText == null) return;

        // Update charCountText to account for longer characterLimit
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");

        if (length < 90) // Under 75%
        {
            __instance.charCountText.color = Color.black;
        }
        else if (length < 120) // Under 100%
        {
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        }
        else // Over or equal to 100%
        {
            __instance.charCountText.color = Color.red;
        }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
public static class ChatBubble_SetName
{
    public static void Postfix(ChatBubble __instance)
	{
        MalumESP.ChatNametags(__instance);
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetColorblindText))]
public static class ChatBubble_SetColorblindText
{
    public static void Postfix(ChatBubble __instance)
    {
        MalumESP.UpdateChatBubbleColorTag(__instance);
    }
}


[HarmonyPatch(typeof(SystemInfo), nameof(SystemInfo.deviceUniqueIdentifier), MethodType.Getter)]
public static class SystemInfo_deviceUniqueIdentifier_Getter
{
    private static string _cachedSpoofedId;

    // Postfix patch of SystemInfo.deviceUniqueIdentifier Getter method
    // Made to hide the user's real unique deviceId by generating a random fake one
    public static void Postfix(ref string __result)
    {
        if (!MalumMenu.spoofDeviceId.Value) return;

        if (string.IsNullOrEmpty(_cachedSpoofedId))
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            _cachedSpoofedId = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        __result = _cachedSpoofedId;
    }
}

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
public static class DisconnectPopup_DoShow
{
    // Postfix patch of DisconnectPopup.DoShow to copy lobby code to clipboard on disconnect
    public static void Postfix(DisconnectPopup __instance)
    {
        if (!CheatToggles.copyLobbyCodeOnDisconnect) return;

        GUIUtility.systemCopyBuffer = AmongUsClient_OnGameJoined.lastGameIdString;

        if (__instance._textArea != null)
        {
            __instance.SetText(__instance._textArea.text + "\n\n<size=60%>Lobby code has been copied to the clipboard</size>");
        }
    }
}

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
public static class PlayerBanData_BanMinutesLeft_Getter
{
    // Postfix patch of PlayerBanData.BanMinutesLeft Getter method to remove disconnect penalty
    public static void Postfix(PlayerBanData __instance, ref int __result)
    {
        if (!CheatToggles.avoidPenalties) return;

        __instance.BanPoints = 0f; // Removes all BanPoints
        __result = 0; // Removes all BanMinutes
    }
}

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
public static class FullAccount_CanSetCustomName
{
    // Prefix patch of FullAccount.CanSetCustomName to allow the usage of custom names
    public static void Prefix(ref bool canSetName)
    {
        if (CheatToggles.unlockFeatures)
        {
            canSetName = true;
        }
    }
}

[HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
public static class AccountManager_CanPlayOnline
{
    // Prefix patch of AccountManager.CanPlayOnline to allow online games
    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.JoinGame))]
public static class InnerNetClient_JoinGame
{
    // Prefix patch of InnerNetClient.JoinGame to allow online games
    public static void Prefix()
    {
        if (CheatToggles.unlockFeatures && DataManager.Player?.Account != null)
        {
            DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.LoggedIn;
        }
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
public static class GameManager_CheckTaskCompletion
{
    // Prefix patch of GameManager.CheckTaskCompletion to prevent a running game from ending
    public static bool Prefix(ref bool __result)
    {
        if (!CheatToggles.noGameEnd) return true;

        __result = false;

        return false;
    }
}

[HarmonyPatch(typeof(Mushroom), nameof(Mushroom.FixedUpdate))]
public static class Mushroom_FixedUpdate
{
    public static void Postfix(Mushroom __instance)
    {
        MalumESP.SporeCloudVision(__instance);
    }
}

// Found here: https://github.com/g0aty/SickoMenu/blob/main/hooks/PlainDoor.cpp
[HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
public static class DoorBreakerGame_Start
{
    // Prefix patch of DoorBreakerGame.Start to automatically open a door when the player interacts with it
    public static bool Prefix(DoorBreakerGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}

// Found here: https://github.com/g0aty/SickoMenu/blob/main/hooks/PlainDoor.cpp
[HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
public static class DoorCardSwipeGame_Begin
{
    // Prefix patch of DoorCardSwipeGame.Begin to automatically open a door when the player interacts with it
    public static bool Prefix(DoorCardSwipeGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}

// Found here: https://github.com/g0aty/SickoMenu/blob/main/hooks/PlainDoor.cpp
[HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
public static class MushroomDoorSabotageMinigame_Begin
{
    // Prefix patch of MushroomDoorSabotageMinigame.Begin to automatically open a door when the player interacts with it
    public static bool Prefix(MushroomDoorSabotageMinigame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        __instance.FixDoorAndCloseMinigame();

        return false;
    }
}

[HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
public static class Console_CanUse_MedScanPatch
{
    public static void Postfix(Console __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (!CheatToggles.enableMedScan) return;
        if (PlayerControl.LocalPlayer == null || pc != PlayerControl.LocalPlayer.Data) return;
        if (PlayerControl.LocalPlayer.Data.IsDead) return;
        if (__instance == null || __instance.TaskTypes == null) return;

        bool isMedScan = false;
        for (int i = 0; i < __instance.TaskTypes.Length; i++)
        {
            if (__instance.TaskTypes[i] == TaskTypes.SubmitScan)
            {
                isMedScan = true;
                break;
            }
        }

        if (isMedScan)
        {
            Vector2 playerPos = PlayerControl.LocalPlayer.GetTruePosition();
            Vector2 consolePos = __instance.transform.position;
            float dist = Vector2.Distance(playerPos, consolePos);
            couldUse = true;
            if (dist <= __instance.UsableDistance)
            {
                canUse = true;
                __result = dist;
            }
        }
    }
}

[HarmonyPatch(typeof(Console), nameof(Console.Use))]
public static class Console_Use_MedScanPatch
{
    public static bool Prefix(Console __instance)
    {
        if (!CheatToggles.enableMedScan) return true;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return true;
        if (__instance == null || __instance.TaskTypes == null) return true;

        bool isMedScan = false;
        for (int i = 0; i < __instance.TaskTypes.Length; i++)
        {
            if (__instance.TaskTypes[i] == TaskTypes.SubmitScan)
            {
                isMedScan = true;
                break;
            }
        }

        if (isMedScan)
        {
            // If the local player already has a real SubmitScan task assigned and not completed, let vanilla handle it
            PlayerTask existingTask = __instance.FindTask(PlayerControl.LocalPlayer);
            if (existingTask != null && existingTask.TaskType == TaskTypes.SubmitScan && !existingTask.IsComplete)
            {
                return true;
            }

            if (Minigame.Instance != null) return false;

            PlayerTask scanTaskPrefab = FindScanTaskPrefab();

            if (scanTaskPrefab != null && scanTaskPrefab.MinigamePrefab != null)
            {
                var normTask = scanTaskPrefab.TryCast<NormalPlayerTask>();
                if (normTask != null)
                {
                    normTask.taskStep = 0;
                }

                var minigame = UnityEngine.Object.Instantiate<Minigame>(scanTaskPrefab.MinigamePrefab);
                minigame.transform.SetParent(Camera.main.transform, false);
                minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
                minigame.Console = __instance;
                minigame.Begin(scanTaskPrefab);
            }
            else
            {
                Utils.ForceSetScanner(PlayerControl.LocalPlayer, true);
            }

            return false;
        }

        return true;
    }

    private static PlayerTask FindScanTaskPrefab()
    {
        if (ShipStatus.Instance == null) return null;

        if (ShipStatus.Instance.SpecialTasks != null)
        {
            for (int i = 0; i < ShipStatus.Instance.SpecialTasks.Count; i++)
            {
                var task = ShipStatus.Instance.SpecialTasks[i];
                if (task != null && task.TaskType == TaskTypes.SubmitScan) return task;
            }
        }

        if (ShipStatus.Instance.LongTasks != null)
        {
            for (int i = 0; i < ShipStatus.Instance.LongTasks.Count; i++)
            {
                var task = ShipStatus.Instance.LongTasks[i];
                if (task != null && task.TaskType == TaskTypes.SubmitScan) return task;
            }
        }

        if (ShipStatus.Instance.CommonTasks != null)
        {
            for (int i = 0; i < ShipStatus.Instance.CommonTasks.Count; i++)
            {
                var task = ShipStatus.Instance.CommonTasks[i];
                if (task != null && task.TaskType == TaskTypes.SubmitScan) return task;
            }
        }

        if (ShipStatus.Instance.ShortTasks != null)
        {
            for (int i = 0; i < ShipStatus.Instance.ShortTasks.Count; i++)
            {
                var task = ShipStatus.Instance.ShortTasks[i];
                if (task != null && task.TaskType == TaskTypes.SubmitScan) return task;
            }
        }

        var allTasks = ShipStatus.Instance.GetAllTasks();
        if (allTasks != null)
        {
            for (int i = 0; i < allTasks.Count; i++)
            {
                var task = allTasks[i];
                if (task != null && task.TaskType == TaskTypes.SubmitScan) return task;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.Begin))]
public static class MedScanMinigame_Begin_Patch
{
    public static void Postfix(MedScanMinigame __instance)
    {
        if (!CheatToggles.enableMedScan) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
        if (__instance == null) return;

        // For fake scans, __instance.medscan might be null because the prefab wasn't initialized with a real MedScanSystem console.
        // We MUST assign it to prevent WalkToOffset from throwing a NullReferenceException and getting the player stuck.
        if (__instance.medscan == null && ShipStatus.Instance != null && ShipStatus.Instance.Systems != null)
        {
            if (ShipStatus.Instance.Systems.ContainsKey(SystemTypes.MedBay))
            {
                __instance.medscan = ShipStatus.Instance.Systems[SystemTypes.MedBay].TryCast<MedScanSystem>();
            }
        }

        bool hasRealTask = false;
        if (PlayerControl.LocalPlayer.myTasks != null)
        {
            foreach (var t in PlayerControl.LocalPlayer.myTasks)
            {
                if (t != null && t.TaskType == TaskTypes.SubmitScan && !t.IsComplete)
                {
                    if (t == __instance.MyTask)
                    {
                        hasRealTask = true;
                        break;
                    }
                }
            }
        }

        if (!hasRealTask && __instance.MyNormTask != null)
        {
            __instance.MyNormTask.taskStep = 0;
        }
    }
}

[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.FixedUpdate))]
public static class MedScanMinigame_FixedUpdate_Patch
{
    public static bool Prefix(MedScanMinigame __instance)
    {
        if (!CheatToggles.enableMedScan) return true;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return true;
        if (__instance == null) return true;

        bool hasRealTask = false;
        if (PlayerControl.LocalPlayer.myTasks != null)
        {
            foreach (var t in PlayerControl.LocalPlayer.myTasks)
            {
                if (t != null && t.TaskType == TaskTypes.SubmitScan && !t.IsComplete)
                {
                    if (t == __instance.MyTask)
                    {
                        hasRealTask = true;
                        break;
                    }
                }
            }
        }

        if (!hasRealTask)
        {
            if (__instance.medscan != null && __instance.medscan.CurrentUser != PlayerControl.LocalPlayer.PlayerId)
            {
                __instance.medscan.CurrentUser = PlayerControl.LocalPlayer.PlayerId;
            }

            if (__instance.medscan != null && __instance.medscan.CurrentUser == PlayerControl.LocalPlayer.PlayerId)
            {
                if (__instance.ScanTimer == 0f)
                {
                    // Allow the normal RPC to be sent so other players see the visual rings!
                    PlayerControl.LocalPlayer.RpcSetScanner(true);
                    PlayerControl.LocalPlayer.SetScanner(true, 0); // ensure local applies
                    __instance.ScanTimer += 0.0001f;
                }

                if (__instance.ScanTimer + Time.fixedDeltaTime >= 10f)
                {
                    // Nullify tasks so vanilla Close() doesn't call NextStep()
                    __instance.MyTask = null;
                    __instance.MyNormTask = null;
                    
                    __instance.Close(); // Call vanilla Close() to properly unfreeze the UI!
                    
                    return false; // Skip original FixedUpdate only on the final frame
                }
                
                return true; // Let original FixedUpdate run to animate the UI
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.Close))]
public static class MedScanMinigame_Close_Patch
{
    public static void Prefix(MedScanMinigame __instance)
    {
        if (!CheatToggles.enableMedScan) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
        if (__instance == null) return;

        bool hasRealTask = false;
        if (PlayerControl.LocalPlayer.myTasks != null)
        {
            foreach (var t in PlayerControl.LocalPlayer.myTasks)
            {
                if (t != null && t.TaskType == TaskTypes.SubmitScan && !t.IsComplete)
                {
                    if (t == __instance.MyTask)
                    {
                        hasRealTask = true;
                        break;
                    }
                }
            }
        }

        if (!hasRealTask)
        {
            // Nullify tasks before vanilla Close() runs so it skips NextStep()
            __instance.MyTask = null;
            __instance.MyNormTask = null;
            // We allow vanilla Close() to run completely so it cleans up the UI and unfreezes the player!
            // RpcUpdateSystem is blocked by our ShipStatus_RpcUpdateSystem_Patch.
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcCompleteTask))]
public static class PlayerControl_RpcCompleteTask_Patch
{
    public static bool Prefix(PlayerControl __instance, uint idx)
    {
        if (__instance != PlayerControl.LocalPlayer) return true;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return true;

        // If local player is an Impostor, never send RpcCompleteTask (instant kick by server anticheat)
        if (PlayerControl.LocalPlayer.Data.Role != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            return false;
        }

        // Fix logic bug: allow valid tasks to be completed, block if tasks is null or index is out of bounds
        if (PlayerControl.LocalPlayer.myTasks == null || idx >= PlayerControl.LocalPlayer.myTasks.Count)
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcUpdateSystem), typeof(SystemTypes), typeof(byte))]
public static class ShipStatus_RpcUpdateSystem_Patch
{
    public static bool Prefix(SystemTypes systemType, byte amount)
    {
        if (!CheatToggles.enableMedScan) return true;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return true;
        if (systemType != SystemTypes.MedBay) return true;

        // Block if Impostor (anticheat immediately kicks Impostors sending system updates for Medbay)
        if (PlayerControl.LocalPlayer.Data.Role != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            return false;
        }

        // Check if player has an actual uncompleted Medbay scan task
        bool hasRealTask = false;
        if (PlayerControl.LocalPlayer.myTasks != null)
        {
            for (int i = 0; i < PlayerControl.LocalPlayer.myTasks.Count; i++)
            {
                var t = PlayerControl.LocalPlayer.myTasks[i];
                if (t != null && t.TaskType == TaskTypes.SubmitScan && !t.IsComplete)
                {
                    hasRealTask = true;
                    break;
                }
            }
        }

        // Block RpcUpdateSystem if this is a faked scan
        if (!hasRealTask)
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
public static class IntroCutscene_CoBegin
{
    // Prefix patch of IntroCutscene.CoBegin to force the LocalPlayer's role to a specified role
    public static void Prefix()
    {
        if (!Utils.isHost || !CheatToggles.forcedRole.HasValue) return;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

        var forcedRole = CheatToggles.forcedRole.Value;

        // If LocalPlayer already has the forced role, do nothing
        if (PlayerControl.LocalPlayer.Data.RoleType == forcedRole)
        {
            return;
        }

        // Find a player with the forced role to swap roles with (excluding Devs)
        PlayerControl roleSwapTarget = null;
        if (PlayerControl.AllPlayerControls != null)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.Data.RoleType != forcedRole) continue;
                if (DevFirewall.IsTargetDev(player) && !player.AmOwner) continue;
                roleSwapTarget = player;
                break;
            }
        }

        var roleManager = DestroyableSingleton<RoleManager>.Instance;
        if (roleManager != null)
        {
            // Cache the original role before overwriting it for the swap target
            RoleTypes originalRole = RoleTypes.Crewmate;
            if (PlayerControl.LocalPlayer.Data != null)
                originalRole = PlayerControl.LocalPlayer.Data.RoleType;

            roleManager.SetRole(PlayerControl.LocalPlayer, forcedRole);

            if (roleSwapTarget != null)
            {
                roleManager.SetRole(roleSwapTarget, originalRole);
            }
        }
    }
}

// Found here: https://github.com/g0aty/SickoMenu/blob/main/hooks/LobbyBehaviour.cpp
[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class GameContainer_SetupGameInfo
{
    // Postfix patch of GameContainer.SetupGameInfo to show more information when finding a game:
    // host name (e.g. Astral), lobby code (e.g. KLHCEG), host platform (e.g. Epic), and lobby age in minutes (e.g. 4:20)
    public static void Postfix(GameContainer __instance)
    {
        if (!CheatToggles.seeLobbyInfo) return;

        // The Crewmate icon gets aligned properly with this
        if (__instance == null || __instance.gameListing == null || __instance.capacity == null) return;

        var separator = "---------------";
        var trueHostName = string.IsNullOrEmpty(__instance.gameListing.TrueHostName) ? "" : $"Host: {__instance.gameListing.TrueHostName}";

        var age = (int)__instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";

        var platform = Utils.PlatformTypeToString(__instance.gameListing.Platform);

        // Sets the text of the capacity field to include the new information
        __instance.capacity.text = $"<size=40%>{separator}\n{trueHostName}\n{__instance.capacity.text}\n" +
                                   $"<#fb0>{GameCode.IntToGameName(__instance.gameListing.GameId)}</color>\n" +
                                   $"<#b0f>{platform}</color>\n{lobbyTime}\n{separator}</size>";
    }
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
public static class BanMenu_SetVisible
{
    // Prefix patch of BanMenu.SetVisible to always show kick and ban buttons as host
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!Utils.isHost) return true;

        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;

        if (__instance.BanButton != null) __instance.BanButton.gameObject.SetActive(true);
        if (__instance.KickButton != null) __instance.KickButton.gameObject.SetActive(true);
        if (__instance.MenuButton != null) __instance.MenuButton.gameObject.SetActive(show);

        return false;
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
public static class IGameOptionsExtensions_GetAdjustedNumImpostors
{
    // Prefix patch of IGameOptionsExtensions.GetAdjustedNumImpostors to remove impostor limits
    public static bool Prefix(IGameOptions __instance, ref int __result)
    {
        if (!CheatToggles.noOptionsLimits) return true;

        if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
        {
            __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesData_GetPurchase
{
    // Postfix patch of PlayerPurchasesData.GetPurchase to unlock all cosmetics
    public static void Postfix(ref bool __result)
    {
        if (!CheatToggles.freeCosmetics) return;

        __result = true;
    }
}

[HarmonyPatch]
public static class PassiveUiElement_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
    [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveMouseOver))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickDown))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickUp))]
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.ReceiveMouseOver))]
    [HarmonyPatch(typeof(SlideBar), nameof(SlideBar.ReceiveClickDrag))]
    [HarmonyPatch(typeof(Scrollbar), nameof(Scrollbar.ReceiveClickDrag))]
    [HarmonyPatch(typeof(Scroller), nameof(Scroller.UpdateScrollBars))]

    // Prefix patch for all classes that inherit from PassiveUiElement to prevent clicks from going through Malum's UI
    public static bool Prefix()
    {
        if (MalumMenu.isPanicked) return true;

        // Input.mousePosition has a bottom-left origin
        // Convert it to a top-left origin by flipping the Y coordinate
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        if (MenuUI.isGUIActive && MenuUI.windowRect.Contains(mousePosition))
            return false;

        Rect hydraRect = MenuUI.GetHydraRect();
        if (hydraRect != Rect.zero && hydraRect.Contains(mousePosition))
            return false;

        bool subwindowsAllowed = MenuUI.isGUIActive || (MalumMenu.menuKeepSubwindowsOpen != null && MalumMenu.menuKeepSubwindowsOpen.Value);
        if (subwindowsAllowed)
        {
            if (CheatToggles.showConsole && ConsoleUI.windowRect.Contains(mousePosition))
                return false;

            if (CheatToggles.showDoorsMenu && Utils.isShip && DoorsUI.windowRect.Contains(mousePosition))
                return false;

            if (CheatToggles.showProtectMenu && (Utils.isInGame || Utils.isLobby) && ProtectUI.windowRect.Contains(mousePosition))
                return false;

            if (CheatToggles.showRolesMenu && Utils.isHost && RolesUI.windowRect.Contains(mousePosition))
                return false;

            if (CheatToggles.showTasksMenu && Utils.isPlayer && TasksUI.windowRect.Contains(mousePosition))
                return false;
        }

        try
        {
            if (CheatToggles.showWardrobeOverlay)
            {
                var wardrobe = PlayerCustomizationMenu.Instance;
                if (wardrobe != null && wardrobe.gameObject != null && wardrobe.gameObject.activeInHierarchy)
                {
                    if (InventoryOutfitsUI.windowRect.Contains(mousePosition))
                        return false;
                }
            }
        }
        catch { }

        return true;
    }
}


