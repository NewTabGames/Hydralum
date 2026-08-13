using System;
using System.Collections.Generic;
using AmongUs.Data;
using BepInEx.Configuration;
using HydraMenu.network;
using UnityEngine;

namespace HydraMenu
{
	public class CosmeticPreset
	{
		public string Name { get; set; } = "Preset";
		public byte ColorId { get; set; }
		public string HatId { get; set; } = "";
		public string VisorId { get; set; } = "";
		public string SkinId { get; set; } = "";
		public string PetId { get; set; } = "";
		public string NamePlateId { get; set; } = "";
	}

	public static class CosmeticPresetManager
	{
		public static List<CosmeticPreset> Presets = new List<CosmeticPreset>();
		public static int SelectedPresetIndex = 0;
		public static bool AutoApplyOnJoin = false;
		public static string NewPresetName = "My Outfit";

		private static ConfigEntry<string> PresetsConfig;
		private static ConfigEntry<bool> AutoApplyConfig;

		public static void Init(ConfigFile config)
		{
			AutoApplyConfig = config.Bind("CosmeticPresets", "AutoApplyOnJoin", false, "Auto apply selected preset on join");
			PresetsConfig = config.Bind("CosmeticPresets", "SavedPresets", "", "Serialized list of saved cosmetic presets");

			AutoApplyOnJoin = AutoApplyConfig.Value;
			LoadFromConfig();
		}

		public static void SaveToConfig()
		{
			try
			{
				if (AutoApplyConfig != null) AutoApplyConfig.Value = AutoApplyOnJoin;
				if (PresetsConfig != null)
				{
					PresetsConfig.Value = SerializePresets();
				}
			}
			catch (Exception ex)
			{
				Hydra.Log?.LogError($"Failed to save cosmetic presets: {ex}");
			}
		}

		public static void LoadFromConfig()
		{
			try
			{
				if (PresetsConfig != null && !string.IsNullOrEmpty(PresetsConfig.Value))
				{
					DeserializePresets(PresetsConfig.Value);
				}
			}
			catch (Exception ex)
			{
				Hydra.Log?.LogError($"Failed to load cosmetic presets: {ex}");
			}
		}

		private static string SerializePresets()
		{
			var items = new List<string>();
			foreach (var p in Presets)
			{
				string safeName = p.Name.Replace(";", "").Replace("|", "");
				items.Add($"{safeName}|{p.ColorId}|{p.HatId}|{p.VisorId}|{p.SkinId}|{p.PetId}|{p.NamePlateId}");
			}
			return string.Join(";", items);
		}

		private static void DeserializePresets(string raw)
		{
			Presets.Clear();
			if (string.IsNullOrWhiteSpace(raw)) return;

			var parts = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				var fields = part.Split('|');
				if (fields.Length >= 7)
				{
					if (byte.TryParse(fields[1], out byte col))
					{
						Presets.Add(new CosmeticPreset
						{
							Name = fields[0],
							ColorId = col,
							HatId = fields[2],
							VisorId = fields[3],
							SkinId = fields[4],
							PetId = fields[5],
							NamePlateId = fields[6]
						});
					}
				}
			}
		}

		public static void SaveCurrentOutfit(string presetName)
		{
			if (PlayerControl.LocalPlayer == null) return;
			var cur = PlayerControl.LocalPlayer.CurrentOutfit;
			if (cur == null) return;

			var preset = new CosmeticPreset
			{
				Name = string.IsNullOrWhiteSpace(presetName) ? $"Preset {Presets.Count + 1}" : presetName,
				ColorId = (byte)cur.ColorId,
				HatId = cur.HatId ?? "",
				VisorId = cur.VisorId ?? "",
				SkinId = cur.SkinId ?? "",
				PetId = cur.PetId ?? "",
				NamePlateId = cur.NamePlateId ?? ""
			};

			Presets.Add(preset);
			SelectedPresetIndex = Presets.Count - 1;
			SaveToConfig();
			Hydra.notifications?.Send("Cosmetics", $"Saved preset '{preset.Name}'!", 4f);
		}

		public static void ApplyPreset(CosmeticPreset preset)
		{
			if (PlayerControl.LocalPlayer == null || preset == null) return;

			try
			{
				PlayerControl.LocalPlayer.CmdCheckColor(preset.ColorId);
				PlayerControl.LocalPlayer.SetColor(preset.ColorId);
				if (PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.DefaultOutfit != null)
				{
					PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId = preset.ColorId;
				}

				BatchedMessage batch = new BatchedMessage();
				var localOutfit = PlayerControl.LocalPlayer.CurrentOutfit;
				byte seq = localOutfit != null ? (byte)(localOutfit.HatSequenceId + 1) : (byte)100;

				batch.QueueSetNameplateStr(PlayerControl.LocalPlayer, preset.NamePlateId, ++seq);
				batch.QueueSetHatStr(PlayerControl.LocalPlayer, preset.HatId, ++seq);
				batch.QueueSetVisorStr(PlayerControl.LocalPlayer, preset.VisorId, ++seq);
				batch.QueueSetSkinStr(PlayerControl.LocalPlayer, preset.SkinId, ++seq);
				batch.QueueSetPetStr(PlayerControl.LocalPlayer, preset.PetId, ++seq);

				batch.FinishBatch();
				Hydra.notifications?.Send("Cosmetics", $"Applied preset '{preset.Name}'!", 4f);
			}
			catch (Exception ex)
			{
				Hydra.Log?.LogError($"Failed to apply cosmetic preset: {ex}");
			}
		}

		public static void DeleteSelected()
		{
			if (Presets.Count == 0 || SelectedPresetIndex < 0 || SelectedPresetIndex >= Presets.Count) return;
			string name = Presets[SelectedPresetIndex].Name;
			Presets.RemoveAt(SelectedPresetIndex);
			if (SelectedPresetIndex >= Presets.Count) SelectedPresetIndex = Math.Max(0, Presets.Count - 1);
			SaveToConfig();
			Hydra.notifications?.Send("Cosmetics", $"Deleted preset '{name}'", 4f);
		}
	}
}
