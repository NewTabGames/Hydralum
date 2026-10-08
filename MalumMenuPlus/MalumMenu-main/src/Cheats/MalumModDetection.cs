using System.Collections.Generic;
using HarmonyLib;
using InnerNet;

namespace MalumMenu;

// Passive client fingerprinting, adapted from KrushMenu's ModDetection.
//
// Vanilla Among Us only sends PlayerControl RPCs whose callId is in the game's RpcCalls enum (plus a
// few newer ones we track, like SpiritGuide). A menu that adds custom network features sends RPCs
// with its own out-of-range callId, so an out-of-range callId betrays a modded client - and specific
// callIds map to specific known menus (their "handshake").
//
// Limitations worth knowing:
//  - Only catches menus that emit a CUSTOM (non-vanilla) RPC. Menus that merely abuse vanilla RPCs
//    (Malum and Hydra included - they reuse SnapTo, MurderPlayer, etc.) have no custom callId and so
//    read as "Unmodded". Those need behavioural detection, not callId detection.
//  - "Unmodded" means "no mod RPC seen", not a proof of innocence - a careful client stays vanilla.
public static class MalumModDetection
{
    // callId -> menu name. Only the signatures with confirmed names from KrushMenu's list; anything
    // else non-vanilla is reported generically as "Modded (RPC N)". Add entries here as you fingerprint
    // more menus (enable the Console tab's incoming-RPC logger to capture their callIds).
    private static readonly Dictionary<byte, string> KnownMods = new()
    {
        { 121, "Chocoo" },
        { 167, "TuffMenu" },
        { 164, "Hydra / Sicko / SickoMenu" },
        { 176, "HostGuard / TOH" },
        { 195, "Polar" },
        { 204, "Polar" },
        { 154, "GNC" },
        { 250, "KillNet" },
        { 80,  "KillNet" },
        { 85,  "KillNet" },
        { 150, "BetterAmongUs / GreaterAmongUs" },
        { 82,  "Unknown (RPC 82)" },
        { 103, "Unknown (Askinchik)" },
        { 212, "BanMod" },
        { 213, "BanMod" },
        { 214, "BanMod" },
        { 215, "BanMod" },
        { 216, "BanMod" },
        { 217, "BanMod" },
        { 218, "BanMod" },
        { 144, "Gaff Menu" },
        { 145, "Gaff Menu" },
        { 188, "GMM" },
        { 189, "GMM" },
        { 169, "Malum Menu" },
        { 133, "Lunar / NjordMenu" },
        { 210, "Lunar / NjordMenu" },
        { 89,  "NjordMenu (Old)" },
        { 202, "ModMenuCrew" },        // from Krush's spoof list (sends an "MMC_v5" handshake)
        { 162, "Unknown (RPC 162)" },  // real menu in Krush's list; name obfuscated there
        { 255, "Unknown (RPC 255)" },  // real menu in Krush's list; name obfuscated there
    };

    // playerId -> detected client label(s). Cleared on joining a new lobby.
    private static readonly Dictionary<byte, HashSet<string>> _detected = new();

    public static void Reset() => _detected.Clear();

    // Called for every PlayerControl RPC we receive (from the HandleRpc logging prefix). Flags the
    // sender when the callId is a known signature or any non-vanilla RPC.
    public static void Observe(PlayerControl player, byte callId)
    {
        if (player == null || player.Data == null || player.AmOwner) return;

        string label;
        if (KnownMods.TryGetValue(callId, out var mod))
            label = mod;
        else if (IsNonVanilla(callId))
            label = $"Modded (RPC {callId})";
        else
            return; // ordinary vanilla RPC - tells us nothing

        Record(player.PlayerId, label);
    }

    // Starlight is a third-party Android BepInEx loader that reports platform ID 112, so a player on
    // platform 112 is running it. This is a platform signature, not an RPC one.
    private const int StarlightPlatformId = 112;

    public static void CheckPlatform(PlayerControl player)
    {
        if (player == null || player.Data == null || player.AmOwner) return;
        try
        {
            var client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromCharacter(player) : null;
            if (client != null && (int)client.PlatformData.Platform == StarlightPlatformId)
                Record(player.PlayerId, "Starlight");
        }
        catch { }
    }

    private static void Record(byte playerId, string label)
    {
        if (!_detected.TryGetValue(playerId, out var set))
            set = _detected[playerId] = new HashSet<string>();
        set.Add(label);
    }

    // A PlayerControl callId that isn't in the game's RpcCalls enum and isn't one of the newer vanilla
    // RPCs we track (e.g. SpiritGuide 67). Those extras are vanilla, so they must not false-flag.
    private static bool IsNonVanilla(byte callId)
    {
        if (System.Enum.IsDefined(typeof(RpcCalls), (RpcCalls)callId)) return false;
        if (RpcLoggingHelper.IsKnownExtraRpc(callId)) return false;
        return true;
    }

    public static bool IsDetected(byte playerId)
        => _detected.TryGetValue(playerId, out var set) && set.Count > 0;

    public static string GetClientLabel(byte playerId)
    {
        if (_detected.TryGetValue(playerId, out var set) && set.Count > 0)
            return string.Join(", ", set);
        return "Unmodded";
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    private static class ResetOnJoin
    {
        private static void Postfix() => Reset();
    }

    // Platform-based detection runs when a player spawns (their platform data is available by then).
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    private static class CheckPlatformOnStart
    {
        private static void Postfix(PlayerControl __instance) => CheckPlatform(__instance);
    }
}
