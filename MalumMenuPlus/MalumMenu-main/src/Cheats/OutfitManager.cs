using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using BepInEx;
using UnityEngine;

namespace MalumMenu;

public static class OutfitManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string OutfitsDirectory => Path.Combine(Paths.ConfigPath, "Outfits");

    public static void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(OutfitsDirectory))
            {
                Directory.CreateDirectory(OutfitsDirectory);
            }
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to create Outfits directory: {ex}");
        }
    }

    public static List<MalumOutfit> LoadAllOutfits()
    {
        EnsureDirectoryExists();
        List<MalumOutfit> outfits = new();

        try
        {
            string[] files = Directory.GetFiles(OutfitsDirectory, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    MalumOutfit outfit = JsonSerializer.Deserialize<MalumOutfit>(json, JsonOptions);
                    if (outfit != null)
                    {
                        if (string.IsNullOrWhiteSpace(outfit.Name))
                        {
                            outfit.Name = Path.GetFileNameWithoutExtension(file);
                        }
                        outfits.Add(outfit);
                    }
                }
                catch (Exception ex)
                {
                    MalumMenu.Log?.LogWarning($"Failed to read outfit file '{file}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to list outfits in '{OutfitsDirectory}': {ex}");
        }

        return outfits;
    }

    public static bool SaveOutfit(MalumOutfit outfit)
    {
        if (outfit == null || string.IsNullOrWhiteSpace(outfit.Name)) return false;

        EnsureDirectoryExists();

        try
        {
            outfit.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string safeName = SanitizeFileName(outfit.Name);
            string filePath = Path.Combine(OutfitsDirectory, $"{safeName}.json");

            string json = JsonSerializer.Serialize(outfit, JsonOptions);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to save outfit '{outfit.Name}': {ex}");
            return false;
        }
    }

    public static bool RenameOutfit(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
        if (oldName.Trim().Equals(newName.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

        EnsureDirectoryExists();

        try
        {
            string oldSafe = SanitizeFileName(oldName);
            string newSafe = SanitizeFileName(newName);
            string oldPath = Path.Combine(OutfitsDirectory, $"{oldSafe}.json");
            string newPath = Path.Combine(OutfitsDirectory, $"{newSafe}.json");

            if (File.Exists(oldPath))
            {
                string json = File.ReadAllText(oldPath);
                MalumOutfit outfit = JsonSerializer.Deserialize<MalumOutfit>(json, JsonOptions);
                if (outfit != null)
                {
                    outfit.Name = newName.Trim();
                    string updatedJson = JsonSerializer.Serialize(outfit, JsonOptions);
                    File.WriteAllText(newPath, updatedJson);

                    if (!oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(oldPath);
                    }
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to rename outfit '{oldName}' to '{newName}': {ex}");
        }

        return false;
    }

    public static bool DeleteOutfit(string outfitName)
    {
        if (string.IsNullOrWhiteSpace(outfitName)) return false;

        try
        {
            string safeName = SanitizeFileName(outfitName);
            string filePath = Path.Combine(OutfitsDirectory, $"{safeName}.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to delete outfit '{outfitName}': {ex}");
        }

        return false;
    }

    public static bool ApplyOutfit(MalumOutfit outfit)
    {
        if (outfit == null) return false;

        bool applied = false;

        // 1. Update Game Customization Data (works in Inventory, Wardrobe, Main Menu & Game)
        try
        {
            var cus = AmongUs.Data.DataManager.Player.Customization;
            if (cus != null)
            {
                cus.Color = (byte)Mathf.Clamp(outfit.ColorId, 0, 17);
                cus.Hat = outfit.HatId ?? "";
                cus.Visor = outfit.VisorId ?? "";
                cus.Skin = outfit.SkinId ?? "";
                cus.Pet = outfit.PetId ?? "";
                cus.NamePlate = outfit.NamePlateId ?? "";
                applied = true;
            }
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogWarning($"Failed to update DataManager.Player.Customization: {ex.Message}");
        }

        // 2. Broadcast RPCs if LocalPlayer is active
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer != null)
        {
            try
            {
                localPlayer.CmdCheckColor((byte)Mathf.Clamp(outfit.ColorId, 0, 17));
                localPlayer.RpcSetHat(outfit.HatId ?? "");
                localPlayer.RpcSetVisor(outfit.VisorId ?? "");
                localPlayer.RpcSetSkin(outfit.SkinId ?? "");
                localPlayer.RpcSetPet(outfit.PetId ?? "");
                localPlayer.RpcSetNamePlate(outfit.NamePlateId ?? "");
                applied = true;
            }
            catch (Exception ex)
            {
                MalumMenu.Log?.LogError($"Failed to broadcast outfit RPCs for '{outfit.Name}': {ex}");
            }
        }

        return applied;
    }

    public static MalumOutfit CaptureCurrentOutfit(string name)
    {
        byte colorId = 0;
        string hatId = "";
        string visorId = "";
        string skinId = "";
        string petId = "";
        string namePlateId = "";
        bool captured = false;

        // Check active Customization (e.g. inside Inventory / Wardrobe)
        try
        {
            var cus = AmongUs.Data.DataManager.Player.Customization;
            if (cus != null)
            {
                colorId = cus.Color;
                hatId = cus.Hat ?? "";
                visorId = cus.Visor ?? "";
                skinId = cus.Skin ?? "";
                petId = cus.Pet ?? "";
                namePlateId = cus.NamePlate ?? "";
                captured = true;
            }
        }
        catch { }

        // Check active LocalPlayer DefaultOutfit
        if (!captured && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.DefaultOutfit != null)
        {
            var currentOutfit = PlayerControl.LocalPlayer.Data.DefaultOutfit;
            colorId = (byte)currentOutfit.ColorId;
            hatId = currentOutfit.HatId ?? "";
            visorId = currentOutfit.VisorId ?? "";
            skinId = currentOutfit.SkinId ?? "";
            petId = currentOutfit.PetId ?? "";
            namePlateId = currentOutfit.NamePlateId ?? "";
            captured = true;
        }

        if (!captured) return null;

        return new MalumOutfit
        {
            Name = string.IsNullOrWhiteSpace(name) ? "My Outfit" : name.Trim(),
            ColorId = colorId,
            HatId = hatId,
            VisorId = visorId,
            SkinId = skinId,
            PetId = petId,
            NamePlateId = namePlateId,
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public static MalumOutfit ClonePlayerOutfit(PlayerControl target, string name)
    {
        if (target == null || target.Data == null) return null;

        var targetOutfit = target.Data.DefaultOutfit;
        if (targetOutfit == null) return null;

        string outfitName = string.IsNullOrWhiteSpace(name) 
            ? $"{target.Data.PlayerName}'s Outfit" 
            : name.Trim();

        return new MalumOutfit
        {
            Name = outfitName,
            ColorId = targetOutfit.ColorId,
            HatId = targetOutfit.HatId ?? "",
            VisorId = targetOutfit.VisorId ?? "",
            SkinId = targetOutfit.SkinId ?? "",
            PetId = targetOutfit.PetId ?? "",
            NamePlateId = targetOutfit.NamePlateId ?? "",
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public static void OpenOutfitsFolder()
    {
        EnsureDirectoryExists();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OutfitsDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MalumMenu.Log?.LogError($"Failed to open Outfits directory in Explorer: {ex}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
