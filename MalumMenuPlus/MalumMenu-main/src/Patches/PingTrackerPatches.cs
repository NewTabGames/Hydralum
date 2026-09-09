using HarmonyLib;

namespace MalumMenu;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
public static class PingTracker_Update
{
    // When the Ping / FPS overlay is enabled we blank the game's built-in ping text (which the game
    // anchors off-centre, and which can show as a duplicate). MalumMenu then draws its own Ping/FPS line
    // truly centred on the screen instead - see MenuUI.OnGUI.
    public static void Postfix(PingTracker __instance)
    {
        try
        {
            if (__instance == null || __instance.text == null) return;

            if (CheatToggles.showPing || CheatToggles.showFps)
                __instance.text.text = "";
        }
        catch { }
    }
}
