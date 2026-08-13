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
			if(PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
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
				if(AmongUsClient.Instance != null && AmongUsClient.Instance.AmConnected)
				{
					Utilities.RandomizePlayer(true);
					Hydra.notifications?.Send("Player Randomizer", "Your avatar has been randomized for this game.", 5);
				}
				else
				{
					Utilities.RandomizePlayer();
					Hydra.notifications?.Send("Player Randomizer", "Your name and avatar has been randomized.", 5);
				}
			}

			if(GUILayout.Button("Randomize Color"))
			{
				if(PlayerControl.LocalPlayer != null)
				{
					PlayerControl.LocalPlayer.CmdCheckColor((byte)Utilities.GetRandomUnusedColor());
				}
			}

			if(GUILayout.Button("Copy Random Player"))
			{
				PlayerControl randomPl = Utilities.GetRandomPlayer();
				if(randomPl != null)
				{
					Utilities.CopyPlayer(randomPl);
				}
			}

			if(GUILayout.Button("Restore Avatar"))
			{
				Utilities.RevertOutfit();
			}

			GUILayout.Space(10);
			GUILayout.Label("<b>Cosmetic Presets Manager</b>");

			bool prevAutoApply = CosmeticPresetManager.AutoApplyOnJoin;
			CosmeticPresetManager.AutoApplyOnJoin = GUILayout.Toggle(CosmeticPresetManager.AutoApplyOnJoin, " Auto-Apply Selected Preset on Join");
			if (prevAutoApply != CosmeticPresetManager.AutoApplyOnJoin)
			{
				CosmeticPresetManager.SaveToConfig();
			}

			if (CosmeticPresetManager.Presets.Count > 0)
			{
				CosmeticPresetManager.SelectedPresetIndex = Mathf.Clamp(CosmeticPresetManager.SelectedPresetIndex, 0, CosmeticPresetManager.Presets.Count - 1);
				var currentPreset = CosmeticPresetManager.Presets[CosmeticPresetManager.SelectedPresetIndex];

				GUILayout.Label($"Preset ({CosmeticPresetManager.SelectedPresetIndex + 1}/{CosmeticPresetManager.Presets.Count}): <b>{currentPreset.Name}</b>");

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("< Prev", GUILayout.Width(60)))
				{
					CosmeticPresetManager.SelectedPresetIndex = (CosmeticPresetManager.SelectedPresetIndex - 1 + CosmeticPresetManager.Presets.Count) % CosmeticPresetManager.Presets.Count;
				}
				if (GUILayout.Button("Next >", GUILayout.Width(60)))
				{
					CosmeticPresetManager.SelectedPresetIndex = (CosmeticPresetManager.SelectedPresetIndex + 1) % CosmeticPresetManager.Presets.Count;
				}
				if (GUILayout.Button("Apply Preset", GUILayout.Width(90)))
				{
					CosmeticPresetManager.ApplyPreset(currentPreset);
				}
				if (GUILayout.Button("Delete", GUILayout.Width(60)))
				{
					CosmeticPresetManager.DeleteSelected();
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Label("<color=#AAAAAA>No cosmetic presets saved yet.</color>");
			}

			GUILayout.Space(4);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name:", GUILayout.Width(45));
			CosmeticPresetManager.NewPresetName = GUILayout.TextField(CosmeticPresetManager.NewPresetName, GUILayout.Width(130));
			if (GUILayout.Button("Save Outfit", GUILayout.Width(90)))
			{
				CosmeticPresetManager.SaveCurrentOutfit(CosmeticPresetManager.NewPresetName);
			}
			GUILayout.EndHorizontal();
		}
	}
}