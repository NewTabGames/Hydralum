using System;
using System.Collections.Generic;
using UnityEngine;

namespace MalumMenu;

public class InventoryOutfitsUI : MonoBehaviour
{
    public static int windowWidth = 285;
    public static int windowHeight = 510;
    private Rect _windowRect;

    private string _customOutfitName = "Outfit 1";
    private int _presetTemplateIndex = 0;
    private string _outfitPendingDelete = "";
    private string _outfitPendingRename = "";
    private string _renameBuffer = "";
    private int _renameTemplateIndex = 0;
    private string _statusMessage = "";
    private float _statusTimer = 0f;
    private Vector2 _outfitsScroll = Vector2.zero;
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

    private void Start()
    {
        _windowRect = new Rect(
            Mathf.Max(10, Screen.width - windowWidth - 25),
            55,
            windowWidth,
            windowHeight
        );
    }

    private void Update()
    {
        if (_statusTimer > 0)
        {
            _statusTimer -= Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        if (MalumMenu.isPanicked) return;

        // Display whenever player is in the Inventory / Wardrobe customization menu
        bool isInventoryOpen = PlayerCustomizationMenu.Instance != null && PlayerCustomizationMenu.Instance.isActiveAndEnabled;
        if (!isInventoryOpen) return;

        UIHelpers.ApplyUIColor();

        if (_cachedOutfits == null)
        {
            RefreshOutfits();
        }

        _windowRect = GUI.Window((int)WindowId.InventoryOutfitsUI, _windowRect, (GUI.WindowFunction)DrawWindow, "Wardrobe Outfit Presets");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();

        // 1. Save Section
        GUILayout.Label("Save Current Outfit", GUIStylePreset.TabSubtitle);
        GUILayout.Label($"Preset Name: <b>{_customOutfitName}</b>", GUIStylePreset.Hint);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUIStylePreset.NormalButton, GUILayout.Width(28), GUILayout.Height(22)))
        {
            _presetTemplateIndex = _presetTemplateIndex > 0 ? _presetTemplateIndex - 1 : PresetTemplates.Length - 1;
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }

        if (GUILayout.Button(PresetTemplates[_presetTemplateIndex], GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }

        if (GUILayout.Button(">", GUIStylePreset.NormalButton, GUILayout.Width(28), GUILayout.Height(22)))
        {
            _presetTemplateIndex = _presetTemplateIndex < PresetTemplates.Length - 1 ? _presetTemplateIndex + 1 : 0;
            _customOutfitName = PresetTemplates[_presetTemplateIndex];
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paste Clipboard", GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clipboard))
            {
                _customOutfitName = clipboard.Trim();
                SetStatus($"<color=white>Pasted: '{_customOutfitName}'</color>");
            }
        }

        if (GUILayout.Button("Use Color", GUIStylePreset.NormalButton, GUILayout.Height(22)))
        {
            int col = 0;
            try { col = AmongUs.Data.DataManager.Player.Customization.Color; } catch { }
            string colName = col >= 0 && col < ColorNames.Length ? ColorNames[col] : $"Color {col}";
            _customOutfitName = $"{colName} Look";
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);

        if (GUILayout.Button("💾 Save Outfit to JSON", GUIStylePreset.NormalButton, GUILayout.Height(26)))
        {
            var outfit = OutfitManager.CaptureCurrentOutfit(_customOutfitName);
            if (outfit != null && OutfitManager.SaveOutfit(outfit))
            {
                RefreshOutfits();
                SetStatus($"<color=green>Saved '{outfit.Name}'!</color>");
            }
            else
            {
                SetStatus("<color=red>Failed to save outfit.</color>");
            }
        }

        GUILayout.Space(6);

        // 2. Presets Library
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Saved Presets ({_cachedOutfits?.Count ?? 0})", GUIStylePreset.TabSubtitle);
        if (GUILayout.Button("Refresh", GUIStylePreset.NormalButton, GUILayout.Width(60), GUILayout.Height(20)))
        {
            RefreshOutfits();
        }
        GUILayout.EndHorizontal();

        if (_statusTimer > 0 && !string.IsNullOrEmpty(_statusMessage))
        {
            GUILayout.Label(_statusMessage, GUIStylePreset.Hint);
        }

        _outfitsScroll = GUILayout.BeginScrollView(_outfitsScroll, GUILayout.Height(235));

        if (_cachedOutfits == null || _cachedOutfits.Count == 0)
        {
            GUILayout.Label("No saved outfits yet.\nClick Save above to create one!", GUIStylePreset.Hint);
        }
        else
        {
            for (int i = 0; i < _cachedOutfits.Count; i++)
            {
                var outfit = _cachedOutfits[i];
                if (outfit == null) continue;

                GUILayout.BeginVertical(GUI.skin.box);

                string colorName = outfit.ColorId >= 0 && outfit.ColorId < ColorNames.Length 
                    ? ColorNames[outfit.ColorId] 
                    : $"Color {outfit.ColorId}";
                GUILayout.Label($"<b>{outfit.Name}</b> <color=#aaaaaa>({colorName})</color>");

                // Check pending rename
                if (_outfitPendingRename == outfit.Name)
                {
                    GUILayout.Label($"Rename to: <b>{_renameBuffer}</b>", GUIStylePreset.Hint);

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", GUIStylePreset.NormalButton, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _renameTemplateIndex = _renameTemplateIndex > 0 ? _renameTemplateIndex - 1 : PresetTemplates.Length - 1;
                        _renameBuffer = PresetTemplates[_renameTemplateIndex];
                    }
                    if (GUILayout.Button(_renameBuffer, GUIStylePreset.NormalButton, GUILayout.Height(20)))
                    {
                        _renameBuffer = PresetTemplates[_renameTemplateIndex];
                    }
                    if (GUILayout.Button(">", GUIStylePreset.NormalButton, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        _renameTemplateIndex = _renameTemplateIndex < PresetTemplates.Length - 1 ? _renameTemplateIndex + 1 : 0;
                        _renameBuffer = PresetTemplates[_renameTemplateIndex];
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Paste", GUIStylePreset.NormalButton, GUILayout.Height(20)))
                    {
                        string clipboard = GUIUtility.systemCopyBuffer;
                        if (!string.IsNullOrWhiteSpace(clipboard))
                        {
                            _renameBuffer = clipboard.Trim();
                        }
                    }
                    if (GUILayout.Button("Confirm", GUIStylePreset.NormalButton, GUILayout.Height(20)))
                    {
                        if (OutfitManager.RenameOutfit(outfit.Name, _renameBuffer))
                        {
                            _outfitPendingRename = "";
                            RefreshOutfits();
                            SetStatus($"<color=green>Renamed to '{_renameBuffer}'!</color>");
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                            break;
                        }
                    }
                    if (GUILayout.Button("Cancel", GUIStylePreset.NormalButton, GUILayout.Width(50), GUILayout.Height(20)))
                    {
                        _outfitPendingRename = "";
                    }
                    GUILayout.EndHorizontal();
                }
                // Check pending delete
                else if (_outfitPendingDelete == outfit.Name)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("<color=#FF5555>Delete?</color>", GUILayout.ExpandWidth(true));

                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                    if (GUILayout.Button("Yes", GUIStylePreset.NormalButton, GUILayout.Width(45), GUILayout.Height(20)))
                    {
                        if (OutfitManager.DeleteOutfit(outfit.Name))
                        {
                            _outfitPendingDelete = "";
                            RefreshOutfits();
                            SetStatus($"<color=yellow>Deleted '{outfit.Name}'.</color>");
                            GUI.backgroundColor = prevBg;
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                            break;
                        }
                    }

                    GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                    if (GUILayout.Button("No", GUIStylePreset.NormalButton, GUILayout.Width(40), GUILayout.Height(20)))
                    {
                        _outfitPendingDelete = "";
                    }
                    GUI.backgroundColor = prevBg;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    // Action Buttons - Row 1: Primary actions
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Equip", GUIStylePreset.NormalButton, GUILayout.Height(21)))
                    {
                        OutfitManager.ApplyOutfit(outfit);
                        SetStatus($"<color=green>Equipped '{outfit.Name}'!</color>");
                    }

                    if (GUILayout.Button("Overwrite", GUIStylePreset.NormalButton, GUILayout.Height(21)))
                    {
                        var updated = OutfitManager.CaptureCurrentOutfit(outfit.Name);
                        if (updated != null && OutfitManager.SaveOutfit(updated))
                        {
                            RefreshOutfits();
                            SetStatus($"<color=green>Updated '{outfit.Name}'!</color>");
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Action Buttons - Row 2: Management
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Rename", GUIStylePreset.NormalButton, GUILayout.Height(21)))
                    {
                        _outfitPendingRename = outfit.Name;
                        _renameBuffer = outfit.Name;
                    }

                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                    if (GUILayout.Button("Delete (X)", GUIStylePreset.NormalButton, GUILayout.Height(21)))
                    {
                        _outfitPendingDelete = outfit.Name;
                    }
                    GUI.backgroundColor = prevBg;
                    GUILayout.EndHorizontal();
                }

                // Clean Cosmetic Details
                string hatText = FormatCosmetic(outfit.HatId, "hat_");
                string skinText = FormatCosmetic(outfit.SkinId, "skin_");
                string visorText = FormatCosmetic(outfit.VisorId, "visor_");
                string petText = FormatCosmetic(outfit.PetId, "pet_");
                GUILayout.Label($"<size=10><color=#888888>H: {hatText} | S: {skinText}\nV: {visorText} | P: {petText}</color></size>");

                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4);

        // 3. Quick Actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Folder", GUIStylePreset.NormalButton, GUILayout.Height(20)))
        {
            OutfitManager.OpenOutfitsFolder();
        }
        if (GUILayout.Button("Restore Default", GUIStylePreset.NormalButton, GUILayout.Height(20)))
        {
            MalumAvatar.RestoreAvatar();
            SetStatus("<color=white>Restored default.</color>");
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    private static string FormatCosmetic(string id, string prefix)
    {
        if (string.IsNullOrEmpty(id) || id.Contains("Empty", StringComparison.OrdinalIgnoreCase))
        {
            return "None";
        }
        if (!string.IsNullOrEmpty(prefix) && id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return id.Substring(prefix.Length);
        }
        return id;
    }

    public void RefreshOutfits()
    {
        _cachedOutfits = OutfitManager.LoadAllOutfits();
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
        _statusTimer = 3.5f;
    }
}
