using HarmonyLib;
using System;
using UnityEngine;
using System.Text.RegularExpressions;

namespace MalumMenu;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class ChatController_AddChat
{
	// Prefix patch of ChatController.AddChat to receive ghost messages if CheatSettings.seeGhosts is enabled even if LocalPlayer is alive
	// Basically does what the original method did with the required modifications
	public static bool Prefix(PlayerControl sourcePlayer, ref string chatText, bool censor, ChatController __instance)
    {
        // Chat Log: snapshot the raw message before any timestamp/formatting is applied below.
        ChatLogRecorder.Capture(sourcePlayer, chatText);

        if (CheatToggles.chatTimestamps && !string.IsNullOrEmpty(chatText))
        {
            // DateTime.Now is already the PC's local time; the toggle just picks 24-hour vs 12-hour display.
            string timeStr = DateTime.Now.ToString(CheatToggles.chatTimestamp24hr ? "HH:mm:ss" : "h:mm:ss tt");
            // Tuck the timestamp into the bottom corner opposite the avatar: your own messages sit on the
            // right (avatar right), so their timestamp goes bottom-left; everyone else's goes bottom-right.
            string tsAlign = (sourcePlayer != null && sourcePlayer.AmOwner) ? "left" : "right";
            chatText = $"{chatText}\n<align=\"{tsAlign}\"><color=#aaaaaa><size=60%>[{timeStr}]</size></color></align>";
        }

        if (!sourcePlayer || !PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.Data == null) return true;

		// Simply run original method if seeGhosts is disabled or LocalPlayer already dead
        if (!CheatToggles.seeGhosts || PlayerControl.LocalPlayer.Data.IsDead) return true;

		NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
		NetworkedPlayerInfo data2 = sourcePlayer.Data;

		if (data2 == null || data == null) return true; // Remove isDead check for LocalPlayer

		ChatBubble pooledBubble = __instance.GetPooledBubble();

		try
		{
			pooledBubble.transform.SetParent(__instance.scroller.Inner);
			pooledBubble.transform.localScale = Vector3.one;
			bool flag = sourcePlayer == PlayerControl.LocalPlayer;
			if (flag)
			{
				pooledBubble.SetRight();
			}
			else
			{
				pooledBubble.SetLeft();
			}
			bool didVote = MeetingHud.Instance && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
			pooledBubble.SetCosmetics(data2);
			__instance.SetChatBubbleName(pooledBubble, data2, data2.IsDead, didVote, PlayerNameColor.Get(data2), null);
			if (censor && AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat)
			{
				chatText = BlockedWords.CensorWords(chatText, false);
			}
			pooledBubble.SetText(chatText);
			pooledBubble.AlignChildren();
			__instance.AlignAllBubbles();
			if (!__instance.IsOpenOrOpening && __instance.notificationRoutine == null)
			{
				__instance.notificationRoutine = __instance.StartCoroutine(__instance.BounceDot());
			}
			if (!flag && !__instance.IsOpenOrOpening)
			{
				SoundManager.Instance.PlaySound(__instance.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
				__instance.chatNotification.SetUp(sourcePlayer, chatText);
			}
		}
		catch (Exception message)
		{
			ChatController.Logger.Error(message.ToString(), null);
			__instance.chatBubblePool.Reclaim(pooledBubble);
		}

        return false; // Skips the original method completly
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update
{
    // Postfix patch of ChatController.Update to unlock longer message length and update live color tags
    public static void Postfix(ChatController __instance)
    {
        if (__instance?.freeChatField?.textArea != null)
        {
            // Chat input is capped at 120 — the highest length that reliably avoids anticheat kicks.
            // (Anything above 120 gets you kicked no matter what, so it isn't offered.)
            __instance.freeChatField.textArea.characterLimit = 120;
        }

        // Dark Mode Chat: recolor the input bar(s) to match the darkened bubbles. Applied both
        // ways because the input field persists (not pooled), so we must restore it when off.
        try
        {
            bool dark = CheatToggles.chatDarkMode;
            Color inputBg = dark ? new Color(0.13f, 0.13f, 0.16f, 1f) : Color.white;
            Color inputText = dark ? new Color(0.93f, 0.93f, 0.96f, 1f) : Color.black;

            if (__instance?.freeChatField != null)
            {
                AbstractChatInputField field = __instance.freeChatField;
                if (field.background != null)
                    field.background.color = inputBg;
                if (__instance.freeChatField.textArea != null && __instance.freeChatField.textArea.outputText != null)
                    __instance.freeChatField.textArea.outputText.color = inputText;
            }

            if (__instance?.quickChatField != null)
            {
                AbstractChatInputField qfield = __instance.quickChatField;
                if (qfield.background != null)
                    qfield.background.color = inputBg;
            }
        }
        catch { }

        if (__instance?.scroller?.Inner != null)
        {
            for (int i = 0; i < __instance.scroller.Inner.childCount; i++)
            {
                var child = __instance.scroller.Inner.GetChild(i);
                if (child == null) continue;
                var bubble = child.GetComponent<ChatBubble>();
                if (bubble != null)
                {
                    MalumESP.UpdateChatBubbleColorTag(bubble);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class ChatController_SendChat
{
    // Postfix patch of ChatController.SendChat to unlock lower chat rate limits
    public static void Postfix(ChatController __instance)
    {
        if (!CheatToggles.lowerRateLimits) return;

		if (__instance.timeSinceLastMessage == 0f)
		{
			// Decreasing rate limit by 1 sec max still avoids anticheat kicks
			__instance.timeSinceLastMessage += 1f;
		}
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
public static class ChatController_SendFreeChat
{
    // Prefix patch of ChatController.SendFreeChat to allow sending URLs without being censored
    public static bool Prefix(ChatController __instance)
    {
		// Only works if CheatSettings.bypassUrlBlock is enabled
        if (!CheatToggles.bypassUrlBlock) return true;
        if (PlayerControl.LocalPlayer == null || __instance?.freeChatField == null) return true;

        string text = __instance.freeChatField.Text;

        // Replace periods in URLs and email addresses with commas to avoid censorship
        string modifiedText = CensorUrlsAndEmails(text);

        ChatController.Logger.Debug("SendFreeChat () :: Sending message: '" + modifiedText + "'", null);
        PlayerControl.LocalPlayer.RpcSendChat(modifiedText);

        return false;
    }

    private static string CensorUrlsAndEmails(string text)
    {
        // Regular expression pattern to match URLs and email addresses
        string pattern = @"(http[s]?://)?([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}(/[\w-./?%&=]*)?|([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)";
        Regex regex = new Regex(pattern);

        // Censor periods in each match
        return regex.Replace(text, match =>
        {
            var censored = match.Value;
            censored = censored.Replace('.', ',');
            return censored;
        });
    }
}
