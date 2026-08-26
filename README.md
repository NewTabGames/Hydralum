# Hydralum

[![Download](https://img.shields.io/badge/Download-Actions%20Builds-00FFAA?style=flat-square&logo=github)](https://github.com/NewTabGames/Hydralum/actions)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0)
[![BepInEx 6 IL2CPP](https://img.shields.io/badge/BepInEx-6%20IL2CPP-purple.svg?style=flat-square)](https://builds.bepinex.dev/projects/bepinex_be)
[![Discord](https://img.shields.io/badge/Discord-Join%20Community-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/GBg7hp7qAX)

Hydralum is an Among Us mod for BepInEx 6 (IL2CPP) that combines MalumMenu and HydraMenu into a single package. You can switch between both menus in-game without crashes or conflicting keybinds.

## Features

- **Dual Menu Support**: Open MalumMenu with `Delete` or HydraMenu with `F6`. You can also switch between them using the in-game header button.
- **Outfit Presets & Wardrobe Manager**: Save, load, and rename unlimited cosmetic loadouts stored as JSON files. Includes a 1-click cloner to copy other players' outfits in your lobby.
- **Persistent Configs**: Hydra settings now save and load automatically between game restarts (`BepInEx/config/com.mrd.hydramenu.cfg`).
- **Live User Presence**: Optional real-time lobby and menu status tracking between Hydralum users.

## Installation

### Option 1: Standalone Package (Easiest)

This package comes pre-packaged with BepInEx 6 IL2CPP and the Hydralum DLLs.

1. Go to the [Actions Tab](https://github.com/NewTabGames/Hydralum/actions) and click the latest build.
2. Under **Artifacts**, download `Hydralum-Standalone-Steam`.
3. Open your Among Us game folder:
   - Steam: Right-click Among Us -> Manage -> Browse local files.
   - Epic Games: Open your Among Us installation folder.
4. Extract all files from the zip directly into your game folder (where `Among Us.exe` is located).
5. Launch the game. Press `Delete` to open MalumMenu or `F6` for HydraMenu.

### Option 2: Manual Install (Existing BepInEx 6 Users)

If you already have BepInEx 6 IL2CPP installed:

1. Go to the [Actions Tab](https://github.com/NewTabGames/Hydralum/actions) and download `Hydralum-DLLs`.
2. Place `MalumMenuPlus.dll` and `HydraMenu.dll` into your `Among Us/BepInEx/plugins/` folder.
3. Launch Among Us.

### Manual BepInEx Setup

Among Us uses Unity IL2CPP, so standard BepInEx 5 will not work. You need BepInEx 6 IL2CPP x86.

1. Download BepInEx 6 IL2CPP x86 (Build 785): [Direct Download](https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.785%2B6abdba4.zip)
2. Extract the zip into your Among Us folder so `winhttp.dll` and the `BepInEx` folder sit next to `Among Us.exe`.
3. Run the game once to generate folders, then close it and add the mod DLLs to `BepInEx/plugins/`.

## Keybinds

| Key | Action | Description |
| :--- | :--- | :--- |
| `Delete` | Toggle MalumMenu | Opens or closes MalumMenu (changeable in Config tab). |
| `F6` | Toggle HydraMenu | Opens or closes HydraMenu. |
| `Escape` | Close Menus | Dismisses Match Info Guide, outfit laptop menus, and dialogs. |
| `Left / Right Arrow` | Vent Hop | Cycles between vents when vent walk is active. |

## Requirements

- Game: Among Us (Steam or Epic Games)
- OS: Windows 10 or 11 (64-bit)
- Mod Loader: BepInEx 6 Unity IL2CPP (x86 Build 785 or newer)

## Building from Source

Prerequisites: [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)

```bash
git clone https://github.com/NewTabGames/Hydralum.git
cd Hydralum

# Build MalumMenu
dotnet build MalumMenuPlus/MalumMenu-main/src/MalumMenu.csproj -c Release

# Build HydraMenu
dotnet build Hydra-main/src/HydraMenu.csproj -c Release
```

Compiled DLLs are output to their respective `bin/Release/net6.0/` folders and copied to `DLLs/`.

## Credits & Licensing

- [MalumMenu](https://github.com/scp222thj/MalumMenu) by scp222thj & astra1dev (GPL-3.0)
- [Hydra](https://github.com/MrDiamond64/Hydra) by MrDiamond64 (GPL-3.0)
- [Hydralum](https://github.com/NewTabGames/Hydralum) by NewTabGames (GPL-3.0)

This project is licensed under the GNU General Public License v3.0 (GPL-3.0).