using System;
using UnityEngine;
using InnerNet;
using System.Linq;
using Il2CppSystem.Collections.Generic;
using System.IO;
using Hazel;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;
using Sentry.Internal.Extensions;
using System.Runtime.CompilerServices;
using AmongUs.InnerNet.GameDataMessages;
using Il2CppInterop.Runtime.Injection;

namespace MalumMenu;

public static class Utils
{
    public static bool isPastingInput;
    public static ReferenceDataManager ReferenceDataManager = DestroyableSingleton<ReferenceDataManager>.Instance; // Useful for getting full lists of all the Among Us cosmetics IDs
    public static SabotageSystemType SabotageSystem => ShipStatus.Instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
    public static bool isShip => ShipStatus.Instance;
    public static bool isClient => AmongUsClient.Instance;
    public static bool isLobby => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !isFreePlay;
    public static bool isOnlineGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool isLocalGame => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool isFreePlay => AmongUsClient.Instance && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool isPlayer => PlayerControl.LocalPlayer;
    public static bool isHost => AmongUsClient.Instance && AmongUsClient.Instance.AmHost;
    public static bool isInGame => AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && isPlayer;
    public static bool isMeeting => MeetingHud.Instance;
    public static bool isMeetingVoting => isMeeting && MeetingHud.Instance.resultsStartedAt <= 0f;
    public static bool isMeetingProceeding => isMeeting && MeetingHud.Instance.resultsStartedAt > 0f;
    public static bool isExiling => ExileController.Instance && !(isAirshipMap && SpawnInMinigame.Instance.isActiveAndEnabled);
    public static bool isAnySabotageActive => ShipStatus.Instance && SabotageSystem.AnyActive;
    public static bool isNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
    public static bool isHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
    public static bool isSkeldMap => (MapNames)GetCurrentMapID() == MapNames.Skeld;
    public static bool isMiraHQMap => (MapNames)GetCurrentMapID() == MapNames.MiraHQ;
    public static bool isPolusMap => (MapNames)GetCurrentMapID() == MapNames.Polus;
    public static bool isDleksMap => (MapNames)GetCurrentMapID() == MapNames.Dleks;
    public static bool isAirshipMap => (MapNames)GetCurrentMapID() == MapNames.Airship;
    public static bool isFungleMap => (MapNames)GetCurrentMapID() == MapNames.Fungle;
    public const float DefaultSpeed = 2.5f;
    public const float DefaultGhostSpeed = 3f;

    // Checks if LocalPlayer's speed is at its default value
    public static bool IsSpeedDefault(bool forGhost = false)
    {
        return forGhost ? Mathf.Approximately(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed, DefaultGhostSpeed) :
            Mathf.Approximately(PlayerControl.LocalPlayer.MyPhysics.Speed, DefaultSpeed);
    }

    // Snaps LocalPlayer's speed to the default if within snapRange
    public static void SnapSpeedToDefault(float snapRange, bool forGhost = false)
    {
        if (forGhost)
        {
            PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed - DefaultGhostSpeed)
                                                             < snapRange ? DefaultGhostSpeed : PlayerControl.LocalPlayer.MyPhysics.GhostSpeed;
        }
        else
        {
            PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed - DefaultSpeed)
                                                        < snapRange ? DefaultSpeed : PlayerControl.LocalPlayer.MyPhysics.Speed;
        }
    }

    // Gets a player's real name, display name, and whether they are disguised or not
    public static (string realName, string displayName, bool isDisguised) GetPlayerIdentity(PlayerControl player)
    {
        if (player == null || player.Data == null) return ("", "", false);

        var realName = $"<color=#{ColorUtility.ToHtmlStringRGB(player.Data.Color)}>{player.Data.PlayerName}</color>";
        int colorId = (player.CurrentOutfit != null && player.CurrentOutfit.ColorId >= 0 && player.CurrentOutfit.ColorId < Palette.PlayerColors.Length)
            ? player.CurrentOutfit.ColorId
            : 0;
        string outfitName = player.CurrentOutfit != null ? player.CurrentOutfit.PlayerName : player.Data.PlayerName;
        var displayName = $"<color=#{ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[colorId])}>{outfitName}</color>";
        var isDisguised = outfitName != player.Data.PlayerName;

        return (realName, displayName, isDisguised);
    }

    // Checks if player is currently vanished
    public static bool IsVanished(NetworkedPlayerInfo playerInfo)
    {
        PhantomRole phantomRole = playerInfo.Role as PhantomRole;

        if (phantomRole != null)
        {
            return phantomRole.fading || phantomRole.isInvisible;
        }

        return false;
    }

    // Checks whether a player is a valid target depending on whether killAnyone cheat is enabled or not
    public static bool IsValidTarget(NetworkedPlayerInfo target)
    {
        var killAnyoneRequirements = target && !target.Disconnected && target.Object.Visible && target.PlayerId != PlayerControl.LocalPlayer.PlayerId && target.Role && target.Object;

        var fullRequirements = killAnyoneRequirements && !target.IsDead && !target.Object.inVent && !target.Object.inMovingPlat && target.Role.CanBeKilled;

        return CheatToggles.killAnyone ? killAnyoneRequirements : fullRequirements;
    }

    public static List<NetworkedPlayerInfo> GetAllPlayerData()
    {
        var playerDataList = new List<NetworkedPlayerInfo>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null)
            {
                playerDataList.Add(player.Data);
            }
        }

        return playerDataList;
    }

    // Adjusts HUD resolution
    // Used to fix UI problems when zooming out
    public static void AdjustResolution()
    {
        if (Screen.height <= 0) return;
        ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
    }

    // Gets RoleBehaviour from a RoleType
    public static RoleBehaviour GetBehaviourByRoleType(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.ToArray().First(r => r.Role == roleType);
    }

    // Gets RoleBehaviour from a TeamType
    public static RoleBehaviour GetBehaviourByTeamType(RoleTeamTypes roleTeamType)
    {
        RoleTypes roleType = (RoleTypes)Enum.Parse(typeof(RoleTypes), roleTeamType.ToString(), true);
        RoleBehaviour role = GetBehaviourByRoleType(roleType);

        return role;
    }

    public static void ForceSetScanner(PlayerControl player, bool toggle)
    {
        var count = ++player.scannerCount;
        player.SetScanner(toggle, count);
        RpcSetScannerMessage rpcMessage = new(player.NetId, toggle, count);
        AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
    }

    public static void ForcePlayAnimation(byte animationType)
    {
        // PlayerControl.LocalPlayer.RpcPlayAnimation(1) wouldn't work if visual tasks are turned off
        // The below way makes sure it works regardless of visual task settings

        PlayerControl.LocalPlayer.PlayAnimation(animationType);
        RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, animationType);
        AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
    }

    // Coroutine to teleport the LocalPlayer to a position after a delay
    public static System.Collections.IEnumerator DelayedSnapTo(Vector2 position, float delay = 0.25f)
    {
        yield return new WaitForSeconds(delay);
        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
    }

    // Kills any player using RPC calls
    public static void MurderPlayer(PlayerControl target, MurderResultFlags result)
    {
        if (isFreePlay)
        {

            PlayerControl.LocalPlayer.MurderPlayer(target, MurderResultFlags.Succeeded);
            return;

        }

        foreach (var item in PlayerControl.AllPlayerControls)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, AmongUsClient.Instance.GetClientIdFromCharacter(item));
            writer.WriteNetObject(target);
            writer.Write((int)result);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static void CompleteTask(PlayerTask task)
    {
        if (isFreePlay)
        {
            PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            return;
        }

        var hostData = AmongUsClient.Instance.GetHost();
        if (hostData == null || hostData.Character.Data.Disconnected) return;

        if (task.IsComplete) return;
        foreach (var item in PlayerControl.AllPlayerControls)
        {
            var messageWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.CompleteTask, SendOption.Reliable, AmongUsClient.Instance.GetClientIdFromCharacter(item));
            messageWriter.WritePacked(task.Id);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }

    // Opens Chat UI
    public static void OpenChat()
    {
        if (!DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.chatScreen.SetActive(true);
            PlayerControl.LocalPlayer.NetTransform.Halt();
            DestroyableSingleton<HudManager>.Instance.Chat.StartCoroutine(DestroyableSingleton<HudManager>.Instance.Chat.CoOpen());
            if (DestroyableSingleton<FriendsListManager>.InstanceExists)
            {
                DestroyableSingleton<FriendsListManager>.Instance.SetFriendButtonColor(true);
            }
            if (DestroyableSingleton<HudManager>.Instance.Chat.chatNotification.gameObject.activeSelf)
			{
				DestroyableSingleton<HudManager>.Instance.Chat.chatNotification.Close();
			}
        }

    }

    // Draws a tracer line between two GameObjects
    public static void DrawTracer(GameObject sourceObject, GameObject targetObject, Color color)
    {
        var lineRenderer = sourceObject.GetComponent<LineRenderer>();

        if (!lineRenderer)
        {
            lineRenderer = sourceObject.AddComponent<LineRenderer>();
        }

        lineRenderer.SetVertexCount(2);
        lineRenderer.SetWidth(0.02F, 0.02F);

        // I just picked an already existing material from the game
        Material material = DestroyableSingleton<HatManager>.Instance.PlayerMaterial;

        lineRenderer.material = material;
        lineRenderer.SetColors(color, color);

        lineRenderer.SetPosition(0, sourceObject.transform.position);
        lineRenderer.SetPosition(1, targetObject.transform.position);
    }

    // Returns whether the ChatUI should be active or not
    public static bool IsChatUiActive()
    {
        try
        {
            return CheatToggles.enableChat || MeetingHud.Instance || !ShipStatus.Instance || PlayerControl.LocalPlayer.Data.IsDead;
        }
        catch
        {
            return false;
        }
    }

    // Returns the max number of nested RPCs that can be in a GameData message
    // without getting kicked by AC
    public static int GetMaxRpcPackingLimit()
    {
        int num = 0;

        if (isClient && AmongUsClient.Instance.AmHost)
        {
            num = GameManager.Instance.LogicOptions.MaxPlayers * 2;
        }

        return 10 + num;
    }

    // Closes Chat UI
    public static void CloseChat()
    {
        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
        }
    }

    // Gets the distance between two players
    public static float GetDistanceBetween(PlayerControl source, PlayerControl target)
    {

        Vector2 vector = target.GetTruePosition() - source.GetTruePosition();
		float magnitude = vector.magnitude;

        return magnitude;

    }

    // Returns a list of all the players in the game ordered from closest to farthest (from LocalPlayer by default)
    public static System.Collections.Generic.List<PlayerControl> GetPlayersSortedByDistance(PlayerControl source = null)
    {

        if (source.IsNull())
        {
            source = PlayerControl.LocalPlayer;
        }

        System.Collections.Generic.List<PlayerControl> outputList = new System.Collections.Generic.List<PlayerControl>();

        outputList.Clear();

        var allPlayers = GameData.Instance?.AllPlayers;
        if (allPlayers != null)
        {
            foreach (var playerInfo in allPlayers)
            {
                if (playerInfo != null && playerInfo.Object != null)
                {
                    outputList.Add(playerInfo.Object);
                }
            }
        }

        if (source != null)
        {
            outputList = outputList.OrderBy(target => GetDistanceBetween(source, target)).ToList();
        }

        return outputList;
    }

    // Returns current map ID if available
    public static byte GetCurrentMapID()
    {
        // Works for the tutorial
        if (isFreePlay)
        {
            return (byte)AmongUsClient.Instance.TutorialMapId;
        }

        // Works for local / online games
        if (GameOptionsManager.Instance?.currentGameOptions != null)
        {
            return GameOptionsManager.Instance.currentGameOptions.MapId;
        }

        // Defaults to byte.MaxValue if the current map ID is unavailable
        return byte.MaxValue;
    }

    // Gets SystemType of the room the player is currently in
    public static SystemTypes GetCurrentRoom()
    {
        return HudManager.Instance.roomTracker.LastRoom.RoomId;
    }

    // Gets the PlainShipRoom of room that overlaps specified position
    public static PlainShipRoom GetRoomFromPosition(Vector2 position)
    {
        return ShipStatus.Instance == null ? null : ShipStatus.Instance.AllRooms.FirstOrDefault(
            room => room != null && room.roomArea != null && room.roomArea.OverlapPoint(position));
    }

    // Returns colored ping text for PingTracker
    public static string GetColoredPingText(string pingText, int ping)
    {
        return ping switch
        {
            < 1 => $"<color=#b8b8b8>{pingText}</color>", // Grey for ping < 1
            < 100 => $"<color=#00ff00ff>{pingText}</color>", // Green for ping < 100
            < 400 => $"<color=#ffff00ff>{pingText}</color>", // Yellow for 100 < ping < 400
            _ => $"<color=#ff0000ff>{pingText}</color>" // Red for ping > 400
        };
    }

    // Returns the current approximate FPS
    public static int GetFps()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0.0001f) return 60;
        return (int)(1f / dt);
    }

    // Gets a UnityEngine.KeyCode from a string
    public static KeyCode StringToKeycode(string keyCodeStr)
    {

        if(!string.IsNullOrEmpty(keyCodeStr)) // Empty strings are automatically invalid
        {
            try
            {
                // Case-insensitive parse of UnityEngine.KeyCode to check if string is valid
                KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), keyCodeStr, true);

                return keyCode;

            }

            catch { }
        }

        return KeyCode.Delete; // If string is invalid, return Delete as the default key
    }

    // Gets a platform type from a string
    public static bool StringToPlatformType(string platformStr, out Platforms? platform)
    {
        if (!string.IsNullOrEmpty(platformStr)) // Empty strings are automatically invalid
        {
            try
            {
                // Case-insensitive parse of Platforms from string (if it valid)
                platform = (Platforms)Enum.Parse(typeof(Platforms), platformStr, true);

                return true; // If platform type is valid, return false
            }catch{}
        }

        platform = null;
        return false; // If platform type is invalid, return false
    }

    // Whether the current lobby enforces server-side anticheat. True on vanilla online lobbies (net
    // objects owned by the server), false on local, freeplay, and modded lobbies. Ported from Hydra.
    public static bool IsAnticheatPresent()
    {
        if (Constants.IsVersionModded() || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
            return false;

        // -2 is the host owner id; a NetworkedPlayerInfo owned by anything else (e.g. -4 server) means
        // the backend is authoritative, i.e. a real anticheat is present.
        return PlayerControl.LocalPlayer.Data.OwnerId != -2;
    }

    public static string PlatformTypeToString(Platforms platform)
    {
        return platform switch
        {
            Platforms.StandaloneEpicPC => "Epic Games",
            Platforms.StandaloneSteamPC => "Steam",
            Platforms.StandaloneMac => "Mac",
            Platforms.StandaloneWin10 => "Microsoft Store",
            Platforms.StandaloneItch => "Itch.io",
            Platforms.IPhone => "iPhone / iPad",
            Platforms.Android => "Android",
            Platforms.Switch => "Nintendo Switch",
            Platforms.Xbox => "Xbox",
            Platforms.Playstation => "PlayStation",
            (Platforms)112 => "Starlight",
            _ => "Unknown"
        };
    }

    // Gets the name for a specified player's role as a string
    // Gets the name for a specified player's role as a string
    // Strings are automatically translated
    public static string GetRoleName(NetworkedPlayerInfo playerData)
    {
        if (playerData == null || playerData.Role == null) return "Unknown";
        try
        {
            var tc = DestroyableSingleton<TranslationController>.Instance;
            if (tc != null)
            {
                var translatedRole = tc.GetString(playerData.Role.StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
                if (translatedRole != "STRMISS" && !string.IsNullOrEmpty(translatedRole)) return translatedRole;

                translatedRole = tc.GetString(GetBehaviourByTeamType(playerData.Role.TeamType).StringName, Il2CppSystem.Array.Empty<Il2CppSystem.Object>());
                if (translatedRole != "STRMISS" && !string.IsNullOrEmpty(translatedRole)) return translatedRole;
            }
        }
        catch { }

        return playerData.Role.Role.ToString();
    }

    // Gets the appropriate nametag for a player
    public static string GetNameTag(NetworkedPlayerInfo playerInfo, string playerName, bool isChat = false)
    {
        var nameTag = playerName;

        if (playerInfo == null || playerInfo.Disconnected) return nameTag;

        try
        {
            var client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromPlayerInfo(playerInfo) : null;
            var host = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetHost() : null;
            var level = playerInfo.PlayerLevel + 1;

            var platform = "Unknown";
            if (!isLocalGame && client != null && client.PlatformData != null)
            {
                try { platform = PlatformTypeToString(client.PlatformData.Platform); } catch { }
            }

            var hostString = (client != null && host != null && client == host) ? "Host - " : "";

            var role = playerInfo.Role;
            var roleColor = "ffffff";
            var roleName = "Unknown";

            if (role != null)
            {
                roleColor = ColorUtility.ToHtmlStringRGB(role.TeamColor);
                roleName = GetRoleName(playerInfo);
            }

            if (CheatToggles.seeRoles)
            {
                if (CheatToggles.seePlayerInfo)
                {
                    if (isChat)
                    {
                        return $"<color=#{roleColor}>{nameTag} <size=70%>{roleName}</size></color> <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                    }

                    return $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n<color=#{roleColor}><size=70%>{roleName}</size>\r\n{nameTag}</color>";
                }
                else
                {
                    if (isChat)
                    {
                        return $"<color=#{roleColor}>{nameTag} <size=70%>{roleName}</size></color>";
                    }

                    return $"<color=#{roleColor}><size=70%>{roleName}</size>\r\n{nameTag}</color>";
                }
            }
            else
            {
                if (CheatToggles.seePlayerInfo)
                {
                    bool isSameTeam = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.Role != null && role != null && PlayerControl.LocalPlayer.Data.Role.NameColor == role.NameColor;

                    if (isSameTeam && role != null)
                    {
                        string nameColorHex = ColorUtility.ToHtmlStringRGB(role.NameColor);
                        if (isChat)
                        {
                            return $"<color=#{nameColorHex}>{nameTag}</color> <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                        }

                        return $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n<color=#{nameColorHex}>{nameTag}</color>";
                    }
                    else
                    {
                        if (isChat)
                        {
                            return $"{nameTag} <size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>";
                        }

                        return $"<size=70%><color=#fb0>{hostString}Lv:{level} - {platform}</color></size>\r\n{nameTag}";
                    }
                }
                else
                {
                    bool isSameTeam = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.Role != null && role != null && PlayerControl.LocalPlayer.Data.Role.NameColor == role.NameColor;

                    if (isSameTeam && role != null && !isChat)
                    {
                        return $"<color=#{ColorUtility.ToHtmlStringRGB(role.NameColor)}>{nameTag}</color>";
                    }
                }
            }
        }
        catch { }

        return nameTag;
    }

    // Returns a player's NetworkedPlayerInfo from their client ID
    public static NetworkedPlayerInfo GetPlayerDataFromClientId(int clientId)
    {
        var players = PlayerControl.AllPlayerControls.ToArray();

        for (int i = 0; i < players.Count; i++)
		{   NetworkedPlayerInfo playerData = players[i].Data;

			if (playerData.ClientId == clientId)
			{
				return playerData;
			}
		}

        return null; // Returns null if no matching player is found
    }

    // Returns a random 1 - 12 characters long name
    public static string GetRandomName()
    {
        var length = UnityEngine.Random.Range(1, 13);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }

    // Returns current AmongUsClient ping in ms
    public static int GetPing()
    {
        if (isClient && AmongUsClient.Instance.AmClient)
        {
            return AmongUsClient.Instance.Ping;
        }
        else
        {
            return 0; // Returns 0 if not connected to a game
        }
    }

    // Shows a custom popup ingame
    // Found here: https://github.com/NuclearPowered/Reactor/blob/6eb0bf19c30733b78532dada41db068b2b247742/Reactor/Networking/Patches/HttpPatches.cs
    public static void ShowPopup(string text)
    {
        var popup = UnityEngine.Object.Instantiate(DiscordManager.Instance.discordPopup, Camera.main!.transform);

        var background = popup.transform.Find("Background").GetComponent<SpriteRenderer>();
        var size = background.size;
        size.x *= 2.5f;
        background.size = size;

        popup.TextAreaTMP.fontSizeMin = 2;
        popup.Show(text);
    }

    public static void ShowNewPopup(string text)
    {
        DestroyableSingleton<DisconnectPopup>.Instance.ShowCustom(text);
    }

    // Loads sprites from manifest resources
    // Found here: https://github.com/Loonie-Toons/TOHE-Restored/blob/TOHE/Modules/Utils.cs
    public static Dictionary<string, Sprite> CachedSprites = new();
    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;

            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            MalumMenu.Log.LogError($"Failed to read Texture: {path}");
        }
        return null;
    }

    // Loads textures from manifest resources
    // Found here: https://github.com/Loonie-Toons/TOHE-Restored/blob/TOHE/Modules/Utils.cs
    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();

            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray(), false);
            return texture;
        }
        catch
        {
            MalumMenu.Log.LogError($"Failed to read Texture: {path}");
        }
        return null;
    }

    // Opens the config file in the default text editor
    public static void OpenConfigFile()
    {
        var configFilePath = MalumMenu.Plugin.Config.ConfigFilePath;
        var configEditor = MalumMenu.configEditor.Value;

        if (!string.IsNullOrWhiteSpace(configEditor))
        {
            if (File.Exists(configFilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = configEditor,
                        Arguments = configFilePath,
                        UseShellExecute = true
                        //Verb = "edit"
                    });
                }
                catch (Exception ex)
                {
                    MalumMenu.Log.LogError(ex.Message);
                }
            }
            else
            {
                MalumMenu.Log.LogError("Configuration file does not exist");
            }
        }
        else
        {
            MalumMenu.Log.LogError("Configuration editor not specified");
        }
    }

    public class PanicCleaner : MonoBehaviour
    {
        // Creates a PanicCleaner to unpatch Harmony
        public static void Create()
        {
            ClassInjector.RegisterTypeInIl2Cpp<PanicCleaner>();
            var go = new GameObject("MalumMenu_PanicCleaner");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<PanicCleaner>();
        }

        // Unpatching Harmony in handled in the next frame after creation
        // This allows some patches to run for a last time and finish properly
        private void LateUpdate()
        {
            try { Harmony.UnpatchID(MalumMenu.Id); } catch { }
            Destroy(gameObject);
        }
    }

    public static void Eject() => Panic();

    public static void Panic()
    {
        if (MalumMenu.isPanicked) return;
        MalumMenu.isPanicked = true;

        CheatToggles.DisableAll();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = false;

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name == "MainMenu" || scene.name == "MatchMaking")
        {
            SceneManager.LoadScene(scene.name);
        }

        UnityEngine.Object.Destroy(MalumMenu.menuUI);

        UnityEngine.Object.Destroy(MalumMenu.consoleUI);
        UnityEngine.Object.Destroy(MalumMenu.doorsUI);
        UnityEngine.Object.Destroy(MalumMenu.tasksUI);
        UnityEngine.Object.Destroy(MalumMenu.protectUI);
        // UnityEngine.Object.Destroy(MalumMenu.rolesUI);

        UnityEngine.Object.Destroy(MalumMenu.keybindListener);

        // Eject HydraMenu simultaneously if present
        try
        {
            System.Type hydraType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "HydraMenu")
                {
                    hydraType = asm.GetType("HydraMenu.Hydra");
                    if (hydraType != null) break;
                }
            }
            if (hydraType != null)
            {
                var ejectMethod = hydraType.GetMethod("Eject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                ejectMethod?.Invoke(null, null);
            }
        }
        catch { }

        PanicCleaner.Create();
    }
}

public static class PlayerVoteAreaHelper
{
    private static readonly System.Reflection.PropertyInfo PlayerIdProp = typeof(PlayerVoteArea).GetProperty("PlayerId") ?? typeof(PlayerVoteArea).GetProperty("TargetPlayerId");
    private static readonly System.Reflection.PropertyInfo VotedForProp = typeof(PlayerVoteArea).GetProperty("VotedForId") ?? typeof(PlayerVoteArea).GetProperty("VotedFor");

    public const byte HasNotVoted = 254;
    public const byte SkippedVote = 253;
    public const byte MissedVote = 252;
    public const byte DeadVote = 255;

    public static byte GetPlayerId(PlayerVoteArea area)
    {
        if (area == null || PlayerIdProp == null) return 255;
        try
        {
            object val = PlayerIdProp.GetValue(area);
            if (val == null) return 255;
            return Convert.ToByte(val);
        }
        catch
        {
            return 255;
        }
    }

    public static int GetVotedFor(PlayerVoteArea area)
    {
        if (area == null || VotedForProp == null) return HasNotVoted;
        try
        {
            object val = VotedForProp.GetValue(area);
            if (val == null) return HasNotVoted;
            return Convert.ToInt32(val);
        }
        catch
        {
            return HasNotVoted;
        }
    }
}

