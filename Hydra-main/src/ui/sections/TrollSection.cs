using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using HydraMenu.assets;
using HydraMenu.modules;
using HydraMenu.network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class TrollSection : Section
	{
		public TrollSection() : base("Troll") { }

		private int selectedVent = 0;
		private string expirationInput = "";
		private bool typingExpiration = false;
		private readonly System.Random rnd = new System.Random();

		public override void Render()
		{
			if(PlayerControl.LocalPlayer == null)
			{
				GUILayout.Label("You are not currently in a game, these options will not work.");
			}

			ModuleManager.blackout.Enabled = GUILayout.Toggle(ModuleManager.blackout.Enabled, "Blackout (Use In Lobby)");
			ModuleManager.crashLobby.Enabled = GUILayout.Toggle(ModuleManager.crashLobby.Enabled, "Queue Lobby Crash");
			Hydra.routines.autoTriggerSpores.Enabled = GUILayout.Toggle(Hydra.routines.autoTriggerSpores.Enabled, "Auto Trigger Spores");
			ModuleManager.disableCameras.Enabled = GUILayout.Toggle(ModuleManager.disableCameras.Enabled, "Disable Security Cameras");

			bool immortalAll = HydraMenu.modules.self.Immortality.ImmortalAll;
			bool newImmortalAll = GUILayout.Toggle(immortalAll, "Make Everyone Immortal");
			if(newImmortalAll != immortalAll) HydraMenu.modules.self.Immortality.SetEveryone(newImmortalAll);

			Hydra.routines.glitterBomb.Enabled = GUILayout.Toggle(Hydra.routines.glitterBomb.Enabled, "Glitterbomb");

			if(GUILayout.Button("Kick All Players"))
			{
				Hydra.Log.LogInfo($"Sending Enter ventilation system update to all players");

				MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
				writer.Write((ushort)0);
				writer.Write((byte)VentilationSystem.Operation.Enter);
				writer.Write((byte)0);

				BatchedMessage batch = new BatchedMessage();
				batch.QueueUpdateSystem(PlayerControl.LocalPlayer, SystemTypes.Ventilation, writer);
				batch.FinishBatch();

				writer.Recycle();

				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					if(player == PlayerControl.LocalPlayer || player.OwnerId == AmongUsClient.Instance.HostId) continue;

					Utilities.KickPlayer(player, true);
				}
			}

			if(GUILayout.Button("Copy Random Player"))
			{
				PlayerControl randomPl = Utilities.GetRandomPlayer();
				Utilities.CopyPlayer(randomPl);
			}

			if(GUILayout.Button("Trigger All Spores"))
			{
				if(Utilities.GetCurrentMap() != MapNames.Fungle)
				{
					Hydra.notifications.Send("Trigger Spores", "This option only works on the Fungle map.");
				}
				else
				{
					FungleShipStatus shipStatus = ShipStatus.Instance.Cast<FungleShipStatus>();

					BatchedMessage batch = new BatchedMessage();

					foreach(Mushroom mushroom in shipStatus.sporeMushrooms.Values)
					{
						batch.QueueTriggerSpore(PlayerControl.LocalPlayer, mushroom);
					}

					batch.FinishBatch();

					Hydra.notifications.Send("Trigger Spores", "All spores have been triggered.", 5);
				}
			}

			if(GUILayout.Button("Deplete HnS Timer"))
			{
				AmongUsClient.Instance.StartCoroutine(DepleteSeekTimer().WrapToIl2Cpp());
			}

			SortedDictionary<int, string> vents = MapAssets.GetVents();

			GUILayout.Space(5);
			GUILayout.Label($"Vent Teleport:");
			Hydra.routines.teleportSpammer.Enabled = Controls.GlobalPlayerSpecificToggle("Teleport Flooder", Hydra.routines.teleportSpammer.targets);
			Hydra.routines.teleportSpammer.excludeSelf = GUILayout.Toggle(Hydra.routines.teleportSpammer.excludeSelf, "Exclude Yourself");

			GUILayout.Label($"Teleport everyone to vent: {vents.GetValueOrDefault(selectedVent, "N/A")}");
			selectedVent = Controls.HorizontalVentSlider(vents, selectedVent);

			if(GUILayout.Button("Teleport to Vent"))
			{
				foreach(PlayerControl player in PlayerControl.AllPlayerControls)
				{
					Teleporter.TeleportToVent(player, selectedVent);
				}
			}

			if(GUILayout.Button("Teleport to Random Vent"))
			{
				if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null && ShipStatus.Instance.AllVents.Count > 0)
				{
					foreach(PlayerControl player in PlayerControl.AllPlayerControls)
					{
						if(Hydra.routines.teleportSpammer.excludeSelf && player == PlayerControl.LocalPlayer) continue;

						int ventId = rnd.Next(0, ShipStatus.Instance.AllVents.Count);

						Teleporter.TeleportToVent(player, ventId);
					}
				}
			}

			GUILayout.Space(5);
			// Automatically close and open all doors at a set interval
			GUILayout.Label("Door Troller:");
			Hydra.routines.doorTroller.Enabled = GUILayout.Toggle(Hydra.routines.doorTroller.Enabled, "Enabled");

			GUILayout.Label($"Lock and Unlock Delay: {Hydra.routines.doorTroller.LockAndUnlockDelay:F2}s");
			Hydra.routines.doorTroller.LockAndUnlockDelay = GUILayout.HorizontalSlider(Hydra.routines.doorTroller.LockAndUnlockDelay, 0.1f, 2.0f);

			GUILayout.Space(5);
			GUILayout.Label("Auto Expose Impostors:");
			ModuleManager.autoExposeImpostors.Enabled = GUILayout.Toggle(ModuleManager.autoExposeImpostors.Enabled, "Enabled");
			ModuleManager.autoExposeImpostors.ExposeOnMurder = GUILayout.Toggle(ModuleManager.autoExposeImpostors.ExposeOnMurder, "Expose On Murder");
			ModuleManager.autoExposeImpostors.ExposeOnShapeshift = GUILayout.Toggle(ModuleManager.autoExposeImpostors.ExposeOnShapeshift, "Expose On Shapeshift");
			ModuleManager.autoExposeImpostors.ExposeOnPhantom = GUILayout.Toggle(ModuleManager.autoExposeImpostors.ExposeOnPhantom, "Expose On Phantom");

			// Host-authoritative features: these only take effect in modded / host-authoritative lobbies
			// (a vanilla Innersloth host rejects them), so they are grouped here under their own heading.
			GUILayout.Space(8);
			GUILayout.Label("Host-Authoritative (modded lobbies only):");
			ModuleManager.disableZipline.Enabled = GUILayout.Toggle(ModuleManager.disableZipline.Enabled, "Disable Zipline");
			ModuleManager.disableSporeTriggers.Enabled = GUILayout.Toggle(ModuleManager.disableSporeTriggers.Enabled, "Disable Spore Triggers");
			Hydra.routines.spamInfluencerCues.Enabled = GUILayout.Toggle(Hydra.routines.spamInfluencerCues.Enabled, "Spam Influencer Cues");

			// Custom lobby expiration timer. GUILayout.TextField is stripped under IL2CPP (crashes the layout),
			// so this is a click-to-type field backed by manual key capture, like the Spoofer's exact-level field.
			GUILayout.BeginHorizontal();
			string expLabel = typingExpiration
				? (string.IsNullOrEmpty(expirationInput) ? "Type a timer, then Enter..." : expirationInput + "_")
				: "Lobby Expiration: " + (string.IsNullOrEmpty(expirationInput) ? "(click to type)" : expirationInput);
			if(GUILayout.Button(expLabel)) typingExpiration = !typingExpiration;
			if(GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
			{
				ApplyExpirationTimer();
				typingExpiration = false;
			}
			GUILayout.EndHorizontal();
			if(typingExpiration) CaptureExpirationInput();
		}

		// Builds the typed expiration timer from raw GUI key events (digits only). Enter/Set applies it; Escape cancels.
		private void CaptureExpirationInput()
		{
			var e = Event.current;
			if(e == null || e.type != EventType.KeyDown) return;

			if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
			{
				ApplyExpirationTimer();
				typingExpiration = false;
				e.Use();
				return;
			}

			if(e.keyCode == KeyCode.Escape)
			{
				typingExpiration = false;
				e.Use();
				return;
			}

			if(e.keyCode == KeyCode.Backspace)
			{
				if(!string.IsNullOrEmpty(expirationInput))
					expirationInput = expirationInput.Substring(0, expirationInput.Length - 1);
				e.Use();
				return;
			}

			char c = e.character;
			// Digits only, capped at 9 so the parsed value stays inside int.
			if(char.IsDigit(c) && expirationInput.Length < 9)
			{
				expirationInput += c;
				e.Use();
			}
		}

		private void ApplyExpirationTimer()
		{
			if(string.IsNullOrEmpty(expirationInput) || LobbyBehaviour.Instance == null) return;

			if(int.TryParse(expirationInput, out int timer))
			{
				BatchedMessage batch = new BatchedMessage();
				batch.QueueLobbyTimeExpiring(timer);
				batch.FinishBatch();
			}
		}

		// In Hide and Seek, completing a task will reduce the HnS hide timer depending on the length of the task (short, common, or long)
		// The problem is the game reduces the task timer even if we have already completed the task
		// so we can spam the CompleteTask RPC with the same task, and reduce the task timer to zero seconds
		private IEnumerator DepleteSeekTimer()
		{
			if(!GameManager.Instance.IsHideAndSeek())
			{
				Hydra.notifications.Send("Deplete HnS Timer", "This feature can only be used in Hide and Seek.");
				yield break;
			}

			LogicGameFlowHnS gameFlow = GameManager.Instance.LogicFlow.Cast<LogicGameFlowHnS>();
			LogicOptionsHnS logicOptions = GameManager.Instance.LogicOptions.Cast<LogicOptionsHnS>();

			if(AmongUsClient.Instance.AmHost)
			{
				gameFlow.AdjustEscapeTimer(gameFlow.currentHideTime, true);
				yield break;
			}

			if(gameFlow.IsFinalCountdown)
			{
				Hydra.notifications.Send("Deplete HnS Timer", "This feature cannot be used during the final hide time.");
				yield break;
			}

			NormalPlayerTask applicableTask = null;
			foreach(PlayerTask task in PlayerControl.LocalPlayer.myTasks)
			{
				NormalPlayerTask normalTask = task.TryCast<NormalPlayerTask>();
				if(normalTask == null) continue;

				applicableTask = normalTask;

				// Prefer a Long task: a longer task deducts more time per completion, so fewer CompleteTask RPCs are needed.
				if(normalTask.Length == NormalPlayerTask.TaskLength.Long) break;
			}

			if(applicableTask == null)
			{
				Hydra.notifications.Send("Deplete HnS Timer", "This feature requires you to have at least one task.");
				yield break;
			}

			float completeDeduction;
			switch(applicableTask.Length)
			{
				case NormalPlayerTask.TaskLength.None:
				case NormalPlayerTask.TaskLength.Common:
				default:
					completeDeduction = logicOptions.GetCommonTaskTimeValue();
					break;

				case NormalPlayerTask.TaskLength.Short:
					completeDeduction = logicOptions.GetShortTaskTimeValue();
					break;

				case NormalPlayerTask.TaskLength.Long:
					completeDeduction = logicOptions.GetLongTaskTimeValue();
					break;
			}

			int requiredCompletions = (int)Math.Ceiling(gameFlow.currentHideTime / completeDeduction);

			Hydra.Log.LogInfo($"Current escape time is {gameFlow.currentHideTime} and each task completion reduces the timer by {completeDeduction}s. We need to send the CompleteTask RPC {requiredCompletions} times to deplete the HnS timer.");

			BatchedMessage batch = new BatchedMessage();

			for(int i = 0; i <= requiredCompletions; i++)
			{
				// The message packing limit for non-hosts should be ten, but the Among Us anticheat disconnects us if we have more than six CompleteTask RPCs in a single batch
				if(batch.msgCount >= 6)
				{
					batch.FinishBatch();
					batch = new BatchedMessage();

					yield return Effects.Wait(0.05f);
				}

				batch.QueueCompleteTask(PlayerControl.LocalPlayer, applicableTask.Id);
			}

			batch.FinishBatch();
		}
	}
}
