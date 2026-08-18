using HarmonyLib;

namespace HydraMenu.features
{
	internal class Chat
	{
		[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
		public static class OnChat
		{
			public static bool LogChatMessages { get; set; } = true;

			static void Postfix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
			{
				if(sourcePlayer == null || sourcePlayer.Data == null) return;

				if(LogChatMessages) Hydra.Log?.LogMessage($"[ChatLogger] {sourcePlayer.Data.PlayerName}: {chatText}");
			}
		}
	}
}