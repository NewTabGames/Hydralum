using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
public static class PlayerControl_StartMeeting
{
    // Postfix of PlayerControl.StartMeeting, which runs on every client regardless of who called
    // the meeting (RpcStartMeeting and the received RPC both route through it). Logs on ConsoleUI
    // who started the meeting and whether it was an emergency button press or a body report.
    public static void Postfix(PlayerControl __instance, NetworkedPlayerInfo target)
    {
        if (!CheatToggles.logMeetings) return;

        try
        {
            var (realName, displayName, isDisguised) = Utils.GetPlayerIdentity(__instance);
            var caller = isDisguised ? $"{realName} (as {displayName})" : realName;

            if (target == null)
            {
                ConsoleUI.Log($"{caller} called an emergency meeting");
            }
            else
            {
                var body = $"<color=#{ColorUtility.ToHtmlStringRGB(target.Color)}>{target.PlayerName}</color>";
                ConsoleUI.Log($"{caller} reported {body}'s body");
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.BeginForGameplay))]
public static class ExileController_BeginForGameplay
{
    // Postfix of ExileController.BeginForGameplay to log the outcome of the vote: who (if anyone)
    // was ejected, or whether the vote was skipped or ended in a tie.
    public static void Postfix(NetworkedPlayerInfo player, bool voteTie)
    {
        if (!CheatToggles.logMeetings) return;

        try
        {
            if (player == null)
            {
                ConsoleUI.Log(voteTie ? "No one was ejected (tie)" : "No one was ejected (skipped)");
            }
            else
            {
                var ejected = $"<color=#{ColorUtility.ToHtmlStringRGB(player.Color)}>{player.PlayerName}</color>";
                ConsoleUI.Log($"{ejected} was ejected");
            }
        }
        catch { }
    }
}
