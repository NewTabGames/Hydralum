using HydraMenu.anticheat;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class AnticheatSection : ISection
	{
		public AnticheatSection() : base("Anticheat") { }

		public override void Render()
		{
			bool prevAC = Anticheat.Enabled;
			Anticheat.Enabled = GUILayout.Toggle(Anticheat.Enabled, "Enable Hydra Anticheat");
			if (Anticheat.Enabled != prevAC) HydraConfig.Save();

			bool prevSpoofPlat = Anticheat.CheckSpoofedPlatforms;
			Anticheat.CheckSpoofedPlatforms = GUILayout.Toggle(Anticheat.CheckSpoofedPlatforms, "Flag Spoofed Platform Data");
			if (Anticheat.CheckSpoofedPlatforms != prevSpoofPlat) HydraConfig.Save();

			GUILayout.Space(5);
			GUILayout.Label("RPCs that should be checked by the anticheat:");
			foreach((RpcCalls rpcCall, RpcCheck handler) in Anticheat.RpcHandlers)
			{
				bool prevHandler = handler.Enabled;
				handler.Enabled = GUILayout.Toggle(handler.Enabled, $"{rpcCall}");
				if (handler.Enabled != prevHandler) HydraConfig.Save();
			}

			GUILayout.Space(5);
			GUILayout.Label("When a cheater is detected:");
			bool prevNotif = Anticheat.sendNotification;
			Anticheat.sendNotification = GUILayout.Toggle(Anticheat.sendNotification, "Send notification");
			if (Anticheat.sendNotification != prevNotif) HydraConfig.Save();

			bool prevDiscard = Anticheat.discardRpc;
			Anticheat.discardRpc = GUILayout.Toggle(Anticheat.discardRpc, "Discard RPC");
			if (Anticheat.discardRpc != prevDiscard) HydraConfig.Save();

			GUILayout.BeginHorizontal();
			GUILayout.Label($"Punish the player with: {Anticheat.punishment}");
			Anticheat.Punishments prevPunish = Anticheat.punishment;
			Anticheat.punishment = (Anticheat.Punishments)GUILayout.HorizontalSlider((float)Anticheat.punishment, 0, 3);
			if (Anticheat.punishment != prevPunish) HydraConfig.Save();
			GUILayout.EndHorizontal();
		}
	}
}