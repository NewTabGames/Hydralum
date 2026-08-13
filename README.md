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

If you don't have BepInEx IL2CPP installed yet, follow these simple steps:

1. **Download BepInEx 6 (IL2CPP x64)**:
   - Download the latest **BepInEx 6.0 IL2CPP x64** release from the [BepInEx GitHub Releases](https://github.com/BepInEx/BepInEx/releases) (look for a zip named like `BepInEx_UnityIL2CPP_x64_...zip`).
2. **Extract to your Among Us Game Folder**:
   - Open your Among Us game directory (where `Among Us.exe` is located).
     - *Steam*: Right-click **Among Us** in Steam library -> **Manage** -> **Browse local files**.
     - *Epic Games*: Navigate to your install folder (e.g. `C:\Program Files\Epic Games\AmongUs`).
   - Drag and extract all files from the BepInEx zip directly into your Among Us game folder so `winhttp.dll`, `doorstop_config.ini`, and the `BepInEx` folder sit right next to `Among Us.exe`.
3. **Run the Game Once**:
   - Launch Among Us once so BepInEx initializes its file structure, then exit the game.

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
