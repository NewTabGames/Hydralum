# Hydralum

**Hydralum** is a dual-menu Among Us mod system for BepInEx IL2CPP, seamlessly pairing customized versions of **MalumMenu** and **HydraMenu** into a single unified in-game GUI experience.

---

## Features

- **Unified Hydralum Container**:
  - Top header bar with a **`[ Switch ]`** button to morph between MalumMenu and HydraMenu instantly.

- **MalumMenu**:
  - Themes & Gradients + live animated RGB rainbow mode.
  - Menu scale and opacity sliders.
  - Adjustable FPS Unlocker (30–240 FPS).
  - Profile persistence for scale, opacity, themes, keybinds, and toggles.
  - Vent Network (Left/Right arrow keys cycle through nearest map vents while vented).
  - Replay-style Console with 12-hour timestamps `[h:mm:ss tt]` and event logging.
  - Continuous Disable Sabotage, Disable Vents + Exclude Yourself, Door controls, and gameplay cheats.

- **HydraMenu**:
  - BepInEx configuration system (`com.mrd.hydramenu.cfg`) saving UI scale, opacity, primary theme color, notifications toggle, and window screen position.

---

## Installation

1. Make sure you have **BepInEx IL2CPP** installed for Among Us.
2. Download or copy both precompiled DLLs from the [`DLLs/`](./DLLs/) folder:
   - `MalumMenuPlus.dll`
   - `HydraMenu.dll`
3. Paste both `.dll` files into your Among Us plugins folder:
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

Both projects are configured to automatically copy output binaries to the root [`DLLs/`](./DLLs/) folder upon building:

```bash
# Build MalumMenu
cd MalumMenu/MalumMenu-main
dotnet build src/MalumMenu.csproj -c Release

# Build HydraMenu
cd ../../Hydra-main
dotnet build src/HydraMenu.csproj -c Release
```

---

## Credits & License

- **MalumMenu**: Developed by scp222thj & astra1dev (GPL-3.0).
- **Hydra**: Developed by MrDiamond64 (GPL-3.0).
- **Hydralum**: Dual-menu integration & features by the project authors.
