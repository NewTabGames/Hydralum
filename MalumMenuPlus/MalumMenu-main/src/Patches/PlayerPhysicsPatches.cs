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
                            deadBody.Reported = true;
                            PlayerControl.LocalPlayer.CmdReportDeadBody(targetPlayer);
                        }
                    }
                }
            }

            try
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
            } catch (NullReferenceException) { }
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
        if (CheatToggles.moonWalk && __instance.AmOwner)
        {
            __instance.ResetAnimState();

            return false;
        }

        return true;
    }
}
