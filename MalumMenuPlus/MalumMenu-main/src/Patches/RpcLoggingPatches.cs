using System;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

public static class RpcLoggingHelper
{
    public static void LogIncoming(PlayerControl player, byte callId, string fallbackName = "Object")
    {
        if (!CheatToggles.logIncomingRpcs) return;
        try
        {
            string rpcName = Enum.IsDefined(typeof(RpcCalls), (RpcCalls)callId)
                ? ((RpcCalls)callId).ToString()
                : $"UnknownRpc_{callId}";

            string playerText = fallbackName;
            string idText = "?";

            if (player != null && player.Data != null)
            {
                string pName = player.Data.PlayerName ?? "Unknown";
                byte pId = player.PlayerId;
                string colorHex = "FFFFFF";

                try
                {
                    if (player.Data.DefaultOutfit != null)
                    {
                        int colorId = player.Data.DefaultOutfit.ColorId;
                        if (Palette.PlayerColors != null && colorId >= 0 && colorId < Palette.PlayerColors.Length)
                        {
                            colorHex = ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[colorId]);
                        }
                    }
                }
                catch { }

                playerText = $"<color=#{colorHex}>{pName}</color>";
                idText = pId.ToString();
            }

            DebugUI.Log($"Received RPC from {playerText} (ID: {idText}): <color=#FF4444>{rpcName}</color> ({callId})");
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class PlayerControl_HandleRpc_Patch
{
    public static void Prefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        RpcLoggingHelper.LogIncoming(__instance, callId);
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
public static class PlayerPhysics_HandleRpc_Patch
{
    public static void Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
    {
        RpcLoggingHelper.LogIncoming(__instance.myPlayer, callId, "PlayerPhysics");
    }
}

[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
public static class CustomNetworkTransform_HandleRpc_Patch
{
    public static void Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
    {
        RpcLoggingHelper.LogIncoming(__instance.myPlayer, callId, "NetworkTransform");
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleRpc))]
public static class MeetingHud_HandleRpc_Patch
{
    public static void Prefix(MeetingHud __instance, byte callId, MessageReader reader)
    {
        RpcLoggingHelper.LogIncoming(null, callId, "MeetingHud");
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
public static class ShipStatus_HandleRpc_Patch
{
    public static void Prefix(ShipStatus __instance, byte callId, MessageReader reader)
    {
        RpcLoggingHelper.LogIncoming(null, callId, "ShipStatus");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.StartRpcImmediately))]
public static class AmongUsClient_StartRpcImmediately_Patch
{
    public static void Prefix(uint targetNetId, byte callId, SendOption sendOption, int targetClientId)
    {
        if (!CheatToggles.logOutgoingRpcs) return;
        try
        {
            string rpcName = Enum.IsDefined(typeof(RpcCalls), (RpcCalls)callId)
                ? ((RpcCalls)callId).ToString()
                : $"UnknownRpc_{callId}";

            DebugUI.Log($"Starting RPC: {callId} ({rpcName}) as {targetNetId} with SendOption {sendOption} to {targetClientId}");
        }
        catch { }
    }
}



[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class PlayerControl_MurderPlayer_LogPatch
{
    public static void Postfix(PlayerControl __instance, PlayerControl target)
    {
        if (!CheatToggles.logDeaths) return;
        try
        {
            if (__instance?.Data == null || target?.Data == null) return;
            string killerName = __instance.Data.PlayerName;
            string victimName = target.Data.PlayerName;
            string msg = $"<color=#FF4444>[Death]</color> {killerName} murdered {victimName}";
            DebugUI.Log(msg);
            ConsoleUI.Log(msg);
        }
        catch { }
    }
}
