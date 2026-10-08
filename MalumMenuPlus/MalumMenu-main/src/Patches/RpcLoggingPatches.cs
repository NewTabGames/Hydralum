using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

public static class RpcLoggingHelper
{
    // RPCs added after the pinned AmongUs.GameLibs version, so they aren't in the referenced RpcCalls enum
    // and would otherwise log as "UnknownRpc_N". Keep this in sync as new roles/features are added.
    private static readonly Dictionary<byte, string> ExtraRpcNames = new Dictionary<byte, string>
    {
        { 67, "SpiritGuideMessage" }, // Influencer (SpiritGuide) - added in the 2026-09 update
    };

    // Resolves an RPC's name: the game's RpcCalls enum first, then our extra map for newer RPCs, then a
    // numeric fallback. Used by both the incoming and outgoing RPC loggers.
    public static string ResolveRpcName(byte callId)
    {
        if (Enum.IsDefined(typeof(RpcCalls), (RpcCalls)callId))
            return ((RpcCalls)callId).ToString();
        if (ExtraRpcNames.TryGetValue(callId, out var name))
            return name;
        return $"UnknownRpc_{callId}";
    }

    // Newer-but-vanilla RPCs (added after the pinned GameLibs) that mod detection must not flag.
    public static bool IsKnownExtraRpc(byte callId) => ExtraRpcNames.ContainsKey(callId);

    public static void LogIncoming(PlayerControl player, byte callId, string fallbackName = "Object")
    {
        if (!CheatToggles.logIncomingRpcs) return;
        try
        {
            string rpcName = ResolveRpcName(callId);

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
        // Client fingerprinting runs on every PlayerControl RPC regardless of the logging toggle.
        MalumModDetection.Observe(__instance, callId);
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

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpcImmediately))]
public static class InnerNetClient_StartRpcImmediately_Patch
{
    public static void Prefix(uint targetNetId, byte callId, SendOption option, int targetClientId)
    {
        if (!CheatToggles.logOutgoingRpcs) return;
        try
        {
            string rpcName = RpcLoggingHelper.ResolveRpcName(callId);

            DebugUI.Log($"Starting RPC: {callId} ({rpcName}) as {targetNetId} with SendOption {option} to {targetClientId}");
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
