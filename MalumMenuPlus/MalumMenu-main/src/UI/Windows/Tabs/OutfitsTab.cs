using System;
using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

public class OutfitsTab : ITab
{
    public string name => "Outfits";

    private string _customOutfitName = "Outfit 1";
    private int _presetTemplateIndex = 0;
    private string _statusMessage = "";
    private float _statusTimer = 0f;
    private Vector2 _outfitsScroll = Vector2.zero;
    private byte _selectedPlayerToClone = byte.MaxValue;
    private List<MalumOutfit> _cachedOutfits = null;

    private static readonly string[] PresetTemplates =
    {
        "Outfit 1", "Outfit 2", "Outfit 3", "Outfit 4", "Outfit 5",
        "Outfit 6", "Outfit 7", "Outfit 8", "Outfit 9", "Outfit 10",
        "Stealth", "Detective", "Captain", "Medic", "Astronaut",
        "Impostor", "Neon", "Cyberpunk", "Casual", "Ghost"
    };

    private static readonly string[] ColorNames =
    {
        "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White",
        "Purple", "Brown", "Cyan", "Lime", "Maroon", "Rose", "Banana", "Gray",
        "Tan", "Sunset"
    };

    public void Draw()
    {
        if (_cachedOutfits == null)
        {
            RefreshOutfits();
        }

        if (_statusTimer > 0)
        {
            _statusTimer -= Time.deltaTime;
        }

        GUILayout.BeginHorizontal();

        // Left column: Outfit Creator, Cloner & Quick Tools
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.44f));

        DrawSaveSection();

        GUILayout.Space(12);

        DrawCloneSection();

        GUILayout.Space(12);

        DrawToolsSection();

        GUILayout.EndVertical();

        GUILayout.Space(15);

        // Right column: Saved Outfits Library
        GUILayout.BeginVertical();

        DrawLibrarySection();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DrawSaveSection()
    {
        GUILayout.Label("Save Current Outfit", GUIStylePreset.TabSubtitle);

        GUILayout.Label($"Preset Name: <b>{_customOutfitName}</b>", GUIStylePreset.Hint);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUIStylePreset.NormalButton, GUILayout.Width(35), GUILayout.Height(24)))
        {
            _presetTemplateIndex = _presetTemplateIndex > 0 ? _presetTemplateIndex - 1 : PresetTemplates.Length - 1;
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }

        if (GUILayout.Button($"Select: {PresetTemplates[_presetTemplateIndex]}", GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }

        if (GUILayout.Button(">", GUIStylePreset.NormalButton, GUILayout.Width(35), GUILayout.Height(24)))
        {
            _presetTemplateIndex = _presetTemplateIndex < PresetTemplates.Length - 1 ? _presetTemplateIndex + 1 : 0;
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paste Clipboard Name", GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clipboard))
            {
                _customOutfitName = clipboard.Trim();
                SetStatus($"<color=white>Pasted name: '{_customOutfitName}'</color>");
            }
        }

        if (GUILayout.Button("Use Color Name", GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
            {
                int col = PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId;
                string colName = col >= 0 && col < ColorNames.Length ? ColorNames[col] : $"Color {col}";
                _customOutfitName = $"{colName} Look";
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (GUILayout.Button("Save as JSON Preset", GUIStylePreset.NormalButton, GUILayout.Height(28)))
        {
            if (PlayerControl.LocalPlayer == null)
            {
                SetStatus("<color=red>Join a lobby to save your outfit!</color>");
            }
            else
            {
                var outfit = OutfitManager.CaptureCurrentOutfit(_customOutfitName);
                if (outfit != null && OutfitManager.SaveOutfit(outfit))
                {
                    RefreshOutfits();
                    SetStatus($"<color=green>Saved '{outfit.Name}' to JSON!</color>");
                }
                else
                {
                    SetStatus("<color=red>Failed to save outfit preset.</color>");
                }
            }
        }
    }

    private void DrawCloneSection()
    {
        GUILayout.Label("Clone / Steal Outfit", GUIStylePreset.TabSubtitle);

        var players = PlayerControl.AllPlayerControls;
        if (players == null || players.Count == 0 || !Utils.isPlayer)
        {
            GUILayout.Label("Join a lobby to clone outfits.", GUIStylePreset.Hint);
            return;
        }

        List<PlayerControl> availablePlayers = new();
        foreach (var p in players)
        {
            if (p != null && p.Data != null && !p.AmOwner)
            {
                availablePlayers.Add(p);
            }
        }

        if (availablePlayers.Count == 0)
        {
            GUILayout.Label("No other players in lobby.", GUIStylePreset.Hint);
            return;
        }

        GUILayout.Label("Select Player:", GUIStylePreset.Hint);
        GUILayout.BeginHorizontal();
        foreach (var p in availablePlayers)
        {
            bool isSelected = p.Data.PlayerId == _selectedPlayerToClone;
            var prevBg = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);

            var colHex = ColorUtility.ToHtmlStringRGB(p.Data.Color);
            if (GUILayout.Button($"<color=#{colHex}>{p.Data.PlayerName}</color>", GUIStylePreset.NormalButton, GUILayout.Height(22)))
            {
                _selectedPlayerToClone = p.Data.PlayerId;
            }
            GUI.backgroundColor = prevBg;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        PlayerControl targetPlayer = null;
        foreach (var p in availablePlayers)
        {
            if (p.Data.PlayerId == _selectedPlayerToClone)
            {
                targetPlayer = p;
                break;
            }
        }

        if (targetPlayer != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clone & Equip", GUIStylePreset.NormalButton, GUILayout.Height(26)))
            {
                var cloned = OutfitManager.ClonePlayerOutfit(targetPlayer, targetPlayer.Data.PlayerName);
                if (cloned != null)
                {
                    OutfitManager.ApplyOutfit(cloned);
                    SetStatus($"<color=cyan>Equipped {targetPlayer.Data.PlayerName}'s outfit!</color>");
                }
            }

            if (GUILayout.Button("Save to JSON", GUIStylePreset.NormalButton, GUILayout.Height(26)))
            {
                var cloned = OutfitManager.ClonePlayerOutfit(targetPlayer, targetPlayer.Data.PlayerName);
                if (cloned != null && OutfitManager.SaveOutfit(cloned))
                {
                    RefreshOutfits();
                    SetStatus($"<color=green>Saved {targetPlayer.Data.PlayerName}'s outfit!</color>");
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawToolsSection()
    {
        GUILayout.Label("Quick Actions", GUIStylePreset.TabSubtitle);

        if (GUILayout.Button("Open Outfits Folder", GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            OutfitManager.OpenOutfitsFolder();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize Avatar", GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            MalumAvatar.RandomizeAvatar();
            SetStatus("<color=yellow>Randomized cosmetics!</color>");
        }

        if (GUILayout.Button("Restore Default", GUIStylePreset.NormalButton, GUILayout.Height(24)))
        {
            MalumAvatar.RestoreAvatar();
            SetStatus("<color=white>Restored account cosmetics.</color>");
        }
        GUILayout.EndHorizontal();
    }

    private void DrawLibrarySection()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Saved Presets ({_cachedOutfits?.Count ?? 0})", GUIStylePreset.TabSubtitle);
        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(70), GUILayout.Height(22)))
        {
            RefreshOutfits();
        }
        GUILayout.EndHorizontal();

        if (_statusTimer > 0 && !string.IsNullOrEmpty(_statusMessage))
        {
            GUILayout.Label(_statusMessage, GUIStylePreset.Hint);
        }

        if (_cachedOutfits == null || _cachedOutfits.Count == 0)
        {
            GUILayout.Label("No saved outfits yet.\nCreate one on the left to save to JSON!", GUIStylePreset.Hint);
            return;
        }

        _outfitsScroll = GUILayout.BeginScrollView(_outfitsScroll);

        for (int i = 0; i < _cachedOutfits.Count; i++)
        {
            var outfit = _cachedOutfits[i];
            if (outfit == null) continue;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            string colorName = outfit.ColorId >= 0 && outfit.ColorId < ColorNames.Length 
                ? ColorNames[outfit.ColorId] 
                : $"Color {outfit.ColorId}";
            
            GUILayout.Label($"<b>{outfit.Name}</b> <color=#aaaaaa>({colorName})</color>", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Equip", GUIStylePreset.NormalButton, GUILayout.Width(65), GUILayout.Height(24)))
            {
                if (OutfitManager.ApplyOutfit(outfit))
                {
                    SetStatus($"<color=green>Equipped '{outfit.Name}'!</color>");
                }
                else
                {
                    SetStatus("<color=red>Join a lobby to equip outfits.</color>");
                }
            }

            if (GUILayout.Button("Overwrite", GUIStylePreset.NormalButton, GUILayout.Width(75), GUILayout.Height(24)))
            {
                var updated = OutfitManager.CaptureCurrentOutfit(outfit.Name);
                if (updated != null && OutfitManager.SaveOutfit(updated))
                {
                    RefreshOutfits();
                    SetStatus($"<color=green>Updated '{outfit.Name}'!</color>");
                }
            }

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("X", GUIStylePreset.NormalButton, GUILayout.Width(28), GUILayout.Height(24)))
            {
                if (OutfitManager.DeleteOutfit(outfit.Name))
                {
                    RefreshOutfits();
                    SetStatus($"<color=yellow>Deleted '{outfit.Name}'.</color>");
                    GUI.backgroundColor = prevBg;
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
            }
            GUI.backgroundColor = prevBg;

            GUILayout.EndHorizontal();

            string hatText = string.IsNullOrEmpty(outfit.HatId) ? "None" : outfit.HatId;
            string skinText = string.IsNullOrEmpty(outfit.SkinId) ? "None" : outfit.SkinId;
            string visorText = string.IsNullOrEmpty(outfit.VisorId) ? "None" : outfit.VisorId;
            string petText = string.IsNullOrEmpty(outfit.PetId) ? "None" : outfit.PetId;
            GUILayout.Label($"<size=11><color=#888888>Hat: {hatText} | Skin: {skinText} | Visor: {visorText} | Pet: {petText}</color></size>");

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        GUILayout.EndScrollView();
    }

    private void RefreshOutfits()
    {
        _cachedOutfits = OutfitManager.LoadAllOutfits();
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
        _statusTimer = 3.5f;
    }
}
