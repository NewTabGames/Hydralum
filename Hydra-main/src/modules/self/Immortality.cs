using System.Collections.Generic;
using Hazel;
using HarmonyLib;
using InnerNet;

namespace HydraMenu.modules.self
{
	internal class Immortality : Module
	{
		// The PlayerControl::CheckMurder function is the handler for CheckMurder RPCs. When the host of the lobby receives this RPC, it first checks
		// to make sure that the player who attempted to kill is an imposter and is alive, and then checks if the player who should be killed is alive, is not inside a vent, and is not on a ladder or platform
		// If everything goes smoothly, a MurderPlayer RPC with flag Succeeded is sent to all online players and the player killed
		// If one of the above checks fails, then a MurderPlayer RPC with flag FailedError is sent and the player is not killed
		// We can potentially use this as an immortality exploit by making the host think we are inside a vent
		// In theory, we should be able to send a GameDataTo message to the host with an EnterVent RPC which will make the host think we are inside a vent
		// but to every other player in the game we will still appear to be moving around
		// The biggest problem with this is that the CheckMurder RPC is server-authoritative, not host-authoritative, meaning that the checks we see in the PlayerControl::CheckMurder function may not actually be the case when the backend Among Us servers handle the CheckMurder RPC
		// The backend Among Us servers do check if the player who should be killed is inside a vent, but not through the EnterVent or ExitVent RPCs!
		// Instead they check if a player is inside a vent through the VentilationSystem system in the ShipStatus net object
		// When your Among Us client enters a vent, you first send an EnterVent RPC which makes your player walk towards a vent and then go inside a vent, and then your client sends an UpdateSystem RPC
		// for the ventilation system with an operation of Enter, which tells players that you are inside of a vent
		// This feature is used for the vent-cleaning feature to determine which players should be kicked out of a vent
		// but it also used by the backend Among Us servers to determine if a player is inside a vent when handling CheckMurder RPCs
		// So when the backend Among Us servers receives a CheckMurder RPC, it goes through a list of all net objects that exist for the given lobby, finds ShipStatus, gets the data for the VentilationSystem, and determines if a player is inside of a vent through there
		// Server authority here is actually helpful for us as our previous theory for immortality would make us immortal in the eyes of the host, meanwhile this will make us visible for all online players
		public Immortality() : base("Immortality") { }

		private static readonly int CUSTOM_VENT_ID = 50;

		private static Immortality Instance
		{
			get { return ModuleManager.immortality; }
		}

		// ---- Immortality for OTHER players ----
		// The local player is made immortal by the vent exploit above (module Enabled). To make ANOTHER
		// player immortal we spoof a VentilationSystem "Enter" for their id: the UpdateSystem RPC carries the
		// affected player as a NetObject, so a non-host can mark anyone as vented (the same hole AntiKick
		// guards against). Vent state resets on meetings/game load, so we re-send for everyone we've marked.
		private static readonly HashSet<byte> _others = new();
		// Per-player sequence id, mirroring what VentilationSystem.Update does for the local player. Vent
		// membership is a stream of Enter/Exit ops each tagged with a sid that must be a valid successor of
		// that player's SequenceBuffer.LastSid, or the op is dropped as stale. A single shared counter (the
		// old approach) is why turning immortality OFF didn't take: the Exit wasn't sequenced per-player so
		// it never un-vented anyone until a meeting wiped the whole vent state.
		private static readonly Dictionary<byte, ushort> _ventSid = new();
		private static bool _hooksReady;

		public static bool ImmortalAll { get; private set; }

		public static bool IsOther(byte pid) { return _others.Contains(pid); }

		public static bool IsImmortal(byte pid)
		{
			if(ImmortalAll) return true;
			if(_others.Contains(pid)) return true;
			if(Instance.Enabled && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == pid) return true;
			return false;
		}

		public static void SetEveryone(bool on)
		{
			EnsureHooks();
			if(PlayerControl.AllPlayerControls == null) return;

			ImmortalAll = on;
			Instance.Enabled = on; // handles our own immortality (OnEnable/OnDisable send the local vent update)

			foreach(var p in PlayerControl.AllPlayerControls)
			{
				if(p == null || p == PlayerControl.LocalPlayer || p.Data == null) continue;
				if(on) _others.Add(p.PlayerId); else _others.Remove(p.PlayerId);
				ApplyVent(p, on);
			}

			Hydra.notifications.Send("Immortality", on ? "Everyone is now immortal" : "Everyone is now mortal", 5);
		}

		public static void SetImmortal(PlayerControl p, bool on)
		{
			if(p == null || p.Data == null) return;
			EnsureHooks();

			if(p == PlayerControl.LocalPlayer)
			{
				Instance.Enabled = on; // local player uses the module's own vent exploit
				return;
			}

			if(on) _others.Add(p.PlayerId); else _others.Remove(p.PlayerId);
			ApplyVent(p, on);
		}

		// Subscribes a session-lived re-send so players we've marked stay immortal after meetings / new rounds,
		// independent of whether the module itself is enabled.
		private static void EnsureHooks()
		{
			if(_hooksReady) return;
			_hooksReady = true;
			EventCoordinator.OnGameLoad += ResendOthers;
			EventCoordinator.OnMeetingEnd += ResendOthers;
			EventCoordinator.OnPlayerMurder += OnKillAttempt; // alert on kill attempts vs us or immortal players
		}

		// Vent state resets on meetings/new rounds, so re-send for everyone still marked to keep them immortal.
		private static void ResendOthers()
		{
			// The game's per-player sequence buffers are reset with the vent state, so drop our high-water
			// marks too and re-baseline from OP_ID_START / the fresh buffers on the re-send below.
			_ventSid.Clear();

			if((!ImmortalAll && _others.Count == 0) || PlayerControl.AllPlayerControls == null) return;
			foreach(var p in PlayerControl.AllPlayerControls)
			{
				if(p == null || p == PlayerControl.LocalPlayer || p.Data == null) continue;
				if(ImmortalAll || _others.Contains(p.PlayerId)) ApplyVent(p, true);
			}
		}

		// Applies / removes immortality for one player by emitting a VentilationSystem operation for THEM -
		// the exact same mechanism the game's VentilationSystem.Update uses for the local player, just aimed
		// at another player's NetObject. Enter makes the server think they are vented (kills fail); Exit
		// un-vents them. Because the op is properly sequenced per player (see NextSid), the Exit is honoured
		// immediately, so toggling immortality OFF now actually works instead of waiting for a meeting.
		private static void ApplyVent(PlayerControl p, bool on)
		{
			SendVentOp(p, on ? VentilationSystem.Operation.Enter : VentilationSystem.Operation.Exit);
		}

		private static void SendVentOp(PlayerControl target, VentilationSystem.Operation op)
		{
			if(target == null || ShipStatus.Instance == null || AmongUsClient.Instance == null) return;

			VentilationSystem ventilation = GetVentilation();
			if(ventilation == null) return;

			ushort sid = NextSid(ventilation, target.PlayerId);

			if(AmongUsClient.Instance.AmHost)
			{
				// Host is authoritative for the ventilation net object: apply the op exactly as if we'd
				// received it from the target (updates PlayersInsideVents, records the sequenced op, marks
				// the system dirty), which then serialises out to the backend + every client on the next tick.
				MessageWriter payload = MessageWriter.Get(SendOption.Reliable);
				payload.Write(sid);
				payload.Write((byte)op);
				payload.Write((byte)CUSTOM_VENT_ID);

				MessageReader reader = MessageReader.Get(payload.ToByteArray(false));
				ShipStatus.Instance.UpdateSystem(SystemTypes.Ventilation, target, reader);
				payload.Recycle();
			}
			else
			{
				// Non-host: send the UpdateSystem RPC to the host, byte-for-byte like a real vent op.
				MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
					ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, -1);
				writer.Write((byte)SystemTypes.Ventilation);
				writer.WriteNetObject(target);
				writer.Write(sid);
				writer.Write((byte)op);
				writer.Write((byte)CUSTOM_VENT_ID);
				AmongUsClient.Instance.FinishRpcImmediately(writer);
			}
		}

		// The next valid sequence id for a given player's vent ops. The game accepts an op only if its sid is
		// a successor of that player's SequenceBuffer.LastSid, so we base off the live buffer when it exists
		// (and OP_ID_START when the player has never vented). We also keep our own per-player high-water mark
		// so several ops fired back-to-back (e.g. "immortal everyone") each get a distinct, increasing sid
		// before the host has echoed the previous one back.
		private static ushort NextSid(VentilationSystem ventilation, byte pid)
		{
			int baseSid = VentilationSystem.OP_ID_START;
			try
			{
				if(ventilation.SeqBuffers != null && ventilation.SeqBuffers.ContainsKey(pid))
				{
					SequenceBuffer<VentilationSystem.VentMoveInfo> buf = ventilation.SeqBuffers[pid];
					if(buf != null) baseSid = buf.LastSid;
				}
			}
			catch { }

			int tracked = _ventSid.TryGetValue(pid, out ushort t) ? t : 0;
			ushort next = (ushort)((baseSid > tracked ? baseSid : tracked) + 1);
			_ventSid[pid] = next;
			return next;
		}

		private static VentilationSystem GetVentilation()
		{
			try
			{
				if(ShipStatus.Instance == null || ShipStatus.Instance.Systems == null) return null;
				ISystemType systemType = ShipStatus.Instance.Systems[SystemTypes.Ventilation];
				return systemType != null ? systemType.Cast<VentilationSystem>() : null;
			}
			catch { return null; }
		}

		[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
		class BlockSendingUpdates
		{
			static bool Prefix(VentilationSystem.Operation op, int ventId)
			{
				if(Instance.Enabled && ventId != CUSTOM_VENT_ID && (op == VentilationSystem.Operation.Enter || op == VentilationSystem.Operation.Exit || op == VentilationSystem.Operation.Move))
				{
					// Hydra.Log.LogInfo($"Our client send VentilationSystem operation {op} for vent {ventId}. Resending Immortality RPC");
					// VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);

					Hydra.Log.LogInfo($"Our client sent VentilationSystem operation {op} for vent {ventId}, cancelling..");
					return false;
				}

				return true;
			}
		}

		private void OnGameLoad()
		{
			Hydra.Log.LogMessage($"A new instance of ShipStatus has spawned, sending the immortality RPC");
			VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
		}

		// Fires on every kill attempt. Alerts when the victim is us OR anyone we've made immortal.
		private static void OnKillAttempt(PlayerControl murderer, PlayerControl victim, MurderResultFlags flags)
		{
			if(murderer == null || victim == null) return;
			if(victim != PlayerControl.LocalPlayer && !IsImmortal(victim.PlayerId)) return;

			string killer = murderer.Data != null ? murderer.Data.PlayerName : "Someone";
			if(victim == PlayerControl.LocalPlayer)
			{
				Hydra.notifications.Send("Immortality", $"{killer} attempted to kill you!", 5);
			}
			else
			{
				string vic = victim.Data != null ? victim.Data.PlayerName : "someone";
				Hydra.notifications.Send("Immortality", $"{killer} attempted to kill {vic}!", 5);
			}
		}

		private void OnMeetingEnd()
		{
			if(PlayerControl.LocalPlayer.Data.IsDead) return;

			Hydra.Log.LogInfo("Meeting has ended, resending Immortality RPC to retain immortal status");
			VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
		}

		protected override void OnEnable()
		{
			EnsureHooks(); // session-lived kill alerts + re-send for immortal players
			EventCoordinator.OnGameLoad += OnGameLoad;
			EventCoordinator.OnMeetingEnd += OnMeetingEnd;

			if(PlayerControl.LocalPlayer != null)
			{
				Hydra.Log.LogInfo("Immortality was enabled, sending a VentilationSystem update with operation Enter");
				VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
			}
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnGameLoad -= OnGameLoad;
			EventCoordinator.OnMeetingEnd -= OnMeetingEnd;

			if(PlayerControl.LocalPlayer != null)
			{
				Hydra.Log.LogInfo("Immortality was disabled, sending a VentilationSystem update with operation Exit");
				VentilationSystem.Update(VentilationSystem.Operation.Exit, CUSTOM_VENT_ID);
			}
		}
	}
}