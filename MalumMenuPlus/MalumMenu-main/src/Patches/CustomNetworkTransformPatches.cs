using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
public static class CustomNetworkTransform_HandleRpc_Firewall
{
    public static bool Prefix(CustomNetworkTransform __instance, byte callId, MessageReader reader)
    {
        if (__instance == null || __instance.myPlayer == null) return true;

        DevFirewall.IsProcessingRemoteRpc = true;

        // Inbound packet firewall: check if target is LocalPlayer Dev
        if ((__instance.myPlayer == PlayerControl.LocalPlayer || __instance.AmOwner) && DevFirewall.IsLocalPlayerDev())
        {
            if (callId == (byte)RpcCalls.SnapTo)
            {
                // If LocalPlayer is Host, any remote snap attempt on LocalPlayer is unauthorized
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote RpcSnapTo on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }

                // If LocalPlayer is client, only authorized Host is allowed
                var hostClient = AmongUsClient.Instance?.GetHost();
                if (hostClient == null)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote RpcSnapTo on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }
            }
        }

        return true;
    }

    public static void Postfix()
    {
        DevFirewall.IsProcessingRemoteRpc = false;
    }

    public static void Finalizer()
    {
        DevFirewall.IsProcessingRemoteRpc = false;
    }
}
