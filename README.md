# Hydralum: Combined MalumMenu and HydraMenu for Among Us

[![Build & Downloads](https://img.shields.io/badge/Download-Actions%20Builds-00FFAA?style=flat-square&logo=github)](https://github.com/NewTabGames/Hydralum/actions)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg?style=flat-square)](https://www.gnu.org/licenses/gpl-3.0)
[![BepInEx 6 IL2CPP](https://img.shields.io/badge/BepInEx-6%20IL2CPP-purple.svg?style=flat-square)](https://builds.bepinex.dev/projects/bepinex_be)
[![Discord](https://img.shields.io/badge/Discord-Join%20Community-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/GBg7hp7qAX)

**Hydralum** is the premier all-in-one Among Us mod menu that combines **MalumMenu** and **HydraMenu** into a single, synchronized BepInEx IL2CPP mod package. Designed for seamless dual-menu switching in-game, Hydralum unites the rich feature set of MalumMenu with the powerful utilities and host features of HydraMenu.

---

## 📑 Table of Contents

- [What is Hydralum?](#what-is-hydralum)
- [Why MalumMenu + HydraMenu Combined?](#why-malummenu--hydramenu-combined)
- [Key Features](#key-features)
- [Installation Guide](#installation-guide)
  - [Option A: 1-Click Standalone (Recommended)](#option-a-1-click-standalone-recommended)
  - [Option B: Manual Plugin Install (Existing BepInEx Users)](#option-b-manual-plugin-install-existing-bepinex-users)
  - [Manual BepInEx 6 Setup](#manual-bepinex-6-setup)
- [Controls & Keybinds](#controls--keybinds)
- [System Requirements](#system-requirements)
- [Frequently Asked Questions (FAQ) & Troubleshooting](#frequently-asked-questions-faq--troubleshooting)
- [Building from Source](#building-from-source)
- [Credits & Licensing](#credits--licensing)

---

## What is Hydralum?

**Hydralum** (a combination of **Hydra** and **Malum**) is an open-source Among Us mod menu project developed and maintained by **[NewTabGames](https://github.com/NewTabGames/Hydralum)**.

Instead of choosing between MalumMenu or HydraMenu, or dealing with conflicting mod installs, Hydralum merges both menus into a unified client. Both menus operate side by side in real time, sharing game state, configuration systems, and presence networking without crashing or causing UI layout conflicts.

---

## Why MalumMenu + HydraMenu Combined?

For a long time, the Among Us modding community relied on two separate top-tier mod menus:
1. **MalumMenu** (by scp222thj and astra1dev): Famous for its polished GUI, extensive visual customization, wardrobe presets, player info tools, and client-side quality-of-life tweaks.
2. **HydraMenu** (by MrDiamond64): Renowned for its host controls, anticheat validation engine, batched network messaging, and troll utilities.

**Hydralum solves the fragmentation** by bundling customized, bug-fixed versions of both menus into one seamless install:
- **Instant In-Game Switching**: Switch between the Malum and Hydra interfaces instantly using the in-game switch button or dedicated hotkeys (`Delete` for Malum, `F6` for Hydra).
- **Persistent Configuration**: Hydra previously lacked a save system; Hydralum provides persistent config saving across restarts for both menus.
- **De-Duplicated Features**: Redundant overlapping toggles are cleaned up so both menus complement each other cleanly.
- **Live User Presence**: Real-time lobby and menu presence network connecting Hydralum users.

---

## Key Features

### 🎮 Dual-Menu System
- **MalumMenu Plus**: Full access to all Malum tabs (Self, Movement, Visuals, Host, Trolling, Ship, Doors, Config, Info, and Stuff).
- **HydraMenu**: Full access to Hydra sections (Self, Movement, Visual, Host, Sabotage, Players, Troll, Anticheat, Spoofer, and Menu).
- **Synchronized State**: Enabling a feature in one menu is recognized and respected across the entire game environment.

### 👔 Wardrobe Manager & Outfit Presets
- **JSON Loadout Presets**: Save, name, load, overwrite, and manage unlimited cosmetic combinations (colors, hats, visors, skins, pets, and nameplates).
- **In-Game Laptop Overlay**: Automatically opens a draggable preset manager whenever you access the in-game customization laptop.
- **1-Click Outfit Cloner**: Instantly clone and wear the exact cosmetic loadout of any player in your lobby with a single click.

### 🛡️ Anticheat & Security Protections
- **Incoming RPC Validator**: Comprehensive validation of 18+ RPC types to protect your game client from malicious lobby packets.
- **Scene Change Guard**: Detects and blocks unauthorized scene-change exploits (such as forced Tutorial map spawning).
- **Vent Sabotage Detector**: Flags and intercepts invalid sabotage commands originating from inside vents.

### 🚪 Enhanced Sabotage & Door Controls
- **Fungle Doors Support**: Complete door sabotage and control across all Fungle rooms (Storage, Kitchen, Lab, Lookout, Mining Pit, Comms, Reactor).
- **Spam Close & Perma-Lock**: High-frequency re-close loop (100ms) that prevents players from slipping through closing doors.
- **Unfixable Sabotage Toggles**: Force persistent electrical lights, comms, and reactor states.

### ⚡ Networking & Movement Tools
- **Named Vent Teleportation**: Integrated vent maps for The Skeld and Polus displaying actual room names instead of arbitrary index numbers.
- **Reliable Packet Sequencing**: Super-sequencing and reliable network delivery preventing movement desync during teleportation.
- **Spectate Mode**: Follow any player smoothly with camera tracking and local shadow disabling.

---

## Installation Guide

### Option A: 1-Click Standalone (Recommended)

The standalone package includes everything pre-configured (BepInEx 6 IL2CPP + Hydralum DLLs):

1. Go to the **[Actions Tab](https://github.com/NewTabGames/Hydralum/actions)** on GitHub.
2. Click the latest successful build run.
3. Scroll down to **Artifacts** and download **`Hydralum-Standalone-Steam`**.
4. Locate your Among Us installation folder:
   - **Steam**: Right-click **Among Us** in your Steam library -> **Manage** -> **Browse local files**.
   - **Epic Games**: Navigate to your install path (e.g., `C:\Program Files\Epic Games\AmongUs`).
5. Extract all contents from the zip archive directly into your game folder (so `winhttp.dll` and the `BepInEx` folder sit in the same folder as `Among Us.exe`).
6. Launch Among Us.
7. Press **`Delete`** (for MalumMenu) or **`F6`** (for HydraMenu) to open the menus!

---

### Option B: Manual Plugin Install (Existing BepInEx Users)

If you already have **BepInEx 6 IL2CPP (x86)** installed:

1. Download **`Hydralum-DLLs`** from the **[Actions Tab](https://github.com/NewTabGames/Hydralum/actions)** (under Artifacts on the latest build).
2. Extract `MalumMenuPlus.dll` and `HydraMenu.dll` into your `Among Us/BepInEx/plugins/` folder.
3. Launch Among Us and press **`Delete`** or **`F6`**!

---

### Manual BepInEx 6 Setup

Among Us is built on Unity IL2CPP, which requires **BepInEx 6 (IL2CPP)** instead of standard BepInEx 5.

> **Important**: Do **NOT** install BepInEx 5 (`BepInEx_win_x64_5.4.x.zip`). That version is designed for Mono games and will not work with Among Us.

1. **Download BepInEx 6 IL2CPP (x86)**:
   - [Direct Download: BepInEx 6 IL2CPP x86 Build 785](https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.785%2B6abdba4.zip)
   - Or browse builds on the [BepInEx Bleeding Edge Portal](https://builds.bepinex.dev/projects/bepinex_be).
2. **Extract Files**: Extract the zip into your game folder next to `Among Us.exe`.
3. **First Run**: Launch the game once so BepInEx can generate its folder structure, then close the game and drop the Hydralum DLLs into `BepInEx/plugins/`.

---

## Controls & Keybinds

| Keybind | Action | Description |
| :--- | :--- | :--- |
| **`Delete`** | **Toggle MalumMenu** | Opens or closes the main MalumMenu interface. Customizable in Config tab. |
| **`F6`** | **Toggle HydraMenu** | Opens or closes the HydraMenu interface. |
| **`F8`** | **Toggle Dev Console** | Opens the Hydralum Developer Console and live presence inspector. |
| **`Escape`** | **Dismiss Overlays** | Closes Match Info Guides, outfit laptop menus, and active subwindows. |
| **`Left / Right Arrow`** | **Vent Network Hop** | Navigates to the previous or next vent when vent cheats are active. |
| **`Up / Down Arrow`** | **Scroll Hydra Sections** | Quickly navigates through HydraMenu sections. |
| **`Page Up / Page Down`** | **Switch Hydra Tabs** | Cycles through HydraMenu tab categories. |
| **`Ctrl + C / V / X`** | **Text Box Shortcuts** | Copy, paste, and cut in custom menu input fields. |
| **Panic Key / Button** | **Panic Mode** | Instantly hides all active menus and overlays for safety. |

---

## System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit).
- **Game Version**: Among Us (Steam, Epic Games Store, or Xbox PC App).
- **Mod Loader**: BepInEx 6 Unity IL2CPP (Win x86 Build 785+).
- **Runtime**: [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) (included with Windows / BepInEx).

---

## Frequently Asked Questions (FAQ) & Troubleshooting

#### Q: The menu does not open when I press Delete or F6.
- Make sure you extracted all files directly into the root game folder where `Among Us.exe` is located, not inside a subfolder.
- Verify that `winhttp.dll` and the `BepInEx` folder exist alongside `Among Us.exe`.
- Ensure you installed **BepInEx 6 IL2CPP x86**, not BepInEx 5 (Mono).

#### Q: My antivirus flagged one of the DLLs.
- This is a standard false positive common with game modding frameworks that use Harmony memory patching. You can safely whitelist your Among Us folder in your antivirus settings.

#### Q: How do I save my outfits?
- Open the in-game customization laptop in any lobby. The Hydralum Wardrobe Manager popup will appear automatically on your screen, allowing you to save and name your current outfit.

#### Q: Can I run other mods alongside Hydralum?
- Yes, as long as the other mods are built for BepInEx 6 IL2CPP and do not conflict with Harmony patch hooks on the same methods.

---

## Building from Source

To compile Hydralum from source code:

1. Install the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Clone the repository:
   ```bash
   git clone https://github.com/NewTabGames/Hydralum.git
   cd Hydralum
   ```
3. Build both menus:
   ```bash
   # Build MalumMenu
   dotnet build MalumMenuPlus/MalumMenu-main/src/MalumMenu.csproj -c Release

   # Build HydraMenu
   dotnet build Hydra-main/src/HydraMenu.csproj -c Release

   # Build Developer Console (Optional)
   dotnet build DevMenu/src/DevMenu.csproj -c Release
   ```
4. Output DLLs are automatically compiled into their respective `bin/Release/net6.0/` directories and copied to the `DLLs/` folder.

---

## Credits & Licensing

Hydralum is built upon the incredible foundational work of the Among Us modding community:

- **[MalumMenu](https://github.com/scp222thj/MalumMenu)**: Created and maintained by **scp222thj** and **astra1dev** (Licensed under GPL-3.0).
- **[Hydra](https://github.com/MrDiamond64/Hydra)**: Created and maintained by **MrDiamond64** (Licensed under GPL-3.0).
- **[Hydralum](https://github.com/NewTabGames/Hydralum)**: Maintained by **NewTabGames** (Licensed under GPL-3.0).

This project is licensed under the terms of the **GNU General Public License v3.0 (GPL-3.0)**.