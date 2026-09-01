using AmongUs.Data;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HydraMenu.assets;
using HydraMenu.modules;
using HydraMenu.network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class SelfSection : Section
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

			ModuleManager.alwaysShowTaskAnimations.Enabled = GUILayout.Toggle(ModuleManager.alwaysShowTaskAnimations.Enabled, "Always Show Task Animations");
			ModuleManager.immortality.Enabled = GUILayout.Toggle(ModuleManager.immortality.Enabled, "Become Immortal");
			ModuleManager.noLadderCooldown.Enabled = GUILayout.Toggle(ModuleManager.noLadderCooldown.Enabled, "No Ladder Cooldown");
			ModuleManager.noZiplineCooldown.Enabled = GUILayout.Toggle(ModuleManager.noZiplineCooldown.Enabled, "No Zipline Cooldown");
			ModuleManager.unlimitedMeetings.Enabled = GUILayout.Toggle(ModuleManager.unlimitedMeetings.Enabled, "Unlimited Meetings");
			ModuleManager.updateStatsFreeplay.Enabled = GUILayout.Toggle(ModuleManager.updateStatsFreeplay.Enabled, "Update Stats in Freeplay");

			GUILayout.Space(5);
			GUILayout.Label("Avatar Controls:");
			if(GUILayout.Button("Randomize Avatar"))
			{
				if(AmongUsClient.Instance.AmConnected)
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
				PlayerControl.LocalPlayer.CmdCheckColor((byte)Utilities.GetRandomUnusedColor());
			}

			if(GUILayout.Button("Restore Avatar"))
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