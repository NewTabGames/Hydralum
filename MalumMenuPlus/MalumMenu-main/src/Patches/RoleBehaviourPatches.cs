using HarmonyLib;
using System.Linq;
using Sentry.Internal.Extensions;
using UnityEngine;
using UnityEngine.Audio;

namespace MalumMenu;

[HarmonyPatch(typeof(EngineerRole), nameof(EngineerRole.FixedUpdate))]
public static class EngineerRole_FixedUpdate
{
    public static void Postfix(EngineerRole __instance)
    {
        try {
            if (__instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                MalumCheats.HandleEngineerCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(ShapeshifterRole), nameof(ShapeshifterRole.FixedUpdate))]
public static class ShapeshifterRole_FixedUpdate
{
    public static void Postfix(ShapeshifterRole __instance)
    {
        try
        {
            if (__instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                MalumCheats.HandleShapeshifterCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(ScientistRole), nameof(ScientistRole.Update))]
public static class ScientistRole_Update
{
    public static void Postfix(ScientistRole __instance)
    {
        try {
            if (__instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                MalumCheats.HandleScientistCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FixedUpdate))]
public static class TrackerRole_FixedUpdate
{
    public static void Postfix(TrackerRole __instance)
    {
        try {
            if (__instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                MalumCheats.HandleTrackerCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.FixedUpdate))]
public static class DetectiveRole_FixedUpdate
{
    public static void Postfix(DetectiveRole __instance)
    {
        try {
            if (__instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                MalumCheats.HandleDetectiveCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.SetCooldown))]
public static class DetectiveRole_SetCooldown
{
    // SetCooldown runs at role assignment (via Initialize) and after each interrogate. If we only
    // zeroed the cooldown in FixedUpdate, becoming Detective with the toggle already active could leave
    // the initial cooldown standing until the freshly-created component starts ticking FixedUpdate.
    // Skipping SetCooldown when the toggle is on means no cooldown is ever applied in the first place.
    public static bool Prefix(DetectiveRole __instance)
    {
        try {
            if (CheatToggles.noInterrogateCooldown && __instance != null && __instance.Player != null && __instance.Player.AmOwner)
            {
                __instance.cooldownSecondsRemaining = 0f;
                return false;
            }
        } catch { }
        return true;
    }
}

[HarmonyPatch(typeof(SpiritGuideRole), nameof(SpiritGuideRole.FindClosestTarget))]
public static class SpiritGuideRole_FindClosestTarget
{
    // Infinite reach: pick the closest LIVING player (the Influencer sends photos to the living) with no
    // distance limit, mirroring the other role reach patches.
    public static bool Prefix(ref PlayerControl __result)
    {
        if (!CheatToggles.spiritGuideReach) return true;

        var sorted = Utils.GetPlayersSortedByDistance();
        if (sorted == null) { __result = null; return false; }

        var local = PlayerControl.LocalPlayer;
        var playerList = sorted.Where(player => !player.IsNull() && player.Data != null && !player.Data.IsDead
            && (local == null || player != local) && player.Collider != null && player.Collider.enabled).ToList();

        __result = playerList.Count > 0 ? playerList[0] : null;
        return false;
    }
}

// --- Influencer (SpiritGuideRole) ability cheats, ported from KrushMenu ---
// The Influencer's ability is sending photo "messages": it has a message cooldown, a per-target selection
// cooldown, a cap on how many photos can be selected at once, and a cooldown after refreshing the image
// grid. These zero/bypass each of those. (Infinite Message Range is already the "Reach" toggle above.)

// No Message Cooldown + No Selection Cooldown both live on the role's per-frame Update; only touch our own
// SpiritGuide instance (AmOwner), like the other role-behaviour patches.
[HarmonyPatch(typeof(SpiritGuideRole), nameof(SpiritGuideRole.Update))]
public static class SpiritGuideRole_Update_InfluencerCheats
{
    public static void Postfix(SpiritGuideRole __instance)
    {
        if (!CheatToggles.noMessageCooldown && !CheatToggles.noSelectionCooldown) return;

        try
        {
            if (__instance == null || __instance.Player == null || !__instance.Player.AmOwner) return;

            if (CheatToggles.noMessageCooldown)
            {
                __instance.cooldownSecondsRemaining = 0f;

                var hud = HudManager.Instance;
                if (hud != null && hud.AbilityButton != null)
                {
                    ((ActionButton)hud.AbilityButton).ResetCoolDown();
                    ((ActionButton)hud.AbilityButton).SetCooldownFill(0f);
                }
            }

            if (CheatToggles.noSelectionCooldown)
                __instance.selectionCooldown = false;
        }
        catch { }
    }
}

// No Refresh Cooldown: zero the message cooldown whenever the image grid is refreshed. Skipped when No
// Message Cooldown is already holding it at zero every frame (mirrors KrushMenu).
[HarmonyPatch(typeof(SpiritGuideRole), nameof(SpiritGuideRole.RefreshImages))]
public static class SpiritGuideRole_RefreshImages_InfluencerCheats
{
    public static void Prefix(SpiritGuideRole __instance)
    {
        if (!CheatToggles.noRefreshCooldown || CheatToggles.noMessageCooldown) return;
        try { if (__instance != null) __instance.cooldownSecondsRemaining = 0f; } catch { }
    }
}

// No Photo Limit: re-add every toggled image button to the selection set so the vanilla cap never stops us
// selecting more photos to send at once.
[HarmonyPatch(typeof(SpiritGuideImageButton), nameof(SpiritGuideImageButton.ToggleSelection))]
public static class SpiritGuideImageButton_ToggleSelection_InfluencerCheats
{
    public static void Postfix(SpiritGuideImageButton __instance)
    {
        if (!CheatToggles.noPhotoLimit || __instance == null) return;

        try
        {
            var role = __instance.spiritGuideRole;
            if (role == null || role.SendingImage) return;

            var selected = role.selectedImageButtons;
            if (selected == null || selected.Contains(__instance)) return;

            selected.Add(__instance);

            if (__instance.highlight != null) __instance.highlight.SetActive(true);
            if (__instance.spiritGuideImage != null) __instance.spiritGuideImage.color = __instance.selectedColor;
            if (__instance.selectAudio != null && SoundManager.Instance != null)
                SoundManager.Instance.PlaySound(__instance.selectAudio, false, 1f, (AudioMixerGroup)null);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.IsValidTarget))]
public static class PhantomRole_IsValidTarget
{
    // Postfix patch of PhantomRole.IsValidTarget to allow killing while invisible
    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killVanished)
        {
            __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
public static class ImpostorRole_IsValidTarget
{
    // Postfix patch of ImpostorRole.IsValidTarget to allow forbidden kill targets for killAnyone cheat
    // Allows killing ghosts (with seeGhosts), impostors, players in vents, etc...
    public static void Postfix(NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killAnyone)
        {
           __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.FindClosestTarget))]
public static class ImpostorRole_FindClosestTarget
{
    // Prefix patch of ImpostorRole.FindClosestTarget to allow for infinite kill reach
    public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.killReach) return true;

        var sorted = Utils.GetPlayersSortedByDistance();
        if (sorted == null) { __result = null; return false; }

        var playerList = sorted.Where(player => !player.IsNull() && player.Data != null && __instance.IsValidTarget(player.Data) && player.Collider != null && player.Collider.enabled).ToList();

        __result = playerList.Count > 0 ? playerList[0] : null;

        return false;
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.FindClosestTarget))]
public static class DetectiveRole_FindClosestTarget
{
    // Prefix patch of DetectiveRole.FindClosestTarget to allow for infinite interrogate reach
    public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.interrogateReach) return true;

        var sorted = Utils.GetPlayersSortedByDistance();
        if (sorted == null) { __result = null; return false; }

        var playerList = sorted.Where(player => !player.IsNull() && player.Data != null && __instance.IsValidTarget(player.Data) && player.Collider != null && player.Collider.enabled).ToList();

        __result = playerList.Count > 0 ? playerList[0] : null;

        return false;
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FindClosestTarget))]
public static class TrackerRole_FindClosestTarget
{
    // Prefix patch of TrackerRole.FindClosestTarget to allow for infinite track reach
    public static bool Prefix(TrackerRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.trackReach) return true;

        var sorted = Utils.GetPlayersSortedByDistance();
        if (sorted == null) { __result = null; return false; }

        var playerList = sorted.Where(player => !player.IsNull() && player.Data != null && __instance.IsValidTarget(player.Data) && player.Collider != null && player.Collider.enabled).ToList();

        __result = playerList.Count > 0 ? playerList[0] : null;

        return false;
    }
}

[HarmonyPatch(typeof(GuardianAngelRole), nameof(GuardianAngelRole.FindClosestTarget))]
public static class GuardianAngelRole_FindClosestTarget
{
    // Prefix patch of GuardianAngelRole.FindClosestTarget for two Guardian Angel settings:
    //   Infinite Protection Range: choose the closest living player with no distance limit (like the
    //     other role "reach" patches; GuardianAngelRole exposes no range value of its own).
    //   Ignore Impostors: skip impostor-role players when picking who to protect.
    // (GuardianAngelRole has no IsValidTarget of its own, so we validate targets ourselves: living,
    // not the local player, with an active collider.)
    public static bool Prefix(ref PlayerControl __result)
    {
        if (!CheatToggles.gaInfiniteRange && !CheatToggles.gaIgnoreImpostors) return true;

        var sorted = Utils.GetPlayersSortedByDistance();
        if (sorted == null) { __result = null; return false; }

        var local = PlayerControl.LocalPlayer;
        var playerList = sorted.Where(player =>
            !player.IsNull() && player.Data != null && !player.Data.IsDead &&
            (local == null || player != local) &&
            player.Collider != null && player.Collider.enabled &&
            (!CheatToggles.gaIgnoreImpostors || player.Data.Role == null || !player.Data.Role.IsImpostor)).ToList();

        __result = playerList.Count > 0 ? playerList[0] : null;

        return false;
    }
}
