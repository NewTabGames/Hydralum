# Hydralum

**Hydralum** is an Among Us mod system for BepInEx IL2CPP that pairs customized versions of **MalumMenu** and **HydraMenu**, allowing you to quickly switch between both menus in-game.

---

## Credits & License

- **[MalumMenu](https://github.com/scp222thj/MalumMenu)**: Developed by scp222thj & astra1dev (GPL-3.0).
- **[Hydra](https://github.com/MrDiamond64/Hydra)**: Developed by MrDiamond64 (GPL-3.0).
- **Hydralum**: Dual-menu integration & features by the project authors.

---

## Features

- **Dual-Menu Integration**: Seamlessly switch between **MalumMenu** and **HydraMenu** in real time with synchronized toggles and state sharing.
- **JSON Outfit Presets & Wardrobe Manager**: Save, name, load, apply, overwrite, and rename unlimited cosmetic loadouts (colors, hats, visors, skins, pets, and nameplates) stored as `.json` files in `BepInEx/config/Outfits/`.
  - **In-Game Wardrobe Overlay**: Automatically pops up a draggable preset manager when opening the wardrobe/inventory laptop to save or apply outfits on the fly.
  - **1-Click Outfit Cloner**: Instantly clone, equip, or save the cosmetic loadout of any player in your lobby with a single click.
- **Persistent Hydra Config System**: Original Hydra never had a configuration system and would reset all settings whenever the game was closed. Hydralum introduces a full persistent configuration system (`BepInEx/config/com.mrd.hydramenu.cfg`) that automatically saves and restores your UI scale, custom theme colors, notification preferences, and window positions across restarts.

---

## Installation

### Option A: 1-Click Drag & Drop Standalone Package (Recommended)

1. Go to the **[Actions tab](https://github.com/NewTabGames/Hydralum/actions)** on GitHub.
2. Click the latest successful build run.
3. Scroll down to **Artifacts** and download **`Hydralum-Standalone-Steam`**.
4. Open your Among Us game folder:
   - *Steam*: Right-click **Among Us** in Steam library -> **Manage** -> **Browse local files**.
5. Extract all contents of the zip file directly into your game folder (where `Among Us.exe` is located).
6. Launch Among Us and press **`Delete`** to open the menu!

---

### Option B: Manual Installation (Existing BepInEx Users)
If you already have **BepInEx 6 IL2CPP** installed:
1. Download **`Hydralum-DLLs`** from the **[Actions tab](https://github.com/NewTabGames/Hydralum/actions)**.
2. Extract `MalumMenuPlus.dll` and `HydraMenu.dll` into your `Among Us/BepInEx/plugins/` folder.
3. Launch Among Us and press **`Delete`**!

---

## How to Install BepInEx Manually (If Not Using Standalone)

Among Us is an **IL2CPP** game, so it requires **BepInEx 6 (IL2CPP)** instead of standard BepInEx 5.

>  **Important**: Do **NOT** download BepInEx 5 (`BepInEx_win_x64_5.4.x.zip`). That version is for Mono games and will not load in Among Us.

1. **Download BepInEx 6 IL2CPP x86**:
   - **[Direct Download: BepInEx 6 IL2CPP x86 (Build 785)](https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x86-6.0.0-be.785%2B6abdba4.zip)**
   - Alternatively, browse all builds on **[BepInEx Bleeding Edge Builds](https://builds.bepinex.dev/projects/bepinex_be)**.
2. **Extract to your Among Us Game Folder**:
   - Open your Among Us game folder (where `Among Us.exe` is located).
     - *Steam*: Right-click **Among Us** in Steam library -> **Manage** -> **Browse local files**.
     - *Epic Games*: Navigate to your install folder (e.g. `C:\Program Files\Epic Games\AmongUs`).
   - Extract all contents of the zip file directly into your game folder so `winhttp.dll`, `doorstop_config.ini`, and the `BepInEx` folder sit right next to `Among Us.exe`.
3. **Run the Game Once**:
   - Launch Among Us once so BepInEx initializes its folder structure (`BepInEx/plugins/`), then exit the game.

---

## Controls & Keybinds

| Action | Keybind / Control |
| :--- | :--- |
| **Toggle Menu (Malum)** | `Delete` key |
| **Switch Menu** | `[ Switch ]` header button in top-right of menu |

---

## Building from Source

Prerequisites: [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).

Both projects are configured to automatically copy output binaries to the local `DLLs/` folder upon building:

```bash
# Build MalumMenu
cd MalumMenuPlus/MalumMenu-main
dotnet build src/MalumMenu.csproj -c Release

# Build HydraMenu
cd ../../Hydra-main
dotnet build src/HydraMenu.csproj -c Release
```









This is made by ai ofc. I thought of a cool project and wanted to build it.
I didn't want to spend months/years learning how to code and once I finally
learned fully, AU is probably gonna be completely dead. Not like it is already.
At this point, there isn't even a point in learning how to code manually.
All you really need to do is understand how it works.