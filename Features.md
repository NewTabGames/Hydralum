# Hydralum Feature Documentation

Comprehensive guide and catalog of all features, toggles, cheats, and protections across HydraMenu and MalumMenuPlus.

---

## Table of Contents
1. [HydraMenu Features](#hydramenu-features)
   - [Self](#1-self)
   - [Visuals](#2-visuals)
   - [Movement](#3-movement)
   - [Players](#4-players)
   - [Host Only](#5-host-only)
   - [Troll and Routines](#6-troll-and-routines)
   - [Sabotage](#7-sabotage)
   - [Protections](#8-protections)
   - [Hydra Anticheat](#9-hydra-anticheat)
   - [Version Spoofer](#10-version-spoofer)
   - [General and Chat](#11-general-and-chat)
   - [Menu and Themes](#12-menu-and-themes)
2. [MalumMenuPlus Features](#malummenuplus-features)
   - [ESP and Visuals](#1-esp-and-visuals)
   - [Movement and Physics](#2-movement-and-physics)
   - [Roles and Reach](#3-roles-and-reach)
   - [Ship and Vents](#4-ship-and-vents)
   - [Host Cheats](#5-host-cheats)
   - [Players and PPM](#6-players-and-ppm-player-pick-menu)
   - [Outfits and Cosmetics](#7-outfits-and-cosmetics)
   - [Chat and Console](#8-chat-and-console)
   - [Engine and Misc](#9-engine-and-misc)
   - [Themes and Customization](#10-themes-and-customization)

---

# HydraMenu Features

### 1. Self
* **Update Stats in Freeplay**: Tracks and increments your official game achievements and stats while in Freeplay mode.
* **Become Immortal**: Prevents impostors from killing you.
* **Always Show Task Animations**: Forces task visual animations to render on your screen regardless of game lobby settings.
* **No Ladder Cooldown**: Removes the climb cooldown when using ladders on maps like the Airship and Fungle.
* **Unlimited Meetings**: Allows calling emergency meetings without consuming your remaining meeting count.
* **Walk In Vents**: Allows your character to move freely around the map while keeping your in-vent state intact.
* **Call Meeting**: Instantly calls an emergency meeting from anywhere on the map.
* **Randomize Avatar**: Randomizes your outfit, skin, hat, visor, and pet.
* **Randomize Color**: Changes your player color to a random unused color.
* **Restore Avatar**: Restores your default saved cosmetics and color.

### 2. Visuals
* **Camera Zoom Slider**: Smoothly zoom out the main game camera (0.5x to 3.0x) for wide-angle map awareness.
* **Map Zoom Slider**: Adjusts mini-map resolution and zoom level to match the camera distance.
* **Fullbright**: Maximizes player vision lighting so the entire map is fully lit.
* **Always Visible Chat**: Keeps the chat button accessible during normal gameplay.
* **Show Ghosts**: Renders dead ghost players while you are still alive.
* **Show Messages by Ghosts**: Displays messages sent by dead ghosts in the in-game chat while alive.
* **Skip Shhh Animation**: Skips the introductory "Shhh" role reveal animation.
* **No Seeker Animation**: Skips the seeker intro animation in Hide and Seek game mode.
* **Accurate Disconnect Reasons**: Displays the exact internal error code or reason when disconnected from a lobby.
* **Show Protections**: Displays active Guardian Angel shields and protections.
* **Hide My Gem**: Hides the Hydralum user diamond above your own character.
* **Hide All Gems**: Hides all Hydralum user diamonds across the entire lobby.

### 3. Movement
* **Player Speed Multiplier**: Adjusts your movement speed multiplier with a slider (1.0x to 10.0x).
* **NoClip**: Disables map collisions so you can walk through walls, objects, and barriers.
* **Use SnapTo RPC**: Uses network SnapTo RPCs for instant authoritative positioning.
* **Teleport to Cursor**: Teleports your character directly to your mouse cursor position.

### 4. Players
* **Player Selection List**: Select any player in the lobby to inspect or apply targeted cheats.
* **Crewmate Color Boxes**: Displays color-coded badges next to each player name.
* **Player Info Display**: Shows target player ID, role, level, platform, and friend code.
* **Target Actions**:
  * **Teleport to Player**: Instantly snaps to the target player position.
  * **Kill Player**: Force-kills the selected player.
  * **Freeze Player**: Locks target player movement in place.
  * **Pet Player**: Rapidly pets the selected player pet.
  * **Follow Player**: Continuously locks your position to follow behind the target player.
  * **Change Target Role**: Sliders to spoof or assign roles to the target player.
  * **Change Target Color**: Assigns any color (IDs 0 to 17) to the target.
  * **Kick / Ban**: Instantly kicks or bans the target from the room.

### 5. Host Only
* **Always Impostor**: Guarantees you receive an impostor role when hosting.
  * **Impostor Role Selector**: Choose between standard Impostor, Shapeshifter, Phantom, or Viper.
* **Ban Mid-Game**: Unlocks the ability to ban disruptive players while a round is actively running.
* **Flipped Skeld**: Activates the mirrored / flipped map layout on The Skeld.
* **Disable Sabotages**: Prevents impostors from triggering any sabotages.
* **Disable Close Doors**: Prevents doors from being locked by impostors.
* **Disable Cameras**: Disables security camera monitoring across the ship.
* **Disable Game End**: Prevents the match from ending when win conditions are met.
* **No Kill Cooldown**: Removes the kill timer cooldown completely for all impostors.
* **Block Low Levels**: Automatically kicks players whose account level is below a set threshold.
  * **Minimum Level Slider**: Configurable minimum level required to join the room.
* **Disable Meetings**: Blocks all emergency meetings from being called.
* **Report Body Spam**: Rapidly spams dead body reports across the lobby.
* **Disco Party (Disco Host)**: Cycles through random character colors in rapid succession.
  * **Color Delay Slider**: Adjusts speed of color cycling (0.1s to 2.0s).

### 6. Troll and Routines
* **Block Sabotages**: Intercepts and blocks sabotage system updates from other players.
* **Block Venting**: Prevents players from entering or using vents.
* **Deplete Seek Timer**: Instantly drains the remaining escape timer in Hide and Seek mode.
* **Auto Trigger Spores**: Automatically pops mushroom spores on The Fungle to generate spore clouds.
* **Teleport Spammer to Vents**: Repeatedly teleports selected players to vent locations.
* **Door Troller**: Automatically cycles door locking and unlocking across all rooms.
  * **Door Delay Slider**: Configurable interval for door toggling (0.1s to 2.0s).

### 7. Sabotage
* **Direct System Sabotage Buttons**:
  * Reactor Meltdown / Seismic Destabilizer
  * Oxygen Depletion (O2)
  * Electrical Lights
  * Communications (Comms)
  * Mushroom Mixup (The Fungle)
* **Door Controls**:
  * **Lock All Doors**: Locks every door on the current map simultaneously.
  * **Unlock All Doors**: Unlocks all doors across the entire map.
  * **Individual Room Doors**: Skeld, Mira HQ, Polus, Airship, and Fungle room toggles.
* **Update Systems Directly**: Bypasses standard network RPC restrictions to directly alter system states.

### 8. Protections
* **Force DTLS Network Encryption**: Forces secure DTLS encryption for all game network traffic.
* **Block Server Teleports**: Prevents the server or modded hosts from force-teleporting your position.
* **Block Unauthorized System Updates**: Blocks invalid or malicious system update packets from unauthorized clients.
* **Block Large Game Messages**: Drops oversized packet payloads designed to crash or desync clients.
* **Block Invalid GameData Messages**: Validates and drops malformed GameData packets.
* **Hardened ReadPackedUInt**: Hardened deserializer protecting against integer overflow and buffer exploits.
* **Memory Allocation Overload Protection**: Protects against VotingComplete packet overloads and client freeze exploits.
* **Bypass Shapeshift Ratelimits**: Allows shapeshifting without client-side cooldown locks.
* **Prevent Votekicks**: Protects the host from being votekicked out of their own lobby.
* **Protect Against Non-Host Kick Exploit**: Blocks unauthorized clients from sending illegitimate kick packets.

### 9. Hydra Anticheat
* **Master Toggle**: Enable/disable Hydra real-time packet inspection engine.
* **Check Spoofed Platforms**: Flags players sending falsified platform identity data.
* **Send Notifications**: Displays in-game HUD alerts when an exploit or illegal RPC is detected.
* **Discard Malicious RPCs**: Automatically drops illegal RPCs before they can affect game state.
* **Punishment Modes**: Configurable response upon detecting a cheater:
  * 0 = None (Log only)
  * 1 = Kick
  * 2 = ErrorKick
  * 3 = Ban
* **Granular RPC Validators**:
  * SetName (Invalid name length or illegal character injection)
  * SetColor (Out-of-bounds color IDs or invalid palette indices)
  * EnterVent and ExitVent (Venting without an authorized role)
  * SnapTo (Illegal position teleports)
  * CompleteTask (Completing tasks not assigned to the player)
  * ClimbLadder (Ladder teleportation exploits)
  * PlayAnimation (Unauthorized visual task animations)
  * SetLevel (Spoofed account level packets)
  * AddVote (Illegal voting during meetings)
  * Exiled (Manipulated ejection calls)
  * CloseDoorsOfType (Unauthorized door locking)
  * SetScanner (Fake medbay scanner animations)
  * ReportDeadBody (Reporting unspawned bodies or reporting across walls)
  * UpdateSystem (Sabotaging while in vents or invalid system operations)
  * UsePlatform (Airship gap platform exploits)
  * SetStartCounter (Premature game start exploits)

### 10. Version Spoofer
* **Enable Version Spoofing**: Spoofs the game version sent during connection handshakes.
* **Version Presets**: Select from presets (18.0, 17.0, 16.0, etc.) to match older or newer server protocol versions.
* **Use Modded Protocol**: Enables compatibility handshake modes for custom community servers.

### 11. General and Chat
* **Log Chat Messages**: Prints all in-game chat messages directly to the console with player IDs and timestamps.

### 12. Menu and Themes
* **UI Scale Slider**: Adjust menu sizing from 0.5x to 2.0x.
* **UI Opacity Slider**: Adjust menu transparency (0% to 100%).
* **Theme Modes**:
  * Solid: Classic clean flat color palette.
  * RGB Wave: Animated rainbow cycling accent.
  * Wave Gradient: 24 custom gradient color presets.
* **Open on Cursor**: Automatically centers the menu window wherever your mouse is located.
* **Disable Notifications**: Suppresses on-screen Hydra notification banners.

---

# MalumMenuPlus Features

### 1. ESP and Visuals
* **Player Nametags**: Renders player identity information above character heads:
  * Role and Role Team Color
  * Account Level
  * Platform Type (Steam, Epic, Android, iOS, PlayStation, Xbox, Switch)
  * Host Badge ([Host])
  * Hydralum User Gem
* **Body Tracers**: Draws direct lines to all dead bodies on the map.
* **Player Tracers**: Draws lines from your character to every player in the lobby.
* **See Ghosts**: Renders ghost players and allows seeing ghost movement while alive.
* **Radar / Mini-Map ESP**: Displays player positions on the mini-map.
* **Fullbright**: Maximizes local lighting.
* **Zoom Out HUD**: Scales the UI properly when zoomed out.

### 2. Movement and Physics
* **Speed Hack**: Increases player walking speed.
* **NoClip**: Walk through walls and map colliders.
* **Teleport to Cursor**: Snaps your player directly to mouse click position.
* **Invert Controls**: Reverses movement input directions.
* **Moon Walk**: Allows walking without triggering the standard walking animation.

### 3. Roles and Reach
* **Kill Anyone**: Allows impostors to kill any target (including ghosts, teammates, and players in vents).
* **Kill Vanished**: Allows killing Phantoms while they are invisible.
* **No Kill Checks**: Bypasses role target validation for kill attempts.
* **Kill Reach**: Infinite kill distance for Impostors.
* **Interrogate Reach**: Infinite interrogation reach for Detectives.
* **Track Reach**: Infinite tracking distance for Trackers.
* **Engineer Cheats**: Instant vent cooldown and unlimited vent duration.
* **Scientist Cheats**: Infinite battery for vitals.
* **Shapeshifter Cheats**: Instant shapeshift without cooldown or animation lock.

### 4. Ship and Vents
* **Unlock All Vents**: Enables venting for all roles (including Crewmates).
* **Vent Network**: Connects all vents across the map into one unified network (cycle through all vents with arrow keys).
* **Kick All From Vents**: Forces all players out of vents.
* **Disable Vents**: Disables venting for everyone in the match.
* **Exclude Yourself**: Keeps your own venting active while disabling vents for others.
* **Enable Med Scan**: Allows using the Medbay scanner console to perform a realistic visual scan animation at any time, even when not assigned the task or when playing as an Impostor.
* **Door Controls**: Instant lock and unlock per room across all maps.
* **Sabotage Map**: Clickable sabotage map overlay to trigger sabotages instantly.

### 5. Host Cheats
* **Force Start Game**: Starts the match countdown immediately.
* **Kill All**: Murders all players in the match instantly.
* **Kill All Crew**: Murders only crewmate players.
* **Kill All Impostors**: Murders only impostor players.
* **End Game**: Instantly ends the match with Impostor Win or Crewmate Win.
* **Close Meeting**: Closes active emergency meetings immediately.
* **Skip Meeting**: Forces voting to skip and concludes the meeting.
* **Call Emergency Meeting**: Triggers an emergency meeting at will.

### 6. Players and PPM (Player Pick Menu)
* **Spectate Player**: Free-cam spectates the selected player with camera following.
  * **Spectate HUD Button**: Custom HUD button next to the Use button to start and stop spectating.
* **Telekill Player**: Teleports to target and executes a kill.
* **Teleport to Player**: Snaps directly to target player.
* **Murder Player**: Force-kills the target player.
* **Eject Player**: Triggers the ejection sequence on the selected player.
* **Set Fake Role**: Locally spoofs the target player role.
* **Set Fake Alive**: Locally toggles target player alive and dead state.

### 7. Outfits and Cosmetics
* **Unlock All Items**: Unlocks all Hats, Skins, Visors, Pets, and Nameplates in your inventory.
* **Unlock All Cosmicubes**: Unlocks all Cosmicubes and their progression trees.
* **Color Sniper**: Automatically claims your target color as soon as it becomes available in a lobby.
* **Outfit Customizer**: Switch cosmetics and preview outfits in real-time.

### 8. Chat and Console
* **Log Deaths / Kill Feed**: Logs every murder, killer identity, victim, room location, and Guardian Angel saves in real time to the Console tab.
* **Auto Report Dead Bodies**: Automatically calls a dead body report when walking near a corpse.
* **Chat Spammer**: Automatically spams custom text into the in-game chat.
* **Always Visible Chat**: Keeps chat open during meetings and gameplay.

### 9. Engine and Misc
* **FPS Unlocker**: Unlocks the game native frame rate limit with a custom Target FPS slider (60 to 360+ FPS).
* **Unlock Characters**: Unlocks full Unicode character input and clipboard pasting (Ctrl + V) in all text boxes.
* **Unlock Features**: Unlocks custom names and free chat permissions.
* **Panic Mode**: Instantly unloads the menu and restores default game state cleanly.
* **Custom Keybinds**: Assign custom hotkeys for any cheat toggle.

### 10. Themes and Customization
* **UI Scale and Opacity**: Adjustable menu scaling and background opacity.
* **Custom Color Themes**: Solid colors, RGB wave animations, and custom hex color support.
