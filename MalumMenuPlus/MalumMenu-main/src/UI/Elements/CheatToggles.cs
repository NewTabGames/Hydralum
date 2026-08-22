using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AmongUs.GameOptions;
using UnityEngine;

namespace MalumMenu;

public struct CheatToggles
{
    // Movement
    public static bool noClip;
    public static bool teleportPlayer;
    public static bool teleportCursor;
    public static bool invertControls;

    // Self (ported from Hydra)
    public static bool becomeImmortal;
    public static bool noLadderCd;
    public static bool unlimitedMeetings;

    // Roles
    public static bool setFakeRole;
    public static bool setFakeAlive;
    public static bool noKillCd;
    public static bool showTasksMenu;
    public static bool completeMyTasks;
    public static bool impostorTasks;
    public static bool killReach;
    public static bool killAnyone;
    public static bool noKillChecks;
    public static bool endlessSsDuration;
    public static bool endlessBattery;
    public static bool endlessTracking;
    public static bool noTrackingCooldown;
    public static bool noTrackingDelay;
    public static bool trackReach;
    public static bool interrogateReach;
    public static bool noVitalsCooldown;
    public static bool noVentCooldown;
    public static bool endlessVentTime;
    public static bool endlessVanish;
    public static bool killVanished;
    public static bool noVanishAnim;
    public static bool noShapeshiftAnim;
    public static bool sabotageInVents;

    // ESP
    public static bool noShadows;
    public static bool seeGhosts;
    public static bool seeRoles;
    public static bool seePlayerInfo;
    public static bool seeDisguises;
    public static bool taskArrows;
    public static bool revealVotes;
    public static bool seeLobbyInfo;

    // Camera
    public static bool spectate;
    public static bool zoomOut;
    public static bool freecam;

    // Minimap
    public static bool mapCrew;
    public static bool mapImps;
    public static bool mapGhosts;
    public static bool colorBasedMap;

    // Tracers
    public static bool tracersImps;
    public static bool tracersCrew;
    public static bool tracersGhosts;
    public static bool tracersBodies;
    public static bool colorBasedTracers;
    public static bool distanceBasedTracers;

    // Chat
    public static bool enableChat;
    public static bool unlockCharacters;
    public static bool bypassUrlBlock;
    public static bool longerMessages;
    public static bool unlockClipboard;
    public static bool lowerRateLimits;

    // Ship
    public static bool closeMeeting;
    public static bool autoOpenDoorsOnUse;
    public static bool unfixableLights;
    public static bool callMeeting;
    public static bool reportBody;
    public static bool autoReportBodies;

    // Sabotage
    public static bool commsSab;
    public static bool unfixableComms;
    public static bool elecSab;
    public static bool reactorSab;
    public static bool oxygenSab;
    public static bool mushSab;
    public static bool mushSpore;
    public static bool showDoorsMenu;
    public static bool openAllDoors;
    public static bool closeAllDoors;
    public static bool spamOpenAllDoors;
    public static bool spamCloseAllDoors;
    public static bool sabotageMap;
    public static bool disableSabotage;
    public static bool sabotageAll;
    public static bool sabotageAllDoors;

    // Vents
    public static bool unlockVents;
    public static bool walkInVents;
    public static bool kickVents;
    public static bool disableVents;
    public static bool ventsExcludeSelf;
    public static bool ventNetwork;

    // Animations
    public static bool animShields;
    public static bool animAsteroids;
    public static bool animEmptyGarbage;
    public static bool animMedScan;
    public static bool animCamsInUse;
    public static bool animPet;
    public static bool moonWalk;

    // Console
    public static bool showConsole;
    public static bool logDeaths;
    public static bool logShapeshifts;
    public static bool logVents;
    public static bool logMeetings;

    // Debug
    public static bool showDebugConsole;
    public static bool logIncomingRpcs;
    public static bool logOutgoingRpcs;

    // Host-Only
    public static bool voteImmune;
    public static bool forceRole;
    public static RoleTypes? forcedRole;
    public static bool showRolesMenu;
    public static bool skipMeeting;
    public static bool forceStartGame;
    public static bool noGameEnd;
    public static bool showProtectMenu;
    public static bool noOptionsLimits;
    public static bool ejectPlayer;
    public static bool killPlayer;
    public static bool telekillPlayer;
    public static bool killAll;
    public static bool killAllCrew;
    public static bool killAllImps;
    // Host-Only (ported from Hydra)
    public static bool disableCloseDoors;
    public static bool disableMeetings;
    public static bool banMidGame;
    public static bool disableSecurityCameras;
    public static bool assignRolesNextRound;
    public static bool discoParty;
    public static bool spamReportBodies;

    // Protections (ported from Hydra) - all default off (opt-in); several sit in the network
    // receive path, so they stay inert until you enable them.
    public static bool forceDtls;
    public static bool blockServerTeleports;
    public static bool blockUnauthorizedSystemUpdates;
    public static bool protectKickExploit;
    public static bool blockLargeMessages;
    public static bool blockInvalidGameData;
    public static bool hardenedPackedInt;
    public static bool protectVotingCompleteOverload;
    public static bool preventVotekick;
    public static bool shapeshiftBypass;

    // Spoofer (ported from Hydra)
    public static bool versionSpoof;
    public static bool moddedProtocol;

    // Anticheat (ported from Hydra) - detection + notifications only, all default off
    public static bool anticheat;
    public static bool anticheatDiscard;
    public static bool anticheatPlatform;

    // Outfits & Avatar
    public static bool colorSniper;
    public static byte colorSniperTargetColor;

    // Passive
    public static bool unlockFps;
    public static bool unlockFeatures;
    public static bool freeCosmetics;
    public static bool avoidPenalties;
    public static bool copyLobbyCodeOnDisconnect;
    public static bool spoofAprilFoolsDate;

    // Modes
    public static bool rgbMode;
    public static bool panicMode;

    // Config
    public static bool reloadConfig;
    public static bool openConfig;
    public static bool loadProfile;
    public static bool saveProfile;

    // Keybind Map: Toggle Name -> KeyCode (KeyCode.None == No Key)
    public static readonly Dictionary<string, KeyCode> Keybinds = new();

    // Map for Reflection Access: Toggle Name -> FieldInfo
    public static readonly Dictionary<string, FieldInfo> ToggleFields = new();

    // Populate reflection map once at startup and initialize Keybinds with KeyCode.None
    static CheatToggles()
    {
        var fields = typeof(CheatToggles).GetFields(BindingFlags.Static | BindingFlags.Public);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(bool)) continue;

            ToggleFields[field.Name] = field;
            Keybinds[field.Name] = KeyCode.None;
        }
    }

    public static void DisablePPMCheats(string variableToKeep)
    {
        ejectPlayer = variableToKeep == "ejectPlayer" && ejectPlayer;
        reportBody = variableToKeep == "reportBody" && reportBody;
        killPlayer = variableToKeep == "killPlayer" && killPlayer;
        telekillPlayer = variableToKeep == "telekillPlayer" && telekillPlayer;
        spectate = variableToKeep == "spectate" && spectate;
        setFakeRole = variableToKeep == "setFakeRole" && setFakeRole;
        setFakeAlive = variableToKeep == "setFakeAlive" && setFakeAlive;
        forceRole = variableToKeep == "forceRole" && forceRole;
        teleportPlayer = variableToKeep == "teleportPlayer" && teleportPlayer;
    }

    public static bool ShouldPPMClose()
    {
        return !setFakeRole && !setFakeAlive && !forceRole && !ejectPlayer && !reportBody && !telekillPlayer && !killPlayer && !spectate && !teleportPlayer;
    }

    // Disables all cheat toggles by setting all to false using the cached ToggleFields
    public static void DisableAll()
    {
        foreach (var field in ToggleFields.Values)
        {
            field.SetValue(null, false);
        }
    }

    // Saves cheat toggles and their keybinds to MalumProfile.txt
    // Format per line: ToggleName = True/False = KeyCode.KEY
    public static void SaveTogglesToProfile()
    {
        using var writer = new StreamWriter(MalumMenu.ProfilePath);

        writer.WriteLine("# MalumProfile");
        writer.WriteLine("# Format: ToggleName = True/False = KeyCode.KEY");
        writer.WriteLine("# - List of supported keycodes: https://docs.unity3d.com/Packages/com.unity.tiny@0.16/api/Unity.Tiny.Input.KeyCode.html");
        writer.WriteLine("# - Setting a keybind is optional. Use KeyCode.None to not set a keybind");
        writer.WriteLine("# - Multiple toggles may have the same key, but multiple keys per toggle are NOT supported");
        writer.WriteLine("# - Keybinds are only applied after loading this profile by pressing 'Load from Profile' in the Config menu");
        writer.WriteLine();

        foreach (var field in ToggleFields.Values)
        {
            Keybinds.TryGetValue(field.Name, out var key);  // If no key is set then write KeyCode.None
            writer.WriteLine($"{field.Name} = {field.GetValue(null)} = KeyCode.{key}");
        }

        writer.WriteLine();
        writer.WriteLine("# FPS limit used by the Unlock FPS toggle (30 - 240)");
        writer.WriteLine($"FpsLimit = {FpsUnlocker.TargetFps}");

        writer.WriteLine();
        writer.WriteLine("# Menu appearance (scale 0.80 - 1.50, opacity 0.30 - 1.00)");
        writer.WriteLine($"MenuScale = {MenuUI.uiScale.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        writer.WriteLine($"MenuOpacity = {MenuUI.uiOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        writer.WriteLine();
        writer.WriteLine("# Menu accent color / theme (empty = default, #RRGGBB = solid, grad:#AAAAAA,#BBBBBB = gradient)");
        writer.WriteLine("# RGB Mode is saved separately as the rgbMode toggle above and overrides this color while on");
        writer.WriteLine($"MenuColor = {MalumMenu.menuHtmlColor.Value}");
    }

    // Loads cheat toggles and their keybinds from MalumProfile.txt if the file is present
    // Format per line: ToggleName = True/False = KeyCode.KEY
    public static void LoadTogglesFromProfile()
    {
        if (!File.Exists(MalumMenu.ProfilePath)) return;

        using var reader = new StreamReader(MalumMenu.ProfilePath);

        while (reader.ReadLine() is { } line)
        {
            // Skips empty lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skips lines that are commented out
            line = line.Trim();
            if (line.StartsWith("#")) continue;

            // Extracts the three relevant config values for each remaining line
            var parts = line.Split('=', 3);
            if (parts.Length < 2) continue;

            // Gets the cheat's FieldInfo from its name
            var name = parts[0].Trim();

            // Special non-toggle setting: the FPS limit for the Unlock FPS feature
            if (name == "FpsLimit")
            {
                if (int.TryParse(parts[1].Trim(), out var fps))
                    FpsUnlocker.TargetFps = Mathf.Clamp(fps, FpsUnlocker.MinFps, FpsUnlocker.MaxFps);
                continue;
            }

            // Menu appearance settings (scale / opacity)
            if (name == "MenuScale")
            {
                if (float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var scale))
                    MenuUI.uiScale = Mathf.Clamp(scale, 0.8f, 1.5f);
                continue;
            }

            if (name == "MenuOpacity")
            {
                if (float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var opacity))
                    MenuUI.uiOpacity = Mathf.Clamp(opacity, 0.3f, 1f);
                continue;
            }

            // Menu accent color / theme. Value has no '=' (hex or "grad:#a,#b"), so parts[1] holds it all.
            // An empty value restores the default (unset) color, mirroring the Default theme button.
            if (name == "MenuColor")
            {
                MalumMenu.menuHtmlColor.Value = parts.Length >= 2 ? parts[1].Trim() : "";
                continue;
            }

            if (!ToggleFields.TryGetValue(name, out var field)) continue;

            // Loads whether the cheat is enabled or disabled by default
            if (bool.TryParse(parts[1].Trim(), out var boolVal))
            {
                field.SetValue(null, boolVal);
            }

            // Loads the keybind associated with each cheat
            KeyCode key = KeyCode.None;
            if (parts.Length >= 3)
            {
                var keyPart = parts[2].Trim();
                if (keyPart.StartsWith("KeyCode."))
                {
                    keyPart = keyPart["KeyCode.".Length..];
                }

                if (!string.IsNullOrEmpty(keyPart) && System.Enum.TryParse<KeyCode>(keyPart, true, out var parsed))
                {
                    key = parsed;
                }
            }

            Keybinds[name] = key;
        }
    }
}
