# Hydralum Feature Documentation

Comprehensive catalog and guide for every feature, toggle, cheat, protection, and utility across **HydraMenu** and **MalumMenuPlus** within Hydralum.

---

## Table of Contents

1. [Hydralum Integration & Architecture](#hydralum-integration--architecture)
2. [HydraMenu Features](#hydramenu-features)
   - [1. General](#1-general)
   - [2. Self](#2-self)
   - [3. Troll](#3-troll)
   - [4. Host](#4-host)
   - [5. Roles](#5-roles)
   - [6. Players](#6-players)
   - [7. Visual](#7-visual)
   - [8. Protections](#8-protections)
   - [9. Anticheat](#9-anticheat)
   - [10. Spoofer](#10-spoofer)
   - [11. Themes](#11-themes)
   - [12. Menu](#12-menu)
   - [13. Info](#13-info)
3. [MalumMenuPlus Features](#malummenuplus-features)
   - [1. Self (Movement)](#1-self-movement)
   - [2. ESP](#2-esp)
   - [3. Roles](#3-roles)
   - [4. Ship](#4-ship)
   - [5. Schizo (Client Desync)](#5-schizo-client-desync)
   - [6. Lobbies (History & Rejoin)](#6-lobbies-history--rejoin)
   - [7. Chat & Chat Sender](#7-chat--chat-sender)
   - [8. Players](#8-players)
   - [9. Outfits & Wardrobe](#9-outfits--wardrobe)
   - [10. Console & RPC Debugger](#10-console--rpc-debugger)
   - [11. Host Only](#11-host-only)
   - [12. Votekick System](#12-votekick-system)
   - [13. Config & Profiles](#13-config--profiles)
   - [14. Themes](#14-themes)
   - [15. Info](#15-info)
   - [16. Stuff (Meme / Joke Tab)](#16-stuff-meme--joke-tab)
4. [MalumMenuPlus Subwindows & Pop-ups](#malummenuplus-subwindows--pop-ups)
   - [Interactive Radar Minimap](#interactive-radar-minimap)
   - [Match Replay Console](#match-replay-console)
   - [Player Pick Menu (PPM)](#player-pick-menu-ppm)
   - [Doors Control Subwindow](#doors-control-subwindow)
   - [Task Manager Subwindow](#task-manager-subwindow)
   - [Protect Players Subwindow](#protect-players-subwindow)
   - [Assign Roles Subwindow](#assign-roles-subwindow)
   - [Chat Log Subwindow](#chat-log-subwindow)
   - [Wardrobe Overlay Subwindow](#wardrobe-overlay-subwindow)
   - [Pop-up Window Scales Submenu](#pop-up-window-scales-submenu)
   - [Keybind Settings Submenu](#keybind-settings-submenu)
   - [Event Logging Console Subwindow](#event-logging-console-subwindow)
   - [RPC Debugger Console Subwindow](#rpc-debugger-console-subwindow)

---

# Hydralum Integration & Architecture

* **In-Place Menu Switcher**: Both menus feature a dedicated "Switch" button in their top title headers, allowing instant toggling between MalumMenu and HydraMenu without desyncing screen coordinates or requiring menu re-openings.
* **Unified Keybind System**: Pressing `Delete` toggles whichever menu was active last, preventing dual-window overlap or conflicting key interceptors.
* **Shared Custom Themes (`CThemes`)**: Any custom solid hex or animated wave gradient theme saved in one menu is automatically persisted in the shared `CThemes` storage and becomes immediately available in the other menu.
* **Live Presence Tracker**: Real-time heartbeat network displaying the active count of online Hydralum users directly on both menu title bars.
* **Mod Detection Engine**: Real-time packet fingerprinting that detects and identifies modded clients (such as Hydra, Malum, Sicko, and others) sending custom RPCs.
* **Developer Firewall**: Safeguards developer accounts from outbound hostile actions (petting, jail, follow, murder, teleports, etc.).
* **Remote Announcements & Update Lockout Modal**: Connects to the Hydralum Firebase backend to display real-time global announcement toast banners and render a full-screen lockout modal with a direct update link whenever a required version threshold is broadcast.

---

# HydraMenu Features

### 1. General
* **Welcome & Orientation**: Guidelines and disclaimers regarding menu usage and fair-play recommendations.
* **Clear Notifications**: Clears all active on-screen notification toast popups and dispatches a confirmation notice.
* **F6 Notification Test Key**: Dedicated debug shortcut to test the on-screen notification toast banner system.

### 2. Self
* **Role Display**: Real-time indicator displaying your current `RoleType` in the lobby or active match.
* **Always Show Task Animations**: Forces visual task animations (shields, weapons, trash chute, medbay scan) to render on your screen regardless of lobby visual task settings.
* **Become Immortal**: Prevents impostors from killing you locally by intercepting damage and kill checks.
* **No Ladder Cooldown**: Removes the climb cooldown when using ladders on The Airship and The Fungle.
* **No Zipline Cooldown**: Removes the interaction delay when using ziplines on The Fungle.
* **Unlimited Meetings**: Allows calling emergency meetings without consuming your remaining meeting count.
* **Update Stats in Freeplay**: Tracks and increments your official game achievements, tasks, and statistics while playing in Freeplay mode.
* **Avatar Controls**:
  * **Randomize Avatar**: Randomizes your current skin, hat, visor, pet, and player name (when disconnected).
  * **Randomize Color**: Instantly assigns an unused color from the lobby palette.
  * **Restore Avatar**: Restores your default saved cosmetics and original color configuration.

### 3. Troll
* **Queue Lobby Crash**: Queues a destructive packet sequence designed to desync and terminate the current lobby.
* **Auto Trigger Spores**: Continuously pops mushroom spores on The Fungle to blanket the map in spore clouds.
* **Disable Security Cameras**: Shuts off camera surveillance feeds across the ship.
* **Disable Close Doors**: Blocks impostors from closing and locking doors.
* **Make Everyone Immortal**: Protects all players in the match from being killed.
* **Glitterbomb**: Rapidly cycles cosmetics and visual particle bursts.
* **Kick All Players**: Broadcasts an invalid ventilation system update to disconnect all non-host players.
* **Copy Random Player**: Instantly copies the cosmetics and appearance of a randomly selected player.
* **Trigger All Spores**: Explodes every mushroom spore simultaneously across The Fungle.
* **Deplete HnS Timer**: Exploit for Hide and Seek mode that rapidly spams batched `CompleteTask` RPCs to instantly deplete the escape countdown timer to zero.
* **Vent Teleport / Teleport Flooder**:
  * **Teleport Flooder**: Global or player-specific toggle to flood player positions into vents.
  * **Exclude Yourself**: Protects local player from being pulled into the vent loop.
  * **Vent Slider**: Interactive slider listing all map vents by room name.
  * **Teleport to Vent**: Teleports all players to the selected vent.
  * **Teleport to Random Vent**: Teleports all players across random vents on the ship.
* **Door Troller**:
  * **Door Troller Toggle**: Continuously cycles locking and unlocking all doors on the map.
  * **Lock and Unlock Delay Slider**: Configurable cycle interval from 0.10s to 2.00s.
* **Auto Expose Impostors**:
  * **Master Toggle**: Automatically announces impostors when illegal actions occur.
  * **Expose On Murder**: Flags the killer immediately when a murder occurs.
  * **Expose On Shapeshift**: Flags players when shapeshifting into another character.
  * **Expose On Phantom**: Flags Phantoms when activating invisibility.

### 4. Host
* **Be Able to Ban Players Mid-Game**: Unlocks host ban capabilities during active gameplay rounds.
* **Use Flipped Skeld Map**: Loads the mirrored layout of The Skeld.
* **Disable Game End**: Blocks win conditions from triggering, allowing the match to continue indefinitely.
* **Disable Vent Clean**: Blocks Engineers from cleaning vents.
* **Fake Shapeshift Bubble**: Spawns cosmetic shapeshift smoke particles without transforming.
* **Kick Players with Low Levels**:
  * **Block Low Levels Toggle**: Automatically kicks joining players below a set level threshold.
  * **Minimum Level Slider**: Configurable level threshold from 0 to 100.
* **Force Start Game**: Starts the game countdown immediately without waiting for player readiness (works in online host, local, and freeplay).
* **Kill Everyone**: Instantly murders all players using batched `MurderPlayer` RPC calls.
* **Force Crewmate Victory**: Concludes the match with an immediate Crewmate Task victory.
* **Force Imposter Victory**: Concludes the match with an immediate Impostor Kill victory.
* **Boom 480**: Host lobby-crash utility: despawns the lobby prefab, fires `StartGame` 98 times, and forces a crewmate victory.
* **Boom 480 Buffed**: Enhanced lobby-crash: despawns the lobby prefab, fires `StartGame` 110 times, and forces a crewmate victory.
* **Map Spawner**:
  * **Map Selector Slider**: Select from The Skeld, MIRA HQ, Polus, The Airship, The Fungle, or Submerged (IDs 0–5).
  * **Spawn Map**: Dynamically instantiates the selected map prefab into the current room.
  * **Despawn Map**: Removes the spawned map object.
  * **Spawn Map (x100)**: Spawns 100 stacked map instances with rate-limit pacing to avoid disconnection.
  * **Clear All**: Despawns every spawned map and lobby instance cleanly.
  * **Spawn Lobby**: Spawns an additional dropship lobby instance.
  * **Despawn Lobby**: Removes the spawned dropship lobby instance.
* **Assign Roles for Next Round**:
  * **Enabled Toggle**: Forces role assignments when the next round starts.
  * **Role Selector Slider**: Horizontal slider supporting all vanilla and modded role types with localized display names.
* **Meeting Controls**:
  * **Disable Meetings**: Prevents emergency meetings and body reports from being called.
  * **Spam Report Bodies**: Repeatedly triggers body report meetings across the lobby.
  * **Vote Immune**: Protects local player from receiving votes during meetings.
  * **Spam Votes**: Floods meeting ballots with rapid automated votes.
  * **Close Meeting**: Instantly terminates active meetings with empty vote packets.
* **Shapeshift Controls**:
  * **Shapeshift Everyone Into Me**: Forces every player to shapeshift into your character.
  * **Shapeshift Everyone Into Random**: Shapeshifts all players into random lobby members.
  * **Revert All Shapeshifts**: Restores all players back to their original forms.
* **Color Controls**:
  * **Color Selector Slider**: Chooses target color ID (0 to 17).
  * **Change Colors Button**: Sets every player's color to the selected color simultaneously.
  * **Disco Party**: Continuously cycles random player colors across all or targeted players.
  * **Color Randomization Delay Slider**: Configurable interval from 0.10s to 2.00s.
* **Zipline Spammer**: (The Fungle only) Global or player-specific routine that repeatedly forces players onto the zipline.

### 5. Roles
* **Move In Vents**: Allows walking and maneuvering freely across the map while inside the vent state.
* **Allow Sabotaging In Vents As Imposter**: Unlocks the sabotage UI button while inside vents.
* **No Sabotage Cooldown**: Completely eliminates the cooldown timer between triggering sabotages.
* **Phantom Cheats**:
  * **Endless Vanish Duration**: Keeps Phantom invisibility active permanently.
  * **No Vanish Cooldown**: Vanish instantly without waiting for ability recharge.
* **No Kill Checks**:
  * **Master Toggle**: Bypasses target distance and state validation for kills.
  * **Kill Other Impostors**: Enables friendly fire against other impostor teammates.
  * **Kill While Vanished**: Allows Phantoms to perform kills without breaking vanish.
  * **No Kill Cooldown (Host-Only)**: Removes the kill cooldown timer for immediate re-kills.
  * **Kill Ghosts (Host-Only)**: Allows murdering dead ghost players.
* **Change Role**:
  * **Role Slider**: Selects target role from all available game roles.
  * **Apply Role**: Applies role immediately (authoritatively synced if host; locally applied if client).

### 6. Players
* **Multi-Select Support**: Hold `Ctrl` and click player cards to select multiple players simultaneously for batch operations.
* **Live Player Info Header**:
  * Player Name, Color, Role, and Alive/Dead state.
  * Client Fingerprint: Detects Unmodded, Hydra, Malum, and other custom clients via `ModDetection`.
  * Friend Code and Product User ID (PUID), with Streamer Mode redaction.
  * Account Level (+1 format) and Platform/Device (Steam, Epic, Android, iOS, Xbox, PlayStation, Switch).
  * Host indicator badge.
* **Single-Target Player Actions**:
  * **Copy Name**: Copies the player's clean username to clipboard without formatting tags.
  * **Spectate**: Follows target player with free camera.
  * **Pet Player**: Continuously pets the target's equipped pet.
  * **Follow**: Locks your movement to trail directly behind target.
  * **Place in Jail**: Confines target player inside an impassable position boundary.
  * **Immortal**: Toggles immortality on target player.
  * **Teleport**: Snaps directly to target player position.
  * **Teleport All To**: Teleports all players to target player's position (host-authoritative).
  * **Murder**: Attempts killing target via `RpcMurderPlayer` (host/no AC) or `CmdCheckMurder` (impostor).
  * **Copy Avatar**: Duplicates target's cosmetics and colors onto yourself.
  * **Report Body**: Starts a meeting targeting player's body.
  * **Kick Player**: Disconnects target from lobby.
  * **Teleport Player to Vent**: Slider listing all map vents + Teleport button.
  * **Whisper**: Sends private chat message to target.
  * **Host-Only Targeted Features**:
    * **Disco Mode**: Cycles random colors specifically on target player.
    * **Force Zipline**: (The Fungle) Forces target onto zipline.
    * **Force Meeting As**: Calls emergency meeting on target's behalf.
    * **Force All Votes To**: Forces every voter in a meeting to vote for target.
    * **Eject**: Instantly triggers ejection cutscene on target.
    * **Frame Shapeshift**: Forces target to transform into a random player.
    * **Frame for Killing All**: Shapeshifts you into target, murders the lobby, and frames them.
    * **Flood Player with Tasks**: Spams 255 tasks into target's task list.
    * **Clear Tasks**: Wipes all tasks from target.
    * **Game Options Modifier (Targeted)**:
      * **Blind**: Sets light vision multiplier to -1.0x.
      * **Fullbright**: Sets light vision multiplier to 1000x.
      * **Slow Speed**: Reduces target speed to 0.1x.
      * **Super Speed**: Boosts target speed to maximum allowed limit.
      * **Reset to Defaults**: Restores default lobby game options.
    * **Change Color**: Assigns any color (0 to 17) to target.
* **Multi-Target Actions (When Multiple Players Selected with Ctrl)**:
  * Selected targets summary chip list & Deselect All button.
  * **Murder Selected**: Kills all selected targets at once.
  * **Teleport All to Me**: Snaps all selected players to your location.
  * **Place All in Jail / Release All**: Confines or releases all selected players.
  * **Kick Selected**: Kicks all chosen players simultaneously.
  * **Make Selected Immortal / Mortal**: Toggles immortality across the group.
  * **Teleport to Vent**: Teleports all selected players to chosen vent.
  * **Host Multi-Target Controls**:
    * Enable / Disable Disco Mode on Selected.
    * Force All to Zipline / Release All (Fungle).
    * Eject Selected.
    * Flood Tasks / Clear Tasks across selected players.
    * Batch Game Options Modifiers (Blind, Fullbright, Slow Speed, Super Speed, Reset).
    * Set Color on all selected players.

### 7. Visual
* **Skip Shhh Animation**: Skips the opening role reveal silence animation.
* **Skip Seeker Animation**: Skips the seeker reveal animation in Hide and Seek mode.
* **Use More Accurate Disconnection Reasons**: Displays exact internal error codes and disconnect packets upon leaving a lobby.
* **Show Guardian Angel Protections**: Renders Guardian Angel protective shields visually on protected players.

### 8. Protections
* **Force Enable DTLS**: Enforces DTLS encryption for all outbound and inbound network packets.
* **Bypass Disconnection Penalties**: Bypasses matchmaking timeout bans when leaving active matches.
* **Block Position Updates from Server**: Blocks server and host-authoritative teleports from moving your character.
* **Block Unauthorized System Updates**: Rejects illegal sabotage and system update packets from non-hosts.
* **Bypass Ratelimits for Shapeshift RPC**: Allows shapeshifting without client-side cooldown lockouts.
* **Anti Crash**: Protects client from the `ReportDeadBody` packet flood crash exploit.
* **Anti Overload**:
  * **Block Large Game Messages**: Drops oversized packet payloads designed to crash or lag client.
  * **Block Invalid GameData Message Types**: Drops malformed or corrupt GameData packets.
  * **Use Hardened Packed Int Deserializer**: Protects against integer overflow and memory corruption exploits.
  * **Protect Against VotingComplete Overloads**: Prevents voting packet flood client freezes.
* **Anti Kick**:
  * **Protect Against Votekicks as Host**: Prevents malicious clients from votekicking the host out of their own room.
  * **Protect Against Host-Only RPC Kick Exploit**: Blocks unauthorized clients from sending illegal kick RPC packets.

### 9. Anticheat
* **Enable Hydra Anticheat**: Master toggle for the real-time RPC packet inspection engine.
* **Flag Spoofed Platform Data**: Flags players falsifying device or OS identity.
* **Granular RPC Validators**:
  * `PlayAnimation` (illegal visual task animations)
  * `CompleteTask` (completing unassigned tasks)
  * `Exiled` (unauthorized ejection calls)
  * `CheckName` / `SetName` (name length violations and illegal formatting injections)
  * `SetColor` (out-of-bounds palette indices)
  * `ReportDeadBody` (reporting unspawned bodies or reporting across walls)
  * `SetScanner` (fake medbay scanner animations)
  * `SetStartCounter` (starting countdown without host authority)
  * `EnterVent` / `ExitVent` (venting without an authorized role)
  * `SnapTo` (illegal teleports and speed exploits)
  * `AddVote` (voting out of turn or during invalid phases)
  * `CloseDoorsOfType` (unauthorized door locking)
  * `ClimbLadder` (ladder teleportation exploits)
  * `UsePlatform` (Airship gap platform exploits)
  * `UpdateSystem` (sabotaging without role / while venting)
  * `SetLevel` (spoofed level packets)
* **GameData Packet Validators**: `SceneChange` and `ClientReady`.
* **Cheater Responses**:
  * **Send Notification**: Displays on-screen alert when an exploit is caught.
  * **Discard RPC**: Discards illegal RPCs before they affect local game state.
  * **Punishment Modes**: Configurable response (None, Kick, ErrorKick, Ban).

### 10. Spoofer
* **Version Spoofer**:
  * **Enable Version Spoofing**: Spoofs handshake version during server connection.
  * **Spoofed Version Slider**: Presets for Current AddressablesVersion, 16.1.0, 17.1, 17.1.2, 17.2, 17.2.1, 17.2.2, 17.3, 17.4, and 18.0.
  * **Use Modded Protocol**: Enables protocol handshake modes for custom community servers (+25 protocols).
* **Level Spoofer**:
  * **Enabled Toggle**: Spoofs displayed account level.
  * **Spoofed Level Slider**: Slider from 1 to 200.
  * **Quick Buttons**: `-100`, `-10`, `+10`, `+100` (supports levels up to 100,001+).
  * **Send Level Update**: Dispatches `RpcSetLevel` packet to sync spoofed level to lobby.
* **Platform Spoofer**:
  * **Enable Platform Spoofing**: Spoofs your reported client device.
  * **Spoofed Platform Slider**: Cycle between PC, Android, iOS, Nintendo Switch, Xbox, PlayStation, and others.

### 11. Themes
* **Menu Opacity Slider**: Adjusts menu transparency (0% to 100%).
* **UI Scale Slider**: Adjusts menu font and element scaling (0.50x to 2.00x).
* **RGB Mode**: Animated rainbow cycling accent.
* **Custom Color**:
  * Solid Hex Input (IL2CPP-safe typing) + color preview swatch + Apply button.
  * Custom Gradient Input (Start Hex & End Hex) + live animated preview + Apply Gradient button.
  * Saved Themes (shared across Hydra & Malum via `CThemes` folder):
    * Name input field + Save button.
    * Two-column saved themes list with live color preview, one-click Apply, and Delete (x).
* **Solid Themes Presets**: 8 clean presets (Default, Malum, Ocean, Emerald, Crimson, Sunset, Gold, Bubblegum).
* **Gradients Presets**: 24 animated wave presets (Fire, Aurora, Galaxy, Ocean, Sunset, Mint, Cyberpunk, Vaporwave, Solar Flare, Matrix, Midnight, Amethyst, Blood Orange, Neon Lime, Lavender, Iceberg, Sakura, Synthwave, Cosmic, Emerald Forest, Electric Rose, Gold Mirage, Abyss, Tropical).

### 12. Menu
* **Disable Notifications**: Suppresses on-screen notification toast popups.
* **Eject**: Cleanly unloads Hydra and its Harmony hooks from game memory.
* **Config Profile Management**:
  * Current config name display.
  * Selected config slider.
  * Save, Load, and Delete config buttons.
  * Name field with IL2CPP-safe typing.
  * New button (creates auto-numbered or custom named configs).
  * Rename button.
  * Open Config Folder button (opens config folder in Windows Explorer).

### 13. Info
* **Version Display**: Hydralum version, Malum Menu version, and Hydra Menu version.
* **Live Users Online Counter**: Displays the number of active Hydralum users online in real time.
* **Invisible Name Method**:
  * Open `player.amogus` Folder button.
  * Copy `\u00AD` (soft hyphen) button.
  * Copy `\u2060` (word joiner) button.
* **Credits & Community Links**: Quick-launch buttons to open official GitHub and Discord links for Hydralum, MalumMenu, and Hydra.

---

# MalumMenuPlus Features

### 1. Self (Movement)
* **Invisibility**: Serverside networked invisibility (invisible to other players on server).
* **Disco**: Rapidly cycles your player color (Speed slider from 1 to 7 changes/sec; up to 100 as host).
* **NoClip**: Walk through walls, objects, and map colliders.
* **Invert Controls**: Reverses movement input directions.
* **Speed Controls**:
  * Dedicated sliders for **Ghost Speed** and **Alive Speed** (0.0x to 20.0x).
  * Snap to Default threshold.
  * **Reset to Default** button.
* **Teleportation**:
  * **to Cursor**: Teleports player directly to mouse click position.
  * **to Player**: Teleports player directly onto any chosen player via PPM.
  * **Use SnapTo RPC**: Uses authoritative SnapTo RPCs for instant teleportation.
* **Hand Animations** (Serverside hand gestures visible to everyone):
  * **Enable Hand Animation Toggle**.
  * **Target Selector**: Target yourself or any other player in the lobby.
  * **13 Animation Patterns**: Goon, Orbit, Figure 8, Halo, Wave, Head Pat, Shield, Heart, Boomerang, Barrage, Spiral, Tornado, Foot Tickle.
  * **Speed Slider**: Configurable from 0.5x to 8.0x.
  * **Radius Slider**: Configurable from 0.2 to 3.5.
* **Teleport to Location**: Pre-configured instant teleport buttons for all rooms across The Skeld, MIRA HQ, Polus, The Airship, The Fungle, and Submerged.

### 2. ESP
* **Hydralum Users (Client-Sided)**:
  * **Disable My Gem**: Hides the Hydralum user diamond above your character.
  * **Disable All Gems**: Hides all Hydralum diamonds across the lobby.
* **Players ESP**:
  * **See Player Info**: Displays role, level, client platform, and host badge above player heads.
  * **See Roles**: Reveals true roles for all players.
  * **See Friend Code**: Displays friend codes on player nametags.
  * **Name Tag Colors**: Colors nametags based on role and team allegiance.
  * **See Ghosts**: Renders ghost players and allows seeing ghost movement while alive.
  * **Chat Color Tags**: Colors player usernames in chat to match their character color.
  * **Reveal Votes**: Shows who each player voted for during emergency meetings.
* **Cooldowns ESP**:
  * **Impostor Kill**: Displays live kill cooldown timers above impostors.
  * **Guardian Angel Protect**: Displays shield cooldown timers.
  * **Advanced Role Cooldown**: Per-role ability cooldown timers (Kill, Viper, Shapeshift, Vanish, Engineer Vent).
* **World ESP**:
  * **Task Arrows**: Guides you directly to uncompleted task stations.
  * **Vent ESP**: Highlights all vents through walls.
  * **No Shadows**: Fullbright lighting mode; completely removes darkness and flashlight cones.
* **Overlay**:
  * **See Lobby Info**: Displays lobby room code, region, map, and privacy settings.
  * **Show Ping**: Centered HUD showing network latency with color-coded tiers.
  * **Show FPS**: Real-time frames-per-second counter.
  * **Show Clock**: Live date and time display pinned to bottom-left corner.
* **Camera Controls**:
  * **Zoom Out**: Expands game camera zoom for wide map awareness.
  * **Spectate**: Camera locks onto and follows chosen player.
  * **Freecam**: Move the camera freely using arrow keys or WASD.
* **Tracers**:
  * Lines drawn from your player to: Crewmates, Impostors, Ghosts, Dead Bodies.
  * **Color-Based**: Tracers match target player color.
  * **Distance-Based**: Tracers shift color based on proximity.
* **Minimap HUD Overlay**:
  * Renders Crewmates, Impostors, and Ghosts on the in-game map.
  * Color-based player indicators.
* **Interactive Radar Minimap**: Embedded procedural radar overlay (see [Subwindows](#interactive-radar-minimap)).
* **Match Replay Console**: Real-time replay recording HUD (see [Subwindows](#match-replay-console)).

### 3. Roles
* **General**:
  * **Set Fake Role**: Spoofs your local role via PPM.
  * **Set Fake Alive**: Locally toggles alive/dead state via PPM.
* **Impostor**:
  * **Kill Reach**: Infinite kill distance for Impostors.
  * **Allow Tasks**: Allows Impostors to interact with and complete task consoles.
* **Shapeshifter**:
  * **No Ss Animation**: Removes shapeshift smoke and transformation delay.
  * **Endless Ss Duration**: Stay shapeshifted indefinitely.
  * **No Ss Cooldown**: Instant shapeshifting without cooldown.
* **Crewmate**:
  * **Show Tasks Menu**: Opens the Task Manager subwindow.
  * **Complete All Tasks Button**: Instantly completes all assigned tasks.
  * **Auto Complete Tasks**: Automatically completes tasks over time.
  * **Disable if Taskbar Updates = Always**: Safety toggle to avoid detection.
  * **Auto Complete Start Delay Slider**: 0s to 60s delay before auto-completion begins.
  * **Time Between Tasks Slider**: 0s to 20s interval between completing tasks.
* **Tracker**:
  * **Endless Tracking**: Tracking arrows remain active permanently.
  * **No Track Delay**: Instant tracking activation.
  * **No Track Cooldown**: Track players without cooldown timers.
  * **Track Reach**: Track players from infinite distance.
* **Engineer**:
  * **Endless Vent Time**: Stay inside vents indefinitely.
  * **No Vent Cooldown**: Vent without cooldown delay.
* **Scientist**:
  * **Endless Battery**: Infinite battery for the vitals tablet.
  * **No Vitals Cooldown**: Open vitals tablet at any time without delay.
* **Detective**:
  * **Interrogate Reach**: Interrogate suspects from infinite range.
  * **No Interrogate Cooldown**: Interrogate without cooldown timer.
* **Influencer (Spirit Guide)**:
  * **Reach**: Infinite message range for Spirit Guide.
  * **No Message Cooldown**: Send messages without cooldown.
  * **No Selection Cooldown**: Select targets instantly.
  * **No Photo Limit**: Removes photo limits.
  * **No Refresh Cooldown**: Instant ability refresh.
* **Guardian Angel**:
  * **Infinite Protection Range**: Protect players from anywhere on the map.
  * **Ignore Impostors**: Prevents accidentally shielding Impostors.
  * **No Protect Cooldown**: Protect players continuously without cooldown.
* **Judge**:
  * **Enable Judge Overrule (No Tasks)**: Overrules meetings without needing completed tasks.
  * **Judge Immune**: Immunity against Judge executions.
* **Change Role**:
  * Role selection slider.
  * Apply Role button (server-synced if host; local if client).

### 4. Ship
* **General**:
  * **Call Meeting**: Calls an emergency meeting from anywhere on the map.
  * **Close Meeting**: Closes active emergency meetings immediately.
  * **Auto-Report Dead Bodies**: Automatically reports corpses upon walking near them.
  * **Auto-Open Doors On Use**: Automatically opens closed doors upon approaching.
* **Sabotage**:
  * Direct Sabotage triggers: Reactor Meltdown, Oxygen Depletion, Electrical Lights, Communications, Mushroom Mixup, Trigger Spores.
  * **Unfixable Lights**: Prevents players from turning electrical lights back on.
  * **Disable Lights (Auto Moving Switches)**: Continuously flips electrical switches randomly.
  * **Unfixable Comms**: Prevents players from repairing radio communications.
  * **Show Doors Menu**: Opens the Doors Control subwindow.
  * **Open Sabotage Map**: Opens the clickable sabotage map overlay.
  * **Disable Sabotage**: Blocks all sabotages from being activated.
  * **Auto-Fix Reactor/O2 (Last 3s)**: Automatically repairs lethal sabotages when 3 seconds remain.
  * **Sabotage All**: Triggers all non-door sabotages simultaneously.
  * **Random Sabotage**: Triggers a random sabotage system.
  * **Fix Sabotages**: Instantly repairs all active sabotages.
  * **Sabotage All: Pin Doors**: Pins doors shut during Sabotage All.
* **Vents**:
  * **Unlock Vents**: Enables venting for all roles (including Crewmates).
  * **Vent Network**: Unifies all vents across the map into one interconnected network (cycle with Left/Right Arrow keys or click any vent to jump to it).
  * **Hide Guide Lines**: Hides connecting lines between vents.
  * **Vent Interaction Range**: Interact with vents from a distance (slider from 1.0x to 8.0x).
  * **Kick All From Vents**: Forces all vented players out of vents.
  * **Disable Vents**: Disables venting for everyone.
  * **Exclude Yourself**: Keeps your own venting enabled while disabling vents for others.
* **Animations (Visual Tasks & FX)**:
  * Shields, Asteroids, Empty Garbage, Medbay Scan.
  * **Enable Med Scan**: Allows using the Medbay scanner console at any time, even without the task or as Impostor.
  * Cams In Use (blinks security camera lights).
  * Pet Animation (plays petting animation).
  * Moonwalk (walk without walking animation, client-sided).

### 5. Schizo (Client Desync)
* **Targeted Client Desync System**: Triggers client-side hallucinations that only the targeted player(s) can see or hear.
* Target individual players or hold `Ctrl` to multi-select targets.
* **Fake Sabotages**:
  * **Reactor / Meltdown**: Sounds alarm and displays meltdown countdown timer on target's screen only.
  * **Oxygen**: Shows oxygen depletion countdown on target's screen only.
  * **Comms**: Disrupts communications interface on target's screen only.
  * **Mushroom Mixup**: Scrambles identities on target's screen only (The Fungle).
* **Fake Doors**:
  * **Close All Doors**: Closes all doors on target's screen only.
  * **Close Specific Room Doors**: Locks specific room doors on target's screen only.

### 6. Lobbies (History & Rejoin)
* Tracks complete history of joined lobbies: Room Code, Host Name, Map, Player Count, Region, Privacy (Public/Private), and Time elapsed.
* **Copy**: Copies room code to clipboard.
* **Join**: Reconnects directly to the selected lobby.
* **Clear**: Wipes lobby history cache.

### 7. Chat & Chat Sender
* **Chat Sender / Spammer**:
  * Message input field (IL2CPP-safe typing).
  * Interval slider (logarithmic scale from 0.1ms to 60s).
  * **Send Once** button.
  * **Start Spam / Stop Spam** button.
* **General**:
  * **Enable Chat**: Unlocks in-game chat button at all times.
  * **Bypass URL Block**: Allows sending web links in chat.
  * **Lower Rate Limits**: Bypasses spam filters and rate limiting.
  * **Dark Mode Chat**: Styles in-game chat interface in a dark aesthetic.
  * **Show Chat Log**: Opens the Chat Log subwindow.
* **Textbox**:
  * **Unlock Extra Characters**: Enables full Unicode and special symbol input.
  * **Unlock Clipboard**: Allows `Ctrl + V` pasting into text fields.
  * **Add Timestamps to Messages**: Timestamps each chat message.
* **Advanced Chat Tags**:
  * **Show Tasks (X/Y)**: Displays completed tasks count tag in chat.
  * **Show Votekick Counter**: Displays current votekicks count tag.
  * **Show Friend Code**: Displays sender's friend code tag.

### 8. Players
* Clickable player list with `Ctrl`+click multi-selection support.
* **Single Player Details**:
  * Player Name, Color, ID, Client ID, Level, Platform, Client Type (Unmodded, Malum, Hydra, etc.).
  * Role, Location (Room and true X, Y coordinates).
  * Friend Code and PUID.
  * **Copy Name** and **Copy PUID** buttons.
  * **Teleport to Player** button.
  * **Copy Avatar** and **Restore Avatar** buttons.
  * **Murder** button.
* **Multi-Target Controls**:
  * Selected targets summary chip list & Deselect All button.
  * **Teleport to First Selected**.
  * **Copy Avatar from Selected**.
  * **Restore Original Avatar**.
  * **Murder Selected** (murders all valid chosen players).

### 9. Outfits & Wardrobe
* **Save Current Outfit**:
  * Preset name template selector (< / > buttons + quick preset names).
  * Paste Clipboard & Use Color buttons.
  * **Save as JSON Preset** button (saved to `BepInEx/config/Outfits/`).
* **Clone / Steal Outfit**:
  * 2-column lobby player selector grid.
  * **Clone & Equip**: Immediately copies and equips target's cosmetics.
  * **Save to JSON**: Saves target's cosmetics to an outfit preset file.
* **Color Sniper**:
  * **Enable Color Sniper Toggle**.
  * **Only Snipe in Lobby Toggle**.
  * Target Color selector (< / > buttons and hex preview).
  * **Claim Color Button**: Automatically claims target color the instant it becomes available.
* **Quick Actions**:
  * Open Outfits Folder.
  * Restore Default Avatar.
  * **Show Wardrobe Overlay on Inventory** toggle.
* **Saved Presets Library**:
  * List of saved outfits with color tags, cosmetic names, and IDs.
  * **Equip**: Instantly equips the preset.
  * **Overwrite**: Updates preset with your current outfit.
  * **Rename**: Renames preset with templates or clipboard.
  * **Delete (X)**: Deletes preset with confirmation.

### 10. Console & RPC Debugger
* **Show Console**: Opens the Console subwindow.
* **Player Loggers**: Log Deaths, Log Shapeshifts, Log Vents, Log Tasks, Log Joins, Log Disconnects, Log Guardian Protect.
* **Meeting Loggers**: Log Meetings, Log Votes, Log Votekicks, Log Judge Verdict.
* **Game Loggers**: Log Game State, Log Sabotages, Log Chat.
* **RPC Console**:
  * Show RPC Console subwindow.
  * Log Incoming RPCs.
  * Log Outgoing RPCs.

### 11. Host Only
* **General**:
  * **Kill While Vanished**: Murder invisible Phantoms.
  * **Kill Anyone**: Murder teammates, ghosts, and vented players.
  * **No Kill Cooldown**: Zero-second kill cooldown.
  * **Ban Mid-Game**: Ban players while rounds are active.
  * **Disable Close Doors**: Prevent doors from being shut.
  * **Disable Security Cameras**: Disable camera feeds.
  * **Show Protect Menu**: Opens Protect Players subwindow.
  * **Show Roles Menu**: Opens Assign Roles subwindow.
  * **Assign Roles Next Round**: Enforces forced roles upon match start.
  * **No Options Limits**: Unlocks lobby rule slider boundaries.
  * **Disco Party**: Continuously cycles colors for all players.
* **Murder Cheats**:
  * **Kill Player**: Opens PPM to select and kill target.
  * **Telekill Player**: Teleports to target, kills them, and snaps back to original spot.
  * **Kill All Crewmates**: Murders only crewmate players.
  * **Kill All Impostors**: Murders only impostor players.
  * **Kill Everyone**: Murders all players in match.
* **Game State**:
  * **Force Start Game**: Starts game countdown immediately.
  * **No Game End**: Blocks victory/defeat game-over screens.
* **Meetings**:
  * **Disable Meetings**: Blocks emergency meetings.
  * **Spam Report Bodies**: Floods meetings with body reports.
  * **Skip Meeting**: Forces meeting skip.
  * **Vote Immune**: Protects host from receiving votes.
  * **Eject Player**: Opens PPM to eject selected player.
* **Role Assigner**:
  * **Enable Role Assigner Toggle**.
  * Total Players, Assigned count, and Impostor cap tracking summary.
  * **Clear All (Random)** button.
  * Interactive per-player role picker (< and > buttons to assign Impostor, Shapeshifter, Phantom, Viper, Crewmate, Engineer, Scientist, Tracker, Noisemaker, Detective, Judge, Guardian Angel, Influencer, or Random).

### 12. Votekick System
* **Votekick & Rejoin Automation System**:
  * **VOTEKICK & REJOIN Loop**: Automated cycle that casts votekicks, leaves, and rejoins the lobby to bypass votekick limits until the target is kicked.
  * **Votekick All (1x)**: Casts a votekick vote against every player in the room.
  * **Rejoin Lobby**: Leaves and rejoins the current room.
  * **Reset Counts**: Clears tracked votekick counts.
  * Live status banner.
* **Player Votekick Cards**:
  * Player Name, Host tag, Level, Platform, Friend Code.
  * Live Votekick badge (`[VK: 0/3]`, `[VK: 1/3]`, `[VK: 2/3 - READY TO KICK]`).
  * Dedicated per-player Votekick button ("Votekick", "KICK NOW!", "VOTED").
* **Options & Protections**:
  * **Votekick Protection**: Protects client from being votekicked.
  * **Floating Notifications on Votekick**: Displays HUD toast alerts when votekicks occur.
  * **Auto-Rejoin on Disconnect**: Automatically reconnects if kicked or disconnected.

### 13. Config & Profiles
* **Profile Management**:
  * Profile carousel selector (< / > buttons with active profile indicator).
  * Load, Update (save current settings), and Delete profile buttons.
  * Profile Name field (IL2CPP-safe typing) + New and Rename buttons.
  * Open Profiles Folder button.
  * **Auto Load on Startup** toggle.
  * Open Config File & Reload Config toggles.
* **Menu Settings**:
  * **Pop-up Window Scales**: Opens submenu to scale all popups individually.
  * **Keybind Settings**: Opens hotkey assignment submenu.
  * **Menu Keybind Button**: Rebinds the menu toggle key (default `Delete`).
  * **Menu Scale Slider**: Configurable from 0.50x to 2.00x.
  * **Menu Opacity Slider**: Configurable from 10% to 100%.
  * **Open on Cursor Toggle**: Centers menu at mouse position upon opening.
  * **Keep Subwindows Open Toggle**: Keeps subwindows visible when main menu is hidden.
  * **Version Warning Popup Toggle**.
  * **Wardrobe Overlay on Inventory Toggle**.
* **Account Cheats**:
  * **Free Cosmetics**: Unlocks all Hats, Skins, Visors, Pets, Nameplates, and Cosmicubes.
  * **Avoid Penalties**: Bypasses quit timeout bans.
  * **Unlock Extra Features**: Unlocks free chat, custom names, and all game modes.
  * **Copy Lobby Code on Disconnect**: Automatically copies room code to clipboard upon disconnect.
  * **Spoof Date to April 1st**: Activates Dekt / April Fools inverted map and Easter eggs.
  * **Unlock FPS**: Unlocks game frame rate with a custom Target FPS type-in field and an Unlimited button.
  * **Time Format Switcher**: Toggle between 24hr and 12hr formats.
* **Modes**:
  * **Eject**: Cleanly unloads MalumMenu from game memory.
  * **Show "Stuff" Tab**: Enables the hidden joke/meme tab.

### 14. Themes
* **RGB Mode**: Animated rainbow cycling accent.
* **Custom Color**:
  * Custom Hex input + color preview swatch + Apply button.
  * Custom Gradient input (Start Hex & End Hex) + live animated preview + Apply Gradient button.
  * Saved Themes (shared across Malum & Hydra via `CThemes` folder):
    * Name input field + Save button.
    * Two-column saved themes list with live color preview, one-click Apply, and Delete (✕).
* **Solid Themes Presets**: 8 clean presets (Malum, Violet, Ocean, Emerald, Crimson, Sunset, Gold, Bubblegum).
* **Gradients Presets**: 24 animated wave presets (Fire, Aurora, Galaxy, Ocean, Sunset, Mint, Cyberpunk, Vaporwave, Solar Flare, Matrix, Midnight, Amethyst, Blood Orange, Neon Lime, Lavender, Iceberg, Sakura, Synthwave, Cosmic, Emerald Forest, Electric Rose, Gold Mirage, Abyss, Tropical).

### 15. Info
* **Version Display**: Hydralum version, Malum Menu version, and Hydra Menu version.
* **Live Users Online Counter**: Displays the number of active Hydralum users online in real time.
* **Show Keybind Info Toggle**:
  * `[Delete]`: Toggle active menu.
  * `[Switch]`: Switch between MalumMenu and HydraMenu in-place.
  * `[Escape]`: Dismiss Match Info Guide, dialogs, and fix menu softlocks.
  * `[Left / Right Arrow]`: Cycle previous / next vent.
* **Invisible Name Method**: Step-by-step instructions with folder opener and copy buttons for `\u00AD` and `\u2060`.
* **Credits & Community Links**: Quick-launch buttons to open official GitHub and Discord links for Hydralum, MalumMenu, and Hydra.

### 16. Stuff (Meme / Joke Tab)
* *Note: Requires enabling "Show 'Stuff' Tab" in Config → Modes.*
* **Overdose on Fentanyl**: Freezes and hangs the main game thread to simulate "Not Responding".
* **Enable Jiggle Physics**
* **Water Pouring Physics**
* **Install GTA 6**
* **Nuke Cyberleek**
* **Long wee wee physics**
* **Sex**
* **Launch the El culo del diablo firework**
* **Coke Can Physics**
* **sussy**
* **dueling for ones honour**
* **Musket Line Battles and die**
* **Sex with Cyberleek**
* **Ricky**
* **Creampie Imposters**
* **Let TyGamer4 Rest**
* **Say Gex**
* **Make TyGamer4 Work Harder**
* **Goon to Fortegreen Feet**
* **When The Imposter Is Gay Button**: Launches the YouTube video link.

---

# MalumMenuPlus Subwindows & Pop-ups

### Interactive Radar Minimap
* Procedural real-time on-screen radar HUD rendered from live map colliders.
* Shows player dots with movement trails, color-coded by role and team.
* **Bodies on Radar**: Displays dead bodies marked with death icons.
* **Ghosts on Radar**: Toggles displaying dead players on radar.
* **Doors on Radar**: Real-time door status indicators (Green = Open, Yellow = Closed, Red = Pinned Shut).
* **Interactive Controls**:
  * **Right-Click Radar**: Instantly teleports your player to the clicked room location.
  * **Left-Click Door Marker**: Shuts the clicked door once.
  * **Double-Click Door Marker**: Pins the door shut continuously.
  * **Click Pinned Door**: Unpins the door.
* **Quick-Action Footer Buttons**:
  * `Sabo`: Triggers Sabotage All without pinning doors.
  * `Fix`: Instantly fixes all active sabotages.
  * `Open`: Unpins and opens all doors.
  * `Close`: Closes all doors (1 click = close once; double click = pin all shut; click while pinned = unpin all).
* **Lock Window Button**: Locks radar in place to prevent accidental dragging.

### Match Replay Console
* Interactive 2D map replay timeline HUD (ported from SickoMenu).
* **Play / Pause & Timeline Scrubber**: Scrub backwards or forwards through the match timeline.
* **LIVE Mode**: Tracks live gameplay events in real time.
* **Clear**: Wipes replay recording history.
* **Player Focus Dropdown**: Filter view to All Players or inspect an individual player's movements.
* **Event Filter Chips**: Toggle display of Kill, Vent, Rep, Shift, GA, Sabo, Meet, and Paths.
* **Movement Trail Rasterization**: Bilinear CPU-rasterized movement trails color-coded per player.
* **Map Markers**:
  * `K`: Kill location with connecting line to victim body marker (`x`).
  * `V`: Vent enter/exit location.
  * `R`: Dead body report location.
  * `S`: Shapeshift transformation point.
  * `P`: Guardian Angel protection event.
* **Clear After Meeting Toggle**: Option to reset replay data after each round or record the entire match end-to-end.

### Player Pick Menu (PPM)
* Specialized visual player selector built on top of the in-game Shapeshifter menu interface.
* Used to execute targeted actions with visual player cards:
  * **Report Body**: Select any player (alive or dead) to report their body.
  * **Eject Player**: Select any player to eject immediately during meetings.
  * **Kill Player**: Select any player to execute.
  * **Telekill Player**: Select any player to teleport to, murder, and return to origin.
  * **Teleport to Player**: Select any player to teleport to their position.
  * **Set Fake Role**: Select any role to locally spoof (Shapeshifter, Phantom, Viper, Impostor, Judge, Tracker, Noisemaker, Engineer, Scientist, Detective, Crewmate, Guardian Angel, Influencer).
  * **Set Fake Alive**: Select Alive or Dead to locally revive or exile.
  * **Force Role**: Select any role to force onto another player.
  * **Spectate**: Select any player to follow with spectator camera.

### Doors Control Subwindow
* Lists every room with doors on the current map with live status (Open / Closed).
* Individual room **Close** and **Open** buttons.
* **Spam Close Toggle**: Continuously re-shuts doors in that room the instant they attempt to open.
* **Spam Open Toggle**: (Polus, Airship, Fungle) Keeps doors in that room held open.
* **Close All & Open All Buttons**.
* **Spam Close All Toggle**: Keeps every door on the ship shut continuously.
* **Spam Open All Toggle**: (Host-only) Keeps every door on the ship held open.

### Task Manager Subwindow
* Displays real-time task progress `[X/Y]` for every player in the lobby.
* Expandable accordion per player revealing exact task names and individual completion states.
* Individual **Complete** button next to each of your own uncompleted tasks to finish them one by one.
* **Complete My Tasks** button to finish all remaining tasks simultaneously.

### Protect Players Subwindow
* Lists all lobby players with live Guardian Angel protection status.
* Displays whether player is **Unprotected** or **Protected by Guardian ID**.
* **Protect Button**: Instantly applies a Guardian Angel shield to target player (Host-authoritative).
* **Keep Protected Toggle**: Continuously reapplies shields to target player whenever broken.
* **Protect Everyone Button**: Instantly shields all players in the match.
* **Keep Everyone Protected Toggle**: Continuously reapplies shields to all players.

### Assign Roles Subwindow
* Compact interface to inspect and assign forced roles for the local player on next game start.
* **Assign** and **Reset** buttons.

### Chat Log Subwindow
* Subwindow to monitor, record, and export in-game chat messages.
* **Record Chat Toggle** with live captured message counter.
* **Field Inclusions**: Toggle inclusion of Timestamp, Name Color, Dead Tag, Role, Level, Platform, Friend Code, Tasks (X/Y), and Votekick Count.
* Live message preview scroll view.
* **Export to .txt**: Saves complete formatted chat history to `BepInEx/config/TextLogs/`.
* **Open Text Logs Folder**: Opens the text logs folder in Windows Explorer.
* **Clear Log**: Wipes captured chat history.

### Wardrobe Overlay Subwindow
* Compact presets HUD overlay that displays automatically alongside the in-game Wardrobe laptop.
* Quick preset name template selector (< / > buttons + quick names).
* Paste Clipboard & Use Color buttons.
* Save Outfit to JSON button.
* Saved Presets Library with color tags and cosmetic IDs (Hat, Skin, Visor, Pet).
* One-click **Equip**, **Overwrite**, **Rename**, and **Delete (X)** buttons.
* Open Folder and Restore Default buttons.

### Pop-up Window Scales Submenu
* Independent scale multiplier sliders (0.50x to 2.50x) for each floating window:
  * Console, RPC Console, Doors, Tasks, Protect Players, Assign Roles, Chat Log, Wardrobe Overlay, Pop-up Window Scales, Keybind Settings.
* **Radar Size Slider** (60% to 180%) & **Radar Opacity Slider** (30% to 100%).
* **Replay Console Size Slider** (60% to 180%) & **Replay Console Opacity Slider** (30% to 100%).
* **Reset All to Default** button.

### Keybind Settings Submenu
* Custom hotkey binder supporting single keys and modifier combinations (`Ctrl+`, `Alt+`, `Shift+`).
* Assignable feature hotkeys:
  * Replay Console
  * Console
  * Doors Menu
  * Task Menu
  * Sabotage All
  * Fix Sabotages
  * Auto Complete Tasks
  * Complete All Tasks
  * Disable Auto Task on Taskbar Updates

### Event Logging Console Subwindow
* Dedicated floating subwindow displaying chronological in-game events in real time.
* Buffers up to 300 timestamped entries (`[h:mm:ss tt]`) with automatic bottom scrolling.
* Captures and logs events enabled under `Console Tab` (Deaths, Shapeshifts, Vents, Tasks, Joins, Disconnects, Guardian Protect, Meetings, Votes, Votekicks, Judge Verdict, Game State, Sabotages, and Chat).
* **Clear Log Button**: Flushes active log buffer.
* **Copy Log to Clipboard Button**: Copies full formatted event log history.

### RPC Debugger Console Subwindow
* Dedicated packet inspector subwindow for live network RPC monitoring.
* Buffers up to 500 chronological inbound and outbound RPC packets with rich-text formatting:
  * Color-coded direction indicators (`[IN]` in green, `[OUT]` in red).
  * Sender / target player information, RPC call enum, and parameter payload.
* Toggled via `Console -> RPC Console -> Show RPC Console`.
* Separate filters for `Log Incoming RPCs` and `Log Outgoing RPCs`.
* **Clear Log Button**: Clears stored RPC packet entries.
* **Copy Log to Clipboard Button**: Copies entire RPC network log to clipboard.
