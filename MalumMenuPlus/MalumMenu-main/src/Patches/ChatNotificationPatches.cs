using HarmonyLib;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(ChatNotification), nameof(ChatNotification.SetUp))]
public static class ChatNotification_SetUp
{
    // Dark-mode the chat notification popup (the bubble that pops up in the lobby/in-game when chat is
    // closed and someone talks), matching the "Dark Mode Chat" setting used for the chat log bubbles.
    public static void Postfix(ChatNotification __instance)
    {
        if (!CheatToggles.chatDarkMode || __instance == null) return;

        try
        {
            if (__instance.background != null)
                __instance.background.color = new Color(0.13f, 0.13f, 0.16f, 1f);

            if (__instance.chatText != null)
                __instance.chatText.color = new Color(0.93f, 0.93f, 0.96f, 1f);
        }
        catch { }
    }
}
