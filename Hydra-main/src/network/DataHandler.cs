using AmongUs.InnerNet.GameDataMessages;
using HarmonyLib;
using Hazel;
using HydraMenu.anticheat;
using HydraMenu.modules;
using InnerNet;

namespace HydraMenu.network
{
	internal class DataHandler
	{
		// On Vanilla servers, messages are limited to at most 1201 bytes
		// On Modded Nikocat servers, messages are limited to at most 1400 bytes
		// Messages shouldn't greater than 1500 bytes anyway, as at that point messages will likely not be received by some clients due to exceeding MTU size
		// 1400 bytes seems to be the sweet spot, as it is enough to support modded servers, and also enough to prevent attacks with large messages
		public static readonly int MAX_MESSAGE_LENGTH = 1400;

		[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
		class HandleGameData
		{
			static bool Prefix(InnerNetClient __instance, MessageReader parentReader)
			{
				if(ModuleManager.antiOverload.Enabled && ModuleManager.antiOverload.BlockLargeGameMessages && parentReader.Length > MAX_MESSAGE_LENGTH)
				{
					parentReader.Recycle();
					return false;
				}

				try
				{
					while(parentReader.BytesRemaining > 0)
					{
						MessageReader reader = parentReader.ReadMessageAsNewBuffer();
						HandleGameDataInner(__instance, reader, ++__instance.msgNum);
					}
				}
				finally
				{
					parentReader.Recycle();
				}

				return false;
			}
		}

		public static void HandleGameDataInner(InnerNetClient innerNetClient, MessageReader reader, int msgNum)
		{
			GameDataTypes type = (GameDataTypes)reader.Tag;

			// A menu handshake can arrive as a GameData message with a non-vanilla tag, which the invalid-message
			// filter below would silently drop. Surface it for detection first (log-only - custom tags are also
			// used by modded servers, so we don't auto-flag). Runs regardless of whether the filter is enabled.
			if(type == GameDataTypes.Invalid || type == (GameDataTypes)3 || type > GameDataTypes.ReadyFlag)
				ModDetection.ObserveCustomTag((byte)type);

			if(ModuleManager.antiOverload.Enabled && ModuleManager.antiOverload.BlockInvalidGameDataMessages && (type == GameDataTypes.Invalid || type == (GameDataTypes)3 || type > GameDataTypes.ReadyFlag))
			{
				reader.Recycle();
				return;
			}

			bool isValid = Anticheat.HandleGameData(type, reader);
			if(!isValid)
			{
				reader.Recycle();
				return;
			}

			innerNetClient.StartCoroutine(innerNetClient.HandleGameDataInner(reader, msgNum));
		}
	}
}