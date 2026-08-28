using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using UnityEngine;

namespace HydraMenu.network
{
	internal class BatchedMessage
	{
		public readonly MessageWriter writer;
		public readonly int targetClientId;
		private int msgCount = 0;

		public BatchedMessage(int targetClientId = (int)Constants.OwnerIds.Everyone)
		{
			writer = MessageWriter.Get(SendOption.Reliable);

			this.targetClientId = targetClientId;
			int gameId = AmongUsClient.Instance != null ? AmongUsClient.Instance.GameId : 0;
			if(targetClientId == (int)Constants.OwnerIds.Everyone)
			{
				writer.StartMessage(InnerNet.Tags.GameData);
				writer.Write(gameId);
			}
			else
			{
				writer.StartMessage(InnerNet.Tags.GameDataTo);
				writer.Write(gameId);
				writer.WritePacked(targetClientId);
			}
		}

		private bool IsGlobal => targetClientId == (int)Constants.OwnerIds.Everyone;

		private bool AmTarget => targetClientId == (AmongUsClient.Instance != null ? AmongUsClient.Instance.ClientId : -1);

		public void QueueDataFlag(uint netId, MessageWriter msg)
		{
			if (msg == null) return;
			msgCount++;
			writer.StartMessage((byte)GameDataTypes.DataFlag);
			writer.WritePacked(netId);
			writer.Write(msg, false);
			writer.EndMessage();
		}

		public void QueueSpawn(InnerNetObject netObject, int ownerId = -2, SpawnFlags flags = SpawnFlags.None)
		{
			if (netObject == null || AmongUsClient.Instance == null) return;
			msgCount++;
			SpawnGameDataMessage spawn = AmongUsClient.Instance.CreateSpawnMessage(netObject, ownerId, flags);
			if (spawn != null)
			{
				spawn.Serialize(writer);
			}
		}

		public void QueueCompleteTask(PlayerControl source, uint taskIndex)
		{
			if (source == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.CompleteTask(taskIndex);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.CompleteTask);
			writer.WritePacked(taskIndex);
			writer.EndMessage();
		}

		public void QueueSetName(PlayerControl source, string name)
		{
			if (source == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.SetName(name);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetName);
			writer.Write(source.NetId);
			writer.Write(name ?? "");
			writer.EndMessage();
		}

		public void QueueSetColor(PlayerControl source, byte color)
		{
			if (source == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.SetColor(color);
				if(AmTarget) return;
			}

			uint netId = source.Data != null ? source.Data.NetId : source.NetId;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetColor);
			writer.Write(netId);
			writer.Write(color);
			writer.EndMessage();
		}

		public void QueueMurderPlayer(PlayerControl source, PlayerControl target, MurderResultFlags result)
		{
			if (source == null || target == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.MurderPlayer(target, result);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.MurderPlayer);
			writer.WritePacked(target.NetId);
			writer.Write((int)result);
			writer.EndMessage();
		}

		public void QueueSnapTo(PlayerControl source, Vector2 position)
		{
			if (source == null || source.NetTransform == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.NetTransform.SnapTo(position, (ushort)(source.NetTransform.lastSequenceId + 1));
				if(AmTarget) return;
			}

			ushort seqId = (ushort)(source.NetTransform.lastSequenceId + 128);

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetTransform.NetId);
			writer.Write((byte)RpcCalls.SnapTo);
			NetHelpers.WriteVector2(position, writer);
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueCloseMeeting()
		{
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				if (MeetingHud.Instance != null) MeetingHud.Instance.Close();
				if(AmTarget) return;
			}

			if (MeetingHud.Instance == null) return;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(MeetingHud.Instance.NetId);
			writer.Write((byte)RpcCalls.CloseMeeting);
			writer.EndMessage();
		}

		public void QueueVotingComplete(MeetingHud.VoterState[] voteStates, NetworkedPlayerInfo ejectedPlayer, bool isTie, bool wasOverruled = false, ushort overruleNonce = 0)
		{
			if (voteStates == null) return;
			msgCount++;

			if((IsGlobal || AmTarget) && MeetingHud.Instance != null)
			{
				try
				{
					var vcMethod = typeof(MeetingHud).GetMethod(nameof(MeetingHud.VotingComplete));
					if (vcMethod != null)
					{
						var vcParams = vcMethod.GetParameters();
						if (vcParams.Length >= 5)
							vcMethod.Invoke(MeetingHud.Instance, new object[] { voteStates, ejectedPlayer, isTie, wasOverruled, overruleNonce });
						else
							vcMethod.Invoke(MeetingHud.Instance, new object[] { voteStates, ejectedPlayer, isTie });
					}
				}
				catch { }
				if(AmTarget) return;
			}

			if (MeetingHud.Instance == null) return;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(MeetingHud.Instance.NetId);
			writer.Write((byte)RpcCalls.VotingComplete);

			writer.WritePacked(voteStates.Length);

			foreach(MeetingHud.VoterState state in voteStates)
			{
				state.Serialize(writer);
			}

			writer.Write(ejectedPlayer != null ? ejectedPlayer.PlayerId : byte.MaxValue);
			writer.Write(isTie);
			writer.Write(wasOverruled);
			writer.Write(overruleNonce);

			writer.EndMessage();
		}

		public void QueueAddVote(int sourceId, int targetId)
		{
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				if (VoteBanSystem.Instance != null) VoteBanSystem.Instance.AddVote(sourceId, targetId);
				if(AmTarget) return;
			}

			if (VoteBanSystem.Instance == null) return;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(VoteBanSystem.Instance.NetId);
			writer.Write((byte)RpcCalls.AddVote);
			writer.Write(sourceId);
			writer.Write(targetId);
			writer.EndMessage();
		}

		public void QueueCloseDoors(SystemTypes door)
		{
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				if (ShipStatus.Instance != null) ShipStatus.Instance.CloseDoorsOfType(door);
				if(AmTarget) return;
			}

			if (ShipStatus.Instance == null) return;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(ShipStatus.Instance.NetId);
			writer.Write((byte)RpcCalls.CloseDoorsOfType);
			writer.Write((byte)door);
			writer.EndMessage();
		}

		public void QueueUpdateSystem(PlayerControl source, SystemTypes system, MessageWriter msg)
		{
			if (ShipStatus.Instance == null || source == null || msg == null) return;
			msgCount++;

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(ShipStatus.Instance.NetId);
			writer.Write((byte)RpcCalls.UpdateSystem);
			writer.Write((byte)system);
			writer.WriteNetObject(source);
			writer.Write(msg, false);
			writer.EndMessage();
		}

		public void QueueSetHatStr(PlayerControl source, string hat, byte seqId)
		{
			if (source == null) return;
			msgCount++;

			byte colorId = source.Data != null && source.Data.DefaultOutfit != null ? (byte)source.Data.DefaultOutfit.ColorId : (byte)0;

			if(IsGlobal || AmTarget)
			{
				source.SetHat(hat, colorId);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetHatStr);
			writer.Write(hat ?? "");
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueSetSkinStr(PlayerControl source, string skin, byte seqId)
		{
			if (source == null) return;
			msgCount++;

			byte colorId = source.Data != null && source.Data.DefaultOutfit != null ? (byte)source.Data.DefaultOutfit.ColorId : (byte)0;

			if(IsGlobal || AmTarget)
			{
				source.SetSkin(skin, colorId);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetSkinStr);
			writer.Write(skin ?? "");
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueSetPetStr(PlayerControl source, string pet, byte seqId)
		{
			if (source == null) return;
			msgCount++;

			byte colorId = source.Data != null && source.Data.DefaultOutfit != null ? (byte)source.Data.DefaultOutfit.ColorId : (byte)0;

			if(IsGlobal || AmTarget)
			{
				source.SetPet(pet, colorId);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetPetStr);
			writer.Write(pet ?? "");
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueSetVisorStr(PlayerControl source, string visor, byte seqId)
		{
			if (source == null) return;
			msgCount++;

			byte colorId = source.Data != null && source.Data.DefaultOutfit != null ? (byte)source.Data.DefaultOutfit.ColorId : (byte)0;

			if(IsGlobal || AmTarget)
			{
				source.SetVisor(visor, colorId);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetVisorStr);
			writer.Write(visor ?? "");
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueSetNameplateStr(PlayerControl source, string nameplate, byte seqId)
		{
			if (source == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.SetNamePlate(nameplate);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetNamePlateStr);
			writer.Write(nameplate ?? "");
			writer.Write(seqId);
			writer.EndMessage();
		}

		public void QueueSetRole(PlayerControl source, RoleTypes role, bool canOverride = false)
		{
			if (source == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.StartCoroutine(source.CoSetRole(role, canOverride));
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.SetRole);
			writer.Write((ushort)role);
			writer.Write(canOverride);
			writer.EndMessage();
		}

		public void QueueShapeshift(PlayerControl source, PlayerControl target, bool shouldAnimate)
		{
			if (source == null || target == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				source.Shapeshift(target, shouldAnimate);
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.Shapeshift);
			writer.WriteNetObject(target);
			writer.Write(shouldAnimate);
			writer.EndMessage();
		}

		public void QueueTriggerSpore(PlayerControl source, Mushroom mushroom)
		{
			if (source == null || mushroom == null) return;
			msgCount++;

			if(IsGlobal || AmTarget)
			{
				mushroom.TriggerSpores();
				if(AmTarget) return;
			}

			writer.StartMessage((byte)GameDataTypes.RpcFlag);
			writer.WritePacked(source.NetId);
			writer.Write((byte)RpcCalls.TriggerSpores);
			writer.Write(mushroom.Id);
			writer.EndMessage();
		}

		public void FinishBatch()
		{
			writer.EndMessage();
			if(msgCount > 0 && AmongUsClient.Instance != null)
			{
				AmongUsClient.Instance.SendOrDisconnect(writer);
			}
			writer.Recycle();
		}
	}
}