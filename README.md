# Hydralum

**Hydralum** is an Among Us mod system for BepInEx IL2CPP that pairs customized versions of **MalumMenu** and **HydraMenu**, allowing you to quickly switch between both menus in-game.

---

## Credits & License

- **[MalumMenu](https://github.com/scp222thj/MalumMenu)**: Developed by scp222thj & astra1dev (GPL-3.0).
- **[Hydra](https://github.com/MrDiamond64/Hydra)**: Developed by MrDiamond64 (GPL-3.0).
- **Hydralum**: Dual-menu integration & features by the project authors.

---

## Features

- **Hydralum Integration**:
  - Quick **`[ Switch ]`** header button to swap between MalumMenu and HydraMenu seamlessly.
  - Hover-aware input protection: Mouse scrolling and clicking over menu windows stay inside the GUI and won't trigger in-game camera zoom or click in-game UI buttons.

- **MalumMenu**:
  - Themes & Gradients + live animated RGB rainbow mode.
  - Menu scale and opacity sliders.
  - Adjustable FPS Unlocker (30–240 FPS).
  - Profile persistence (`MalumProfile.txt`) for scale, opacity, themes, keybinds, and toggles.
  - Vent Network (Left/Right arrow keys cycle through map vents for all roles).
  - Distance-override Disable Vents with **Exclude Yourself** support.
  - Replay-style Console with 12-hour timestamps `[h:mm:ss tt]` and event logging.
  - Continuous Disable Sabotage, Door controls, and gameplay cheats.

- **HydraMenu**:
  - BepInEx configuration system (`com.mrd.hydramenu.cfg`) saving UI scale, opacity, primary theme color, notifications toggle, and window screen position.
  - Host controls, player utilities, teleporters, and notification system.

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
