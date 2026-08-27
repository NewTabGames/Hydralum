using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HydraMenu.assets;
using HydraMenu.features;
using HydraMenu.network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class SelfSection : ISection
	{
		public SelfSection() : base("Self") { }

		public override void Render()
		{
			if(PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
			{
				GUILayout.Label("You are not currently in a game, these options will not work.");
			}
			else
			{
				GUILayout.Label($"Role: {PlayerControl.LocalPlayer.Data.RoleType}");
			}

			bool prevFreeplay = Self.UpdateStatsFreeplay.Enabled;
			Self.UpdateStatsFreeplay.Enabled = GUILayout.Toggle(Self.UpdateStatsFreeplay.Enabled, "Update Stats in Freeplay");
			if (Self.UpdateStatsFreeplay.Enabled != prevFreeplay) HydraConfig.Save();

			bool prevImmortal = Immortality.Enabled;
			Immortality.Enabled = GUILayout.Toggle(Immortality.Enabled, "Become Immortal");
			if (Immortality.Enabled != prevImmortal) HydraConfig.Save();

			bool prevTaskAnim = Self.AlwaysShowTaskAnimations;
			Self.AlwaysShowTaskAnimations = GUILayout.Toggle(Self.AlwaysShowTaskAnimations, "Always Show Task Animations");
			if (Self.AlwaysShowTaskAnimations != prevTaskAnim) HydraConfig.Save();

			bool prevLadder = Self.NoLadderCooldown.Enabled;
			Self.NoLadderCooldown.Enabled = GUILayout.Toggle(Self.NoLadderCooldown.Enabled, "No Ladder Cooldown");
			if (Self.NoLadderCooldown.Enabled != prevLadder) HydraConfig.Save();

			bool prevMeetings = Self.UnlimitedMeetings.enabled;
			Self.UnlimitedMeetings.enabled = GUILayout.Toggle(Self.UnlimitedMeetings.enabled, "Unlimited Meetings");
			if (Self.UnlimitedMeetings.enabled != prevMeetings) HydraConfig.Save();

			bool prevMoveInVents = Self.MoveModifier.MoveInVents;
			Self.MoveModifier.MoveInVents = GUILayout.Toggle(Self.MoveModifier.MoveInVents, "Walk In Vents");
			if (Self.MoveModifier.MoveInVents != prevMoveInVents) HydraConfig.Save();

			if(GUILayout.Button("Call Meeting"))
			{
				if (PlayerControl.LocalPlayer != null)
					Utilities.AttemptStartMeeting(PlayerControl.LocalPlayer, null);
			}

			GUILayout.Space(5);
			GUILayout.Label("Avatar Controls:");
			if(GUILayout.Button("Randomize Avatar"))
			{
				if(AmongUsClient.Instance != null && AmongUsClient.Instance.AmConnected)
				{
					Utilities.RandomizePlayer(true);

					Hydra.notifications.Send("Player Randomizer", "Your avatar has been randomized for this game.", 5);
				}
				else
				{
					Utilities.RandomizePlayer();

					Hydra.notifications.Send("Player Randomizer", "Your name and avatar has been randomized.", 5);
				}
			}

			if(GUILayout.Button("Randomize Color"))
			{
				if (PlayerControl.LocalPlayer != null)
					PlayerControl.LocalPlayer.CmdCheckColor((byte)Utilities.GetRandomUnusedColor());
			}

			if(GUILayout.Button("Restore Avatar"))
			{
				if (PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.CmdCheckColor(DataManager.Player.Customization.Color);
					PlayerControl.LocalPlayer.RpcSetHat(DataManager.Player.Customization.Hat);
					PlayerControl.LocalPlayer.RpcSetVisor(DataManager.Player.Customization.Visor);
					PlayerControl.LocalPlayer.RpcSetSkin(DataManager.Player.Customization.Skin);
					PlayerControl.LocalPlayer.RpcSetPet(DataManager.Player.Customization.Pet);
				}
			}
		}
	}
}