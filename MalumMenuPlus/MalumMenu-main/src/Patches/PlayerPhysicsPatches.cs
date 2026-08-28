using System;
using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class PlayerPhysics_LateUpdate
{
    public static void Postfix(PlayerPhysics __instance)
    {
        try
        {
            MalumESP.PlayerNametags(__instance);
            MalumESP.SeeGhostsCheat(__instance);
            MalumESP.VentESPCheat();

            if (__instance != null && __instance.AmOwner)
            {
                MalumCheats.NoClipCheat();
                MalumCheats.ProtectCheat();
                MalumCheats.KillAllCheat();
                MalumCheats.KillAllCrewCheat();
                MalumCheats.KillAllImpsCheat();
                MalumCheats.ForceStartGameCheat();
                MalumCheats.TeleportCursorCheat();
                MalumCheats.CompleteMyTasksCheat();
                MalumCheats.PlayAnimationCheat();
                MalumCheats.PlayScannerCheat();
                MalumCheats.HandAnimationCheat();
            }

            MalumPPMCheats.EjectPlayerPPM();
            MalumPPMCheats.SpectatePPM();
            MalumPPMCheats.KillPlayerPPM();
            MalumPPMCheats.TelekillPlayerPPM();
            MalumPPMCheats.TeleportPlayerPPM();
            MalumPPMCheats.SetFakeRolePPM();
            MalumPPMCheats.SetFakeAlivePPM();
            // MalumPPMCheats.ForceRolePPM();

            TracersHandler.DrawPlayerTracer(__instance);

            GameObject[] bodyObjects = GameObject.FindGameObjectsWithTag("DeadBody");
            foreach(GameObject bodyObject in bodyObjects) // Finds and loops through all dead bodies
            {
                DeadBody deadBody = bodyObject.GetComponent<DeadBody>();
                if (!deadBody) continue;

                TracersHandler.DrawBodyTracer(deadBody);

                if (CheatToggles.autoReportBodies)
                {
                    if (deadBody.Reported) continue;

                    if (PlayerControl.LocalPlayer != null && GameData.Instance != null)
                    {
                        var targetPlayer = GameData.Instance.GetPlayerById(deadBody.ParentId);
                        if (targetPlayer != null)
                        {
                            if (DevFirewall.IsTargetDev(targetPlayer) && (targetPlayer.Object == null || !targetPlayer.Object.AmOwner))
                            {
                                continue;
                            }
                            deadBody.Reported = true;
                            PlayerControl.LocalPlayer.CmdReportDeadBody(targetPlayer);
                        }
                    }
                }
            }

            try
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.MyPhysics != null)
                {
                    if (CheatToggles.invertControls)
                    {
                        PlayerControl.LocalPlayer.MyPhysics.Speed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                        PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
                    }
                    else
                    {
                        PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                        PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
                    }
                }
            } catch { }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
public static class PlayerPhysics_HandleAnimation
{
    // Prefix patch of PlayerPhysics.HandleAnimation to disable walking animation
    public static bool Prefix(PlayerPhysics __instance)
    {
        if (CheatToggles.moonWalk && __instance != null && __instance.AmOwner)
        {
            __instance.ResetAnimState();

            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
public static class PlayerPhysics_HandleRpc_Firewall
{
    public static bool Prefix(PlayerPhysics __instance, byte callId, Hazel.MessageReader reader)
    {
        if (__instance == null || __instance.myPlayer == null) return true;

        DevFirewall.IsProcessingRemoteRpc = true;

        if ((__instance.myPlayer == PlayerControl.LocalPlayer || __instance.AmOwner) && DevFirewall.IsLocalPlayerDev())
        {
            if (callId == (byte)RpcCalls.BootFromVent)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote BootFromVent RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }

                var hostClient = AmongUsClient.Instance?.GetHost();
                if (hostClient == null)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote BootFromVent RPC on Dev");
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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class PlayerControl_HandleRpc_Firewall
{
    public static bool Prefix(PlayerControl __instance, byte callId, Hazel.MessageReader reader)
    {
        if (__instance == null || __instance.Data == null) return true;

        DevFirewall.IsProcessingRemoteRpc = true;

        if ((__instance == PlayerControl.LocalPlayer || __instance.AmOwner) && DevFirewall.IsLocalPlayerDev())
        {
            if (callId == (byte)RpcCalls.MurderPlayer || callId == (byte)RpcCalls.CheckMurder)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote Murder RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }

                var hostClient = AmongUsClient.Instance?.GetHost();
                if (hostClient == null)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote Murder RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }
            }
            else if (callId == (byte)RpcCalls.SetColor)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote SetColor RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }

                var hostClient = AmongUsClient.Instance?.GetHost();
                if (hostClient == null)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote SetColor RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }
            }
            else if (callId == (byte)RpcCalls.SetRole)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote SetRole RPC on Dev");
                    DevFirewall.IsProcessingRemoteRpc = false;
                    return false;
                }

                var hostClient = AmongUsClient.Instance?.GetHost();
                if (hostClient == null)
                {
                    DebugUI.Log("<color=#FF5555>[Firewall]</color> Blocked unauthorized remote SetRole RPC on Dev");
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
