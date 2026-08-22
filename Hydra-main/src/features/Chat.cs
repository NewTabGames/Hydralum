using HarmonyLib;

namespace HydraMenu.features
{
	internal class Chat
	{
		[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
		public static class OnChat
		{
			public static bool LogChatMessages { get; set; } = true;
			public static bool ShowMessagesByGhosts { get; set; } = false;

			static void Postfix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
			{
				if(sourcePlayer == null || sourcePlayer.Data == null) return;

				if(LogChatMessages) Hydra.Log?.LogMessage($"[ChatLogger] {sourcePlayer.Data.PlayerName}: {chatText}");

				if(ShowMessagesByGhosts && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && !PlayerControl.LocalPlayer.Data.IsDead && sourcePlayer.Data.IsDead)
				{
					__instance.AddChatWarning($"{sourcePlayer.Data.PlayerName}\n{chatText}");
				}
			}
		}

		[HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
		public static class AlwaysVisibleChat
		{
			public static bool Enabled { get; set; } = true;

			static void Prefix(ref bool visible)
			{
				if(Enabled) visible = true;
			}
		}
	}
}