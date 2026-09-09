using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;

namespace MalumMenu;

// Advanced multi-profile manager. Each profile is a .txt (same format as the legacy MalumProfile.txt)
// stored in BepInEx/config/MalumProfiles. Supports add / delete / rename / update (save) / load, with
// one profile marked "current". The legacy single-file profile is migrated in as "Default" on startup.
public static class ProfileManager
{
    public static string ProfilesDir => Path.Combine(Paths.ConfigPath, "MalumProfiles");

    public static readonly List<string> Profiles = new();
    public static string CurrentProfile = "Default";

    public static string PathFor(string name) => Path.Combine(ProfilesDir, name + ".txt");
    public static string CurrentProfilePath => PathFor(CurrentProfile);

    public static void Initialize()
    {
        try
        {
            if (!Directory.Exists(ProfilesDir)) Directory.CreateDirectory(ProfilesDir);

            // Migrate the legacy single-file profile (config/MalumProfile.txt) into the folder as "Default"
            if (File.Exists(MalumMenu.ProfilePath) && !File.Exists(PathFor("Default")))
            {
                try { File.Copy(MalumMenu.ProfilePath, PathFor("Default")); } catch { }
            }

            RefreshList();

            if (Profiles.Count == 0)
            {
                // Nothing on disk yet — seed a Default profile from the current toggles
                CurrentProfile = "Default";
                CheatToggles.SaveTogglesToProfile(CurrentProfilePath);
                RefreshList();
            }
            else
            {
                CurrentProfile = Profiles.Contains("Default") ? "Default" : Profiles[0];
            }
        }
        catch { }
    }

    public static void RefreshList()
    {
        Profiles.Clear();
        try
        {
            if (!Directory.Exists(ProfilesDir)) return;
            foreach (var f in Directory.GetFiles(ProfilesDir, "*.txt"))
                Profiles.Add(Path.GetFileNameWithoutExtension(f));
            Profiles.Sort(StringComparer.OrdinalIgnoreCase);
        }
        catch { }
    }

    // Cleans a user-entered name into something safe to use as a filename. Returns null if unusable.
    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        name = name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        if (name.Length > 32) name = name.Substring(0, 32);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static bool Exists(string name) => name != null && Profiles.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));

    // Save the current toggles into the current profile file (i.e. "update")
    public static void SaveCurrent() => CheatToggles.SaveTogglesToProfile(CurrentProfilePath);

    // Load the current profile file into the toggles
    public static void LoadCurrent() => CheatToggles.LoadTogglesFromProfile(CurrentProfilePath);

    // Create a new profile from the current toggles and make it current. Returns false if the name is
    // invalid or already taken.
    public static bool Create(string rawName)
    {
        var name = Sanitize(rawName);
        if (name == null || Exists(name)) return false;

        CurrentProfile = name;
        CheatToggles.SaveTogglesToProfile(CurrentProfilePath);
        RefreshList();
        return true;
    }

    // Save the current toggles into an existing (or new) named profile and make it current.
    public static bool SaveAs(string name)
    {
        name = Sanitize(name);
        if (name == null) return false;

        CurrentProfile = name;
        CheatToggles.SaveTogglesToProfile(CurrentProfilePath);
        RefreshList();
        return true;
    }

    public static bool Load(string name)
    {
        if (!Exists(name)) return false;

        CurrentProfile = name;
        CheatToggles.LoadTogglesFromProfile(CurrentProfilePath);
        return true;
    }

    public static bool Delete(string name)
    {
        if (!Exists(name)) return false;

        try { if (File.Exists(PathFor(name))) File.Delete(PathFor(name)); }
        catch { return false; }

        RefreshList();

        // Never leave the manager with zero profiles
        if (Profiles.Count == 0)
        {
            CurrentProfile = "Default";
            CheatToggles.SaveTogglesToProfile(CurrentProfilePath);
            RefreshList();
        }
        else if (string.Equals(CurrentProfile, name, StringComparison.OrdinalIgnoreCase))
        {
            CurrentProfile = Profiles[0];
        }

        return true;
    }

    public static bool Rename(string oldName, string rawNew)
    {
        var newName = Sanitize(rawNew);
        if (newName == null || !Exists(oldName) || Exists(newName)) return false;

        try { File.Move(PathFor(oldName), PathFor(newName)); }
        catch { return false; }

        if (string.Equals(CurrentProfile, oldName, StringComparison.OrdinalIgnoreCase)) CurrentProfile = newName;
        RefreshList();
        return true;
    }

    public static void OpenFolder()
    {
        try
        {
            if (!Directory.Exists(ProfilesDir)) Directory.CreateDirectory(ProfilesDir);
            Process.Start("explorer.exe", ProfilesDir);
        }
        catch { }
    }
}
