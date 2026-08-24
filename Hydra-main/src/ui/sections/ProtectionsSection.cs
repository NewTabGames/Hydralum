using HydraMenu.features;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class ProtectionsSection : ISection
	{
		public ProtectionsSection() : base("Protections") { }
		public override void Render()
		{
			// Network
			bool prevDTLS = Protections.ForceDTLS.Enabled;
			Protections.ForceDTLS.Enabled = GUILayout.Toggle(Protections.ForceDTLS.Enabled, "Force enable DTLS to encrypt network data");
			if (Protections.ForceDTLS.Enabled != prevDTLS) HydraConfig.Save();

			bool prevBlockTp = Protections.BlockServerTeleports.Enabled;
			Protections.BlockServerTeleports.Enabled = GUILayout.Toggle(Protections.BlockServerTeleports.Enabled, "Block position updates from server");
			if (Protections.BlockServerTeleports.Enabled != prevBlockTp) HydraConfig.Save();

			bool prevBlockUnauth = Protections.BlockUnauthorizedSystemUpdates;
			Protections.BlockUnauthorizedSystemUpdates = GUILayout.Toggle(Protections.BlockUnauthorizedSystemUpdates, "Block unauthorized system updates");
			if (Protections.BlockUnauthorizedSystemUpdates != prevBlockUnauth) HydraConfig.Save();

			// Overloads
			bool prevBlockLarge = Protections.BlockLargeGameMessages;
			Protections.BlockLargeGameMessages = GUILayout.Toggle(Protections.BlockLargeGameMessages, "Block large game messages");
			if (Protections.BlockLargeGameMessages != prevBlockLarge) HydraConfig.Save();

			bool prevBlockInvalid = Protections.BlockInvalidGameDataMessages;
			Protections.BlockInvalidGameDataMessages = GUILayout.Toggle(Protections.BlockInvalidGameDataMessages, "Block invalid game data message types");
			if (Protections.BlockInvalidGameDataMessages != prevBlockInvalid) HydraConfig.Save();

			bool prevHardened = Protections.HardenedReadPackedUInt.Enabled;
			Protections.HardenedReadPackedUInt.Enabled = GUILayout.Toggle(Protections.HardenedReadPackedUInt.Enabled, "Use hardened packed int deserializer");
			if (Protections.HardenedReadPackedUInt.Enabled != prevHardened) HydraConfig.Save();

			bool prevMem = Protections.MemoryAllocationOverload.Enabled;
			Protections.MemoryAllocationOverload.Enabled = GUILayout.Toggle(Protections.MemoryAllocationOverload.Enabled, "Protect against VotingComplete overloads");
			if (Protections.MemoryAllocationOverload.Enabled != prevMem) HydraConfig.Save();

			bool prevBypass = Protections.BypassShapeshiftRatelimits.Enabled;
			Protections.BypassShapeshiftRatelimits.Enabled = GUILayout.Toggle(Protections.BypassShapeshiftRatelimits.Enabled, "Bypass ratelimits for Shapeshift RPC");
			if (Protections.BypassShapeshiftRatelimits.Enabled != prevBypass) HydraConfig.Save();

			bool prevVotekick = Protections.Votekicks.Enabled;
			Protections.Votekicks.Enabled = GUILayout.Toggle(Protections.Votekicks.Enabled, "Prevent being votekicked as host");
			if (Protections.Votekicks.Enabled != prevVotekick) HydraConfig.Save();

			bool prevNonHost = Protections.ProtectAgainstNonHostKickExploit;
			Protections.ProtectAgainstNonHostKickExploit = GUILayout.Toggle(Protections.ProtectAgainstNonHostKickExploit, "Protect against non-host kick exploit");
			if (Protections.ProtectAgainstNonHostKickExploit != prevNonHost) HydraConfig.Save();
		}
	}
}