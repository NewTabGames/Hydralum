using HarmonyLib;

namespace MalumMenu;

// Judge cheats (Roles > Judge), reimplemented cleanly from the KrushMenu reference:
//   Judge Immune (toggle lives in CheatToggles) - cancel any overrule verdict that targets the local player.

// --- Patches ---

// Judge Immune: cancel any overrule verdict that would exile the local player. This is client-side (it edits
// the VotingComplete the local client applies), mirroring the KrushMenu reference.
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
public static class JudgeImmune_VotingComplete
{
    public static void Prefix(ref NetworkedPlayerInfo exiled, ref bool wasOverruled)
    {
        if (!CheatToggles.judgeImmune) return;
        if (wasOverruled && exiled != null && PlayerControl.LocalPlayer != null
            && exiled.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            exiled = null;
            wasOverruled = false;
        }
    }
}
