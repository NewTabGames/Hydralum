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
- Added a persistent configuration system for **HydraMenu** (`BepInEx/config/com.mrd.hydramenu.cfg`) that automatically saves and loads UI scale, primary theme color, notifications state, and window position.

---

## Installation & Download

1. Make sure you have **BepInEx IL2CPP** installed for Among Us.
2. Download the precompiled DLLs:
   - Go to the **[Actions tab](https://github.com/NewTabGames/Hydralum/actions)** on GitHub.
   - Click the latest successful build run.
   - Scroll down to **Artifacts** and download `Hydralum-DLLs`.
3. Extract `MalumMenuPlus.dll` and `HydraMenu.dll` into your Among Us plugins folder:
   ```text
   Among Us/BepInEx/plugins/
   ```
4. Launch Among Us.

---

## Default Controls & Keybinds

| Action | Keybind / Control |
| :--- | :--- |
| **Hydralum** | `Delete` key |

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
