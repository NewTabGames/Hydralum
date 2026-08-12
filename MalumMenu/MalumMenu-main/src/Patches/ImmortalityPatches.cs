using HarmonyLib;

namespace MalumMenu;

// Become Immortal, adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
//
// The backend murder check decides whether you can be killed partly by reading vent occupancy from
// the ShipStatus VentilationSystem. By sending a VentilationSystem "Enter" for a vent id that does
// not exist (50), we make the server believe we are permanently sitting in a vent, so it refuses
// every kill against us - while to other players we still walk around normally. We also block our
// own real vent enter/exit/move sends so the fake occupancy is never cleared.
public static class MalumImmortality
{
    public const int CustomVentId = 50;
    private static bool _active;

    // Sends the Enter/Exit RPC when the toggle flips. Poll this every frame.
    public static void Sync()
    {
        if (CheatToggles.becomeImmortal == _active) return;

        var localPlayer = PlayerControl.LocalPlayer;

        // Only send while not physically inside a real vent (if we are, the server already thinks
        // we're vented and the block patch keeps it that way).
        if (localPlayer != null && !localPlayer.inVent)
        {
            VentilationSystem.Update(
                CheatToggles.becomeImmortal ? VentilationSystem.Operation.Enter : VentilationSystem.Operation.Exit,
                CustomVentId);
        }

        _active = CheatToggles.becomeImmortal;
    }

    // Re-asserts the fake vent occupancy after events that reset the ventilation system (a new game
    // starting or a meeting ending).
    public static void Reassert()
    {
        if (!CheatToggles.becomeImmortal) return;

        VentilationSystem.Update(VentilationSystem.Operation.Enter, CustomVentId);
    }
}

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
public static class Immortal_VentilationUpdate
{
    // Block our own real vent enter/exit/move updates while immortal so the server keeps believing
    // we are sitting in the fake vent. Our own vent-50 updates and unrelated ops (e.g. BootImpostors
    // used by the vent-kick cheats) still pass through.
    public static bool Prefix(VentilationSystem.Operation op, int ventId)
    {
        if (!CheatToggles.becomeImmortal || ventId == MalumImmortality.CustomVentId) return true;

        return op != VentilationSystem.Operation.Enter
            && op != VentilationSystem.Operation.Exit
            && op != VentilationSystem.Operation.Move;
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
public static class Immortal_GameStart
{
    public static void Postfix() => MalumImmortality.Reassert();
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
public static class Immortal_MeetingClose
{
    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null
            || PlayerControl.LocalPlayer.Data.IsDead) return;

        MalumImmortality.Reassert();
    }
}
