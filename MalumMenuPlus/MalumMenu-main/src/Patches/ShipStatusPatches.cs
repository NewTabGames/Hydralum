using HarmonyLib;

namespace MalumMenu;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class ShipStatus_FixedUpdate
{
    public static void Postfix(ShipStatus __instance)
    {
        MalumSabotageCheats.Process(__instance);
        MalumCheats.OpenSabotageMapCheat();

        MalumCheats.CloseMeetingCheat();
        MalumCheats.SkipMeetingCheat();
        MalumCheats.CallMeetingCheat();
        MalumCheats.WalkInVentCheat();
        MalumCheats.KickVentsCheat();
        MalumCheats.DisableVentsCheat();

        MalumPPMCheats.ReportBodyPPM();

        if (__instance is FungleShipStatus fungle)
        {
            MalumSabotageCheats.ProcessFungle(fungle);
        }
    }
}
