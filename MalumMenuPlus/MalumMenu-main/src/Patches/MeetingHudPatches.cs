using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace MalumMenu;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class MeetingHud_Update
{
    public static List<int> votedPlayers = new List<int>();

    // Prefix patch of MeetingHud.Update to constantly bloop new vote icons for each new vote being cast during the meeting
    public static void Prefix(MeetingHud __instance)
    {
        if (!CheatToggles.revealVotes) return; // Only process when revealVotes is active

        try
        {
            if (__instance == null || __instance.playerStates == null) return;

            if (__instance.resultsStartedAt <= 0f)
            {
                foreach (var playerVoteArea in __instance.playerStates)
                {
                    if (!playerVoteArea) continue;

                    byte targetId = PlayerVoteAreaHelper.GetPlayerId(playerVoteArea);
                    int votedFor = PlayerVoteAreaHelper.GetVotedFor(playerVoteArea);

                    var playerData = GameData.Instance?.GetPlayerById(targetId);

                    if (playerData != null && !playerData.Disconnected && votedFor != PlayerVoteAreaHelper.HasNotVoted && votedFor != PlayerVoteAreaHelper.MissedVote && votedFor != PlayerVoteAreaHelper.DeadVote && !votedPlayers.Contains(targetId))
                    {
                        votedPlayers.Add(targetId);

                        if (votedFor != PlayerVoteAreaHelper.SkippedVote)
                        {
                            foreach (var votedForArea in __instance.playerStates)
                            {
                                if (votedForArea != null && PlayerVoteAreaHelper.GetPlayerId(votedForArea) == votedFor)
                                {
                                    __instance.BloopAVoteIcon(playerData, 0, votedForArea.transform);
                                    break;
                                }
                            }
                        }
                        else if (__instance.SkippedVoting != null)
                        {
                            __instance.BloopAVoteIcon(playerData, 0, __instance.SkippedVoting.transform);
                        }
                    }
                }

                foreach (var votedForArea in __instance.playerStates)
                {
                    if (!votedForArea) continue;

                    var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
                    if (!voteSpreader || voteSpreader.Votes == null) continue;

                    foreach (var spriteRenderer in voteSpreader.Votes)
                    {
                        if (spriteRenderer != null && spriteRenderer.gameObject != null)
                            spriteRenderer.gameObject.SetActive(true);
                    }
                }

                if (__instance.SkippedVoting != null)
                {
                    var voteSpreader = __instance.SkippedVoting.transform.GetComponent<VoteSpreader>();
                    if (voteSpreader != null && voteSpreader.Votes != null)
                    {
                        foreach (var spriteRenderer in voteSpreader.Votes)
                        {
                            if (spriteRenderer != null && spriteRenderer.gameObject != null)
                                spriteRenderer.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
        catch { }
    }

    public static void Postfix(MeetingHud __instance)
    {
        try
        {
            MalumESP.MeetingNametags(__instance);

            // Bugfix: NoClip staying active if meeting is called whilst climbing ladder
            if (PlayerControl.LocalPlayer)
            {
                PlayerControl.LocalPlayer.onLadder = false;
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
public static class MeetingHud_PopulateResults
{
    // Prefix patch of MeetingHud.PopulateResults to clear all vote icons before repopulating them for final results
    public static void Prefix(MeetingHud __instance)
    {
        try
        {
            if (__instance == null) return;

            if (__instance.playerStates != null)
            {
                foreach (var votedForArea in __instance.playerStates)
                {
                    if (!votedForArea) continue;

                    var voteSpreader = votedForArea.transform.GetComponent<VoteSpreader>();
                    if (!voteSpreader || voteSpreader.Votes == null) continue;

                    var length = voteSpreader.Votes.Count;
                    if (length == 0) continue;

                    foreach (var spriteRenderer in voteSpreader.Votes)
                    {
                        if (spriteRenderer != null) Object.DestroyImmediate(spriteRenderer);
                    }

                    voteSpreader.Votes.Clear();
                }
            }

            if (__instance.SkippedVoting != null)
            {
                var voteSpreader = __instance.SkippedVoting.transform.GetComponent<VoteSpreader>();
                if (voteSpreader != null && voteSpreader.Votes != null)
                {
                    foreach (var spriteRenderer in voteSpreader.Votes)
                    {
                        if (spriteRenderer != null) Object.DestroyImmediate(spriteRenderer);
                    }

                    voteSpreader.Votes.Clear();
                }
            }

            MeetingHud_Update.votedPlayers.Clear();
        }
        catch { }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
public static class MeetingHud_CheckForEndVoting
{
    // Prefix patch of MeetingHud.CheckForEndVoting to make the local player immune to being voted out
    public static bool Prefix(MeetingHud __instance)
    {
        if (!CheatToggles.voteImmune) return true; // We don't need to check whether we are host because this method only runs on the host's side

        try
        {
            if (__instance == null || __instance.playerStates == null || !__instance.playerStates.All(ps => ps != null && (ps.AmDead || ps.DidVote))) return true;

            var max = __instance.CalculateVotes().MaxPair(out var tie);
            var allPlayers = GameData.Instance?.AllPlayers?.ToArray();
            var exiled = allPlayers?.FirstOrDefault(v => !tie && v.PlayerId == max.Key);

            // This is the only change from the original method - make sure local player is not exiled
            if (exiled != null && PlayerControl.LocalPlayer != null && exiled == PlayerControl.LocalPlayer.Data)
            {
                exiled = null;
            }

            var states = new MeetingHud.VoterState[__instance.playerStates.Length];

            for (var index = 0; index < __instance.playerStates.Length; ++index)
            {
                var playerState = __instance.playerStates[index];
                if (playerState != null)
                {
                    states[index] = new MeetingHud.VoterState
                    {
                        VoterId = PlayerVoteAreaHelper.GetPlayerId(playerState),
                        VotedForId = (byte)PlayerVoteAreaHelper.GetVotedFor(playerState)
                    };
                }
            }

            var rpcMethod = typeof(MeetingHud).GetMethod(nameof(MeetingHud.RpcVotingComplete));
            if (rpcMethod != null)
            {
                var pars = rpcMethod.GetParameters();
                if (pars.Length >= 5)
                {
                    rpcMethod.Invoke(__instance, new object[] { states, exiled, tie, false, (ushort)0 });
                }
                else
                {
                    rpcMethod.Invoke(__instance, new object[] { states, exiled, tie });
                }
            }

            return false;
        }
        catch
        {
            return true;
        }
    }
}
