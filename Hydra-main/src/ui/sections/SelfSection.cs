using AmongUs.Data;
using HydraMenu.features;
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

			Self.UpdateStatsFreeplay.Enabled = GUILayout.Toggle(Self.UpdateStatsFreeplay.Enabled, "Update Stats in Freeplay");
			Immortality.Enabled = GUILayout.Toggle(Immortality.Enabled, "Become Immortal");
			Self.NoLadderCooldown.Enabled = GUILayout.Toggle(Self.NoLadderCooldown.Enabled, "No Ladder Cooldown");
			Self.UnlimitedMeetings.enabled = GUILayout.Toggle(Self.UnlimitedMeetings.enabled, "Unlimited Meetings");

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

			if(GUILayout.Button("Copy Random Player"))
			{
				PlayerControl randomPl = Utilities.GetRandomPlayer();
				Utilities.CopyPlayer(randomPl);
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