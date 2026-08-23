using BepInEx.Configuration;
using HydraMenu.anticheat;
using HydraMenu.features;
using HydraMenu.ui;
using UnityEngine;

namespace HydraMenu
{
    public static class HydraConfig
    {
        // GUI
        public static ConfigEntry<float> MenuScale;
        public static ConfigEntry<float> MenuOpacity;
        public static ConfigEntry<int> PrimaryColor;
        public static ConfigEntry<int> ThemeMode;
        public static ConfigEntry<int> GradientIndex;
        public static ConfigEntry<bool> OpenOnCursor;
        public static ConfigEntry<bool> DisableNotifications;
        public static ConfigEntry<float> WindowPosX;
        public static ConfigEntry<float> WindowPosY;

        // Visuals
        public static ConfigEntry<bool> Fullbright;
        public static ConfigEntry<bool> AlwaysVisibleChat;
        public static ConfigEntry<bool> ShowGhosts;
        public static ConfigEntry<bool> ShowMessagesByGhosts;
        public static ConfigEntry<bool> SkipShhhAnimation;
        public static ConfigEntry<bool> NoSeekerAnimation;
        public static ConfigEntry<bool> AccurateDisconnectReasons;
        public static ConfigEntry<bool> ShowProtections;
        public static ConfigEntry<bool> HideMyGem;
        public static ConfigEntry<bool> HideAllGems;

        // Protections
        public static ConfigEntry<bool> ForceDTLS;
        public static ConfigEntry<bool> BlockServerTeleports;
        public static ConfigEntry<bool> BlockUnauthorizedSystemUpdates;
        public static ConfigEntry<bool> BlockLargeGameMessages;
        public static ConfigEntry<bool> BlockInvalidGameDataMessages;
        public static ConfigEntry<bool> HardenedReadPackedUInt;
        public static ConfigEntry<bool> MemoryAllocationOverload;
        public static ConfigEntry<bool> BypassShapeshiftRatelimits;
        public static ConfigEntry<bool> PreventVotekicks;
        public static ConfigEntry<bool> ProtectAgainstNonHostKickExploit;

        // Self
        public static ConfigEntry<bool> ColorSniperEnabled;
        public static ConfigEntry<int> ColorSniperTargetColor;
        public static ConfigEntry<bool> UpdateStatsFreeplay;
        public static ConfigEntry<bool> ImmortalityEnabled;
        public static ConfigEntry<bool> AlwaysShowTaskAnimations;
        public static ConfigEntry<bool> NoLadderCooldown;
        public static ConfigEntry<bool> UnlimitedMeetings;

        // General / Chat
        public static ConfigEntry<bool> LogChatMessages;

        // Movement
        public static ConfigEntry<bool> UseSnapToRPC;

        // Host
        public static ConfigEntry<bool> BanMidGame;
        public static ConfigEntry<bool> FlippedSkeld;
        public static ConfigEntry<bool> DisableSabotages;
        public static ConfigEntry<bool> DisableCloseDoors;
        public static ConfigEntry<bool> DisableCameras;
        public static ConfigEntry<bool> DisableGameEnd;
        public static ConfigEntry<bool> NoKillCooldown;
        public static ConfigEntry<bool> BlockLowLevels;
        public static ConfigEntry<uint> BlockLowLevelsMinLevel;
        public static ConfigEntry<bool> AlwaysImposter;
        public static ConfigEntry<bool> DisableMeetings;

        // Troll
        public static ConfigEntry<bool> BlockSabotages;
        public static ConfigEntry<bool> BlockVenting;

        // Sabotage
        public static ConfigEntry<bool> UpdateSystemsDirectly;

        // Spoofer
        public static ConfigEntry<bool> EnableVersionSpoofing;
        public static ConfigEntry<bool> UseModdedProtocol;

        // Anticheat
        public static ConfigEntry<bool> AnticheatEnabled;
        public static ConfigEntry<bool> CheckSpoofedPlatforms;
        public static ConfigEntry<bool> SendAnticheatNotification;
        public static ConfigEntry<bool> DiscardAnticheatRpc;
        public static ConfigEntry<int> AnticheatPunishment;
        private static ConfigFile _config;

        public static void Init(ConfigFile config)
        {
            _config = config;
            // GUI
            MenuScale = config.Bind("GUI", "Scale", 1.0f, "Menu UI scale factor (0.5 to 2.0)");
            MenuOpacity = config.Bind("GUI", "Opacity", 1.0f, "Menu opacity (0.0 to 1.0)");
            PrimaryColor = config.Bind("GUI", "PrimaryColor", 0, "Primary UI color scheme index");
            ThemeMode = config.Bind("GUI", "ThemeMode", 0, "Theme mode: 0 = Solid, 1 = RGB Wave, 2 = Wave Gradient");
            GradientIndex = config.Bind("GUI", "GradientIndex", 0, "Selected wave gradient preset index (0 to 23)");
            OpenOnCursor = config.Bind("GUI", "OpenOnCursor", true, "Open menu centered on mouse cursor position");
            DisableNotifications = config.Bind("GUI", "DisableNotifications", false, "Disable in-game Hydra notifications");
            WindowPosX = config.Bind("GUI", "WindowPosX", 250f, "Saved window X position");
            WindowPosY = config.Bind("GUI", "WindowPosY", 100f, "Saved window Y position");

            // Visuals
            Fullbright = config.Bind("Visuals", "Fullbright", false, "Maximum player vision/lighting");
            AlwaysVisibleChat = config.Bind("Visuals", "AlwaysVisibleChat", false, "Keep chat bubble button visible during gameplay");
            ShowGhosts = config.Bind("Visuals", "ShowGhosts", false, "Render dead ghost players while alive");
            ShowMessagesByGhosts = config.Bind("Visuals", "ShowMessagesByGhosts", false, "Show messages sent by dead ghosts in chat while alive");
            SkipShhhAnimation = config.Bind("Visuals", "SkipShhhAnimation", false, "Skip intro Shhh animation");
            NoSeekerAnimation = config.Bind("Visuals", "NoSeekerAnimation", false, "Skip Hide & Seek seeker animation");
            AccurateDisconnectReasons = config.Bind("Visuals", "AccurateDisconnectReasons", true, "Show accurate disconnection reasons");
            ShowProtections = config.Bind("Visuals", "ShowProtections", false, "Show Guardian Angel Protections");
            HideMyGem = config.Bind("Visuals", "HideMyGem", false, "Hide your own gem on your screen");
            HideAllGems = config.Bind("Visuals", "HideAllGems", false, "Hide all gems on your screen");

            // Protections
            ForceDTLS = config.Bind("Protections", "ForceDTLS", true, "Force enable DTLS network encryption");
            BlockServerTeleports = config.Bind("Protections", "BlockServerTeleports", false, "Block position updates from server");
            BlockUnauthorizedSystemUpdates = config.Bind("Protections", "BlockUnauthorizedSystemUpdates", true, "Block unauthorized system updates");
            BlockLargeGameMessages = config.Bind("Protections", "BlockLargeGameMessages", true, "Block large game messages");
            BlockInvalidGameDataMessages = config.Bind("Protections", "BlockInvalidGameDataMessages", true, "Block invalid game data message types");
            HardenedReadPackedUInt = config.Bind("Protections", "HardenedReadPackedUInt", true, "Use hardened packed int deserializer");
            MemoryAllocationOverload = config.Bind("Protections", "MemoryAllocationOverload", true, "Protect against VotingComplete overloads");
            BypassShapeshiftRatelimits = config.Bind("Protections", "BypassShapeshiftRatelimits", false, "Bypass ratelimits for Shapeshift RPC");
            PreventVotekicks = config.Bind("Protections", "PreventVotekicks", false, "Prevent being votekicked as host");
            ProtectAgainstNonHostKickExploit = config.Bind("Protections", "ProtectAgainstNonHostKickExploit", true, "Protect against non-host kick exploit");

            // Self
            ColorSniperEnabled = config.Bind("Self", "ColorSniperEnabled", false, "Automatically grab your chosen color when available in lobby");
            ColorSniperTargetColor = config.Bind("Self", "ColorSniperTargetColor", 0, "Target color index for Color Sniper");
            UpdateStatsFreeplay = config.Bind("Self", "UpdateStatsFreeplay", false, "Update player stats in Freeplay");
            ImmortalityEnabled = config.Bind("Self", "Immortality", false, "Become immortal");
            AlwaysShowTaskAnimations = config.Bind("Self", "AlwaysShowTaskAnimations", false, "Always show task visual animations");
            NoLadderCooldown = config.Bind("Self", "NoLadderCooldown", false, "Remove ladder climb cooldown");
            UnlimitedMeetings = config.Bind("Self", "UnlimitedMeetings", false, "Unlimited emergency meetings");

            // General / Chat
            LogChatMessages = config.Bind("Chat", "LogChatMessages", false, "Log in-game chat messages to console");

            // Movement
            UseSnapToRPC = config.Bind("Movement", "UseSnapToRPC", true, "Use SnapTo RPC for teleports");

            // Host
            BanMidGame = config.Bind("Host", "BanMidGame", false, "Allow banning players during game");
            FlippedSkeld = config.Bind("Host", "FlippedSkeld", false, "Use Flipped Skeld map layout");
            DisableSabotages = config.Bind("Host", "DisableSabotages", false, "Disable sabotages");
            DisableCloseDoors = config.Bind("Host", "DisableCloseDoors", false, "Disable closing doors");
            DisableCameras = config.Bind("Host", "DisableCameras", false, "Disable security cameras");
            DisableGameEnd = config.Bind("Host", "DisableGameEnd", false, "Disable game from ending");
            NoKillCooldown = config.Bind("Host", "NoKillCooldown", false, "Remove impostor kill cooldown");
            BlockLowLevels = config.Bind("Host", "BlockLowLevels", false, "Kick players below minimum level");
            BlockLowLevelsMinLevel = config.Bind("Host", "BlockLowLevelsMinLevel", 0u, "Minimum level required to join");
            AlwaysImposter = config.Bind("Host", "AlwaysImposter", false, "Always become impostor as host");
            DisableMeetings = config.Bind("Host", "DisableMeetings", false, "Disable emergency meetings");

            // Troll
            BlockSabotages = config.Bind("Troll", "BlockSabotages", false, "Block all sabotages");
            BlockVenting = config.Bind("Troll", "BlockVenting", false, "Disable vents for other players");

            // Sabotage
            UpdateSystemsDirectly = config.Bind("Sabotage", "UpdateSystemsDirectly", false, "Update sabotage systems directly");

            // Spoofer
            EnableVersionSpoofing = config.Bind("Spoofer", "EnableVersionSpoofing", false, "Enable broadcast version spoofing");
            UseModdedProtocol = config.Bind("Spoofer", "UseModdedProtocol", false, "Use modded handshake protocol");

            // Anticheat
            AnticheatEnabled = config.Bind("Anticheat", "Enabled", true, "Enable Hydra anticheat");
            CheckSpoofedPlatforms = config.Bind("Anticheat", "CheckSpoofedPlatforms", true, "Flag spoofed platform data");
            SendAnticheatNotification = config.Bind("Anticheat", "SendNotification", true, "Send notification when cheater detected");
            DiscardAnticheatRpc = config.Bind("Anticheat", "DiscardRpc", true, "Discard malicious RPCs");
            AnticheatPunishment = config.Bind("Anticheat", "Punishment", (int)Anticheat.Punishments.None, "Punishment mode (0=None, 1=Kick, 2=ErrorKick, 3=Ban)");

            // Apply loaded config values
            MainUI.scale = Mathf.Clamp(MenuScale.Value, 0.5f, 2.0f);
            Styles.menuOpacity = Mathf.Clamp(MenuOpacity.Value, 0f, 1f);
            Styles.primaryColor = (Styles.UIColors)Mathf.Clamp(PrimaryColor.Value, 0, Styles.ColorValues.Count - 1);
            Styles.activeThemeMode = (Styles.ThemeMode)Mathf.Clamp(ThemeMode.Value, 0, 2);
            Styles.selectedGradientIndex = Mathf.Clamp(GradientIndex.Value, 0, Styles.Gradients.Length - 1);
            MainUI.windowPosition = new Vector2(WindowPosX.Value, WindowPosY.Value);

            // Apply Visuals
            Visuals.Fullbright.Enabled = Fullbright.Value;
            Chat.AlwaysVisibleChat.Enabled = AlwaysVisibleChat.Value;
            Visuals.ShowGhosts.Enabled = ShowGhosts.Value;
            Chat.OnChat.ShowMessagesByGhosts = ShowMessagesByGhosts.Value;
            Visuals.SkipShhhAnimation.Enabled = SkipShhhAnimation.Value;
            Visuals.NoSeekerAnimationPatch.Enabled = NoSeekerAnimation.Value;
            Visuals.AccurateDisconnectReasons.Enabled = AccurateDisconnectReasons.Value;
            Visuals.ShowProtections.Enabled = ShowProtections.Value;
            Visuals.HideMyGem.Enabled = HideMyGem.Value;
            Visuals.HideAllGems.Enabled = HideAllGems.Value;

            // Apply Protections
            Protections.ForceDTLS.Enabled = ForceDTLS.Value;
            Protections.BlockServerTeleports.Enabled = BlockServerTeleports.Value;
            Protections.BlockUnauthorizedSystemUpdates = BlockUnauthorizedSystemUpdates.Value;
            Protections.BlockLargeGameMessages = BlockLargeGameMessages.Value;
            Protections.BlockInvalidGameDataMessages = BlockInvalidGameDataMessages.Value;
            Protections.HardenedReadPackedUInt.Enabled = HardenedReadPackedUInt.Value;
            Protections.MemoryAllocationOverload.Enabled = MemoryAllocationOverload.Value;
            Protections.BypassShapeshiftRatelimits.Enabled = BypassShapeshiftRatelimits.Value;
            Protections.Votekicks.Enabled = PreventVotekicks.Value;
            Protections.ProtectAgainstNonHostKickExploit = ProtectAgainstNonHostKickExploit.Value;

            // Apply Self
            Self.ColorSniper.Enabled = ColorSniperEnabled.Value;
            Self.ColorSniper.TargetColor = (byte)Mathf.Clamp(ColorSniperTargetColor.Value, 0, (int)ui.Controls.PlayerColors.Fortegreen);
            Self.UpdateStatsFreeplay.Enabled = UpdateStatsFreeplay.Value;
            Immortality.Enabled = ImmortalityEnabled.Value;
            Self.AlwaysShowTaskAnimations = AlwaysShowTaskAnimations.Value;
            Self.NoLadderCooldown.Enabled = NoLadderCooldown.Value;
            Self.UnlimitedMeetings.enabled = UnlimitedMeetings.Value;

            // Apply General
            Chat.OnChat.LogChatMessages = LogChatMessages.Value;

            // Apply Movement
            Teleporter.UseSnapToRPC = UseSnapToRPC.Value;

            // Apply Host
            Host.BanMidGame.Enabled = BanMidGame.Value;
            Host.FlippedSkeld = FlippedSkeld.Value;
            Host.DisableSabotages.Enabled = DisableSabotages.Value;
            Host.DisableCloseDoors.Enabled = DisableCloseDoors.Value;
            Host.DisableCameras.Enabled = DisableCameras.Value;
            Host.DisableGameEnd.Enabled = DisableGameEnd.Value;
            Host.NoKillCooldown.Enabled = NoKillCooldown.Value;
            Host.BlockLowLevels.Enabled = BlockLowLevels.Value;
            Host.BlockLowLevels.MinLevel = BlockLowLevelsMinLevel.Value;
            Host.AlwaysImposter.Enabled = AlwaysImposter.Value;
            Host.DisableMeetings.Enabled = DisableMeetings.Value;

            // Apply Troll
            Troll.BlockSabotages.Enabled = BlockSabotages.Value;
            Troll.BlockVenting.Enabled = BlockVenting.Value;

            // Apply Sabotage
            Sabotage.UpdateSystemsDirectly = UpdateSystemsDirectly.Value;

            // Apply Spoofer
            Spoofer.shouldSpoofVersion = EnableVersionSpoofing.Value;
            Spoofer.useModdedProtocol = UseModdedProtocol.Value;

            // Apply Anticheat
            Anticheat.Enabled = AnticheatEnabled.Value;
            Anticheat.CheckSpoofedPlatforms = CheckSpoofedPlatforms.Value;
            Anticheat.sendNotification = SendAnticheatNotification.Value;
            Anticheat.discardRpc = DiscardAnticheatRpc.Value;
            Anticheat.punishment = (Anticheat.Punishments)Mathf.Clamp(AnticheatPunishment.Value, 0, 3);
        }

        public static void Save()
        {
            if (MenuScale != null) MenuScale.Value = MainUI.scale;
            if (MenuOpacity != null) MenuOpacity.Value = Styles.menuOpacity;
            if (PrimaryColor != null) PrimaryColor.Value = (int)Styles.primaryColor;
            if (ThemeMode != null) ThemeMode.Value = (int)Styles.activeThemeMode;
            if (GradientIndex != null) GradientIndex.Value = Styles.selectedGradientIndex;
            if (OpenOnCursor != null) OpenOnCursor.Value = OpenOnCursor.Value;
            if (DisableNotifications != null && Hydra.notifications != null)
                DisableNotifications.Value = Hydra.notifications.DisableNotifications;
            if (WindowPosX != null) WindowPosX.Value = MainUI.windowPosition.x;
            if (WindowPosY != null) WindowPosY.Value = MainUI.windowPosition.y;

            // Visuals
            if (Fullbright != null) Fullbright.Value = Visuals.Fullbright.Enabled;
            if (AlwaysVisibleChat != null) AlwaysVisibleChat.Value = Chat.AlwaysVisibleChat.Enabled;
            if (ShowGhosts != null) ShowGhosts.Value = Visuals.ShowGhosts.Enabled;
            if (ShowMessagesByGhosts != null) ShowMessagesByGhosts.Value = Chat.OnChat.ShowMessagesByGhosts;
            if (SkipShhhAnimation != null) SkipShhhAnimation.Value = Visuals.SkipShhhAnimation.Enabled;
            if (NoSeekerAnimation != null) NoSeekerAnimation.Value = Visuals.NoSeekerAnimationPatch.Enabled;
            if (AccurateDisconnectReasons != null) AccurateDisconnectReasons.Value = Visuals.AccurateDisconnectReasons.Enabled;
            if (ShowProtections != null) ShowProtections.Value = Visuals.ShowProtections.Enabled;
            if (HideMyGem != null) HideMyGem.Value = Visuals.HideMyGem.Enabled;
            if (HideAllGems != null) HideAllGems.Value = Visuals.HideAllGems.Enabled;

            // Protections
            if (ForceDTLS != null) ForceDTLS.Value = Protections.ForceDTLS.Enabled;
            if (BlockServerTeleports != null) BlockServerTeleports.Value = Protections.BlockServerTeleports.Enabled;
            if (BlockUnauthorizedSystemUpdates != null) BlockUnauthorizedSystemUpdates.Value = Protections.BlockUnauthorizedSystemUpdates;
            if (BlockLargeGameMessages != null) BlockLargeGameMessages.Value = Protections.BlockLargeGameMessages;
            if (BlockInvalidGameDataMessages != null) BlockInvalidGameDataMessages.Value = Protections.BlockInvalidGameDataMessages;
            if (HardenedReadPackedUInt != null) HardenedReadPackedUInt.Value = Protections.HardenedReadPackedUInt.Enabled;
            if (MemoryAllocationOverload != null) MemoryAllocationOverload.Value = Protections.MemoryAllocationOverload.Enabled;
            if (BypassShapeshiftRatelimits != null) BypassShapeshiftRatelimits.Value = Protections.BypassShapeshiftRatelimits.Enabled;
            if (PreventVotekicks != null) PreventVotekicks.Value = Protections.Votekicks.Enabled;
            if (ProtectAgainstNonHostKickExploit != null) ProtectAgainstNonHostKickExploit.Value = Protections.ProtectAgainstNonHostKickExploit;

            // Self
            if (ColorSniperEnabled != null) ColorSniperEnabled.Value = Self.ColorSniper.Enabled;
            if (ColorSniperTargetColor != null) ColorSniperTargetColor.Value = (int)Self.ColorSniper.TargetColor;
            if (UpdateStatsFreeplay != null) UpdateStatsFreeplay.Value = Self.UpdateStatsFreeplay.Enabled;
            if (ImmortalityEnabled != null) ImmortalityEnabled.Value = Immortality.Enabled;
            if (AlwaysShowTaskAnimations != null) AlwaysShowTaskAnimations.Value = Self.AlwaysShowTaskAnimations;
            if (NoLadderCooldown != null) NoLadderCooldown.Value = Self.NoLadderCooldown.Enabled;
            if (UnlimitedMeetings != null) UnlimitedMeetings.Value = Self.UnlimitedMeetings.enabled;

            // General / Chat
            if (LogChatMessages != null) LogChatMessages.Value = Chat.OnChat.LogChatMessages;

            // Movement
            if (UseSnapToRPC != null) UseSnapToRPC.Value = Teleporter.UseSnapToRPC;

            // Host
            if (BanMidGame != null) BanMidGame.Value = Host.BanMidGame.Enabled;
            if (FlippedSkeld != null) FlippedSkeld.Value = Host.FlippedSkeld;
            if (DisableSabotages != null) DisableSabotages.Value = Host.DisableSabotages.Enabled;
            if (DisableCloseDoors != null) DisableCloseDoors.Value = Host.DisableCloseDoors.Enabled;
            if (DisableCameras != null) DisableCameras.Value = Host.DisableCameras.Enabled;
            if (DisableGameEnd != null) DisableGameEnd.Value = Host.DisableGameEnd.Enabled;
            if (NoKillCooldown != null) NoKillCooldown.Value = Host.NoKillCooldown.Enabled;
            if (BlockLowLevels != null) BlockLowLevels.Value = Host.BlockLowLevels.Enabled;
            if (BlockLowLevelsMinLevel != null) BlockLowLevelsMinLevel.Value = Host.BlockLowLevels.MinLevel;
            if (AlwaysImposter != null) AlwaysImposter.Value = Host.AlwaysImposter.Enabled;
            if (DisableMeetings != null) DisableMeetings.Value = Host.DisableMeetings.Enabled;

            // Troll
            if (BlockSabotages != null) BlockSabotages.Value = Troll.BlockSabotages.Enabled;
            if (BlockVenting != null) BlockVenting.Value = Troll.BlockVenting.Enabled;

            // Sabotage
            if (UpdateSystemsDirectly != null) UpdateSystemsDirectly.Value = Sabotage.UpdateSystemsDirectly;

            // Spoofer
            if (EnableVersionSpoofing != null) EnableVersionSpoofing.Value = Spoofer.shouldSpoofVersion;
            if (UseModdedProtocol != null) UseModdedProtocol.Value = Spoofer.useModdedProtocol;

            // Anticheat
            if (AnticheatEnabled != null) AnticheatEnabled.Value = Anticheat.Enabled;
            if (CheckSpoofedPlatforms != null) CheckSpoofedPlatforms.Value = Anticheat.CheckSpoofedPlatforms;
            if (SendAnticheatNotification != null) SendAnticheatNotification.Value = Anticheat.sendNotification;
            if (DiscardAnticheatRpc != null) DiscardAnticheatRpc.Value = Anticheat.discardRpc;
            if (AnticheatPunishment != null) AnticheatPunishment.Value = (int)Anticheat.punishment;

            _config?.Save();
        }
    }
}
