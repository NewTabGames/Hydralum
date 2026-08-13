# Hydralum

**Hydralum** is an Among Us mod system for BepInEx IL2CPP that pairs customized versions of **MalumMenu** and **HydraMenu**, allowing you to quickly switch between both menus in-game.

---

## Credits & License

- **[MalumMenu](https://github.com/scp222thj/MalumMenu)**: Developed by scp222thj & astra1dev (GPL-3.0).
- **[Hydra](https://github.com/MrDiamond64/Hydra)**: Developed by MrDiamond64 (GPL-3.0).
- **Hydralum**: Dual-menu integration & features by the project authors.

---

## Features

- Includes **MalumMenu** and **HydraMenu** with custom features and enhancements.
- Persistent configuration system for **HydraMenu** (`BepInEx/config/com.mrd.hydramenu.cfg`) that automatically saves and loads UI scale, primary theme color, notifications state, and window position.

---

## How to Install BepInEx (Easy 3-Step Guide)

Among Us is an **IL2CPP** game, so it requires **BepInEx 6 (IL2CPP)** instead of standard BepInEx 5.

> ⚠️ **Important**: Do **NOT** download BepInEx 5 (`BepInEx_win_x64_5.4.x.zip`). That version is for Mono games and will not load in Among Us.

1. **Download BepInEx 6 IL2CPP x64**:
   - **[Direct Download: BepInEx 6 IL2CPP x64 (Build 785)](https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785%2B6abdba4.zip)**
   - Alternatively, browse all builds on **[BepInEx Bleeding Edge Builds](https://builds.bepinex.dev/projects/bepinex_be)**.
2. **Extract to your Among Us Game Folder**:
   - Open your Among Us game folder (where `Among Us.exe` is located).
     - *Steam*: Right-click **Among Us** in Steam library -> **Manage** -> **Browse local files**.
     - *Epic Games*: Navigate to your install folder (e.g. `C:\Program Files\Epic Games\AmongUs`).
   - Extract all contents of the zip file directly into your game folder so `winhttp.dll`, `doorstop_config.ini`, and the `BepInEx` folder sit right next to `Among Us.exe`.
3. **Run the Game Once**:
   - Launch Among Us once so BepInEx initializes its folder structure (`BepInEx/plugins/`), then exit the game.

---

## Installing Hydralum

1. Download the precompiled DLLs:
   - Go to the **[Actions tab](https://github.com/NewTabGames/Hydralum/actions)** on GitHub.
   - Click the latest successful build run.
   - Scroll down to **Artifacts** and download `Hydralum-DLLs`.
2. Extract `MalumMenuPlus.dll` and `HydraMenu.dll` into your Among Us plugins folder:
   ```text
   Among Us/BepInEx/plugins/
   ```
3. Launch Among Us and press **`Delete`** to open the menu!

---

## Controls & Keybinds

| Action | Keybind / Control |
| :--- | :--- |
| **Toggle Menu** | `Delete` key |
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
