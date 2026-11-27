using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MalumMenu
{
    public static class PresenceTracker
    {
        // Base of the online service. Clients only ever POST their own status here and read back the
        // online COUNT (plus announcement + required version). They cannot read anyone else's data.
        private const string BackendUrl = "https://hydralum-core.hydralum.workers.dev";
        private static readonly string SessionId = Guid.NewGuid().ToString("N");
        // The status key is the player's STABLE id (PUID, else friend code) rather than a random
        // per-launch GUID, so relaunches / extra game instances of the SAME account overwrite one row
        // instead of piling up. SessionId stays only as a last-resort fallback for a client with neither.
        private static volatile string _presenceKey;
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static CancellationTokenSource _cts;
        private static bool _started = false;

        // IMPORTANT: the Hydralum version is duplicated in BOTH plugins - here AND Hydra's PresenceTracker
        // (separate assemblies, so neither can share the constant). Bump BOTH on every Hydralum release. If they
        // drift, the lower one fails the required-version check and sets the shared "HydralumOutdated" AppDomain
        // flag, which makes the OTHER (correct) plugin pop a bogus "UPDATE REQUIRED" dialog showing equal versions.
        public const string CurrentHydralumVersion = "2.0.0";
        // Hydra's version, reported in the status payload. Malum can't reference Hydra's assembly, so keep
        // this in sync with Hydra's csproj <Version> on each Hydra bump.
        public const string HydraVersion = "2.2.0";
        public const string GitHubUrl = "https://github.com/NewTabGames/Hydralum";
        public static bool IsOutdated { get; set; } = false;
        public static string RequiredVersion { get; set; } = "1.2.0";
        // The outdated check is latched the first time we successfully read the required version (at launch).
        // After that it never changes for this session, so pushing a new required version does not suddenly
        // lock out a player who is already in-game - it only takes effect the next time they launch.
        private static volatile bool _versionChecked = false;

        public static int OnlineCount { get; private set; } = 1;

        // Thread-safe cached local player and lobby state (updated on Unity main thread)
        public static string CurrentRoomCode { get; private set; } = "";
        public static string LocalPlayerName { get; private set; } = "";
        public static int LocalPlayerId { get; private set; } = -1;
        public static string LocalFriendCode { get; private set; } = "";
        public static string LocalPuid { get; private set; } = "";
        public static string CurrentGameState { get; private set; } = "Menus";

        private static string _lastRoomCode = "";
        private static string _lastGameState = "Menus";
        private static volatile bool _forceRefresh = false;

        public static int GetOnlineCount()
        {
            var val = AppDomain.CurrentDomain.GetData("HydralumOnlineCount");
            if (val is int count && count > 0)
            {
                return count;
            }
            return Math.Max(1, OnlineCount);
        }

        private static float lastUpdateTime = 0f;

        // Called on Unity Main Thread (e.g. MenuUI.Update)
        public static void UpdateMainThread()
        {
            if (UnityEngine.Time.unscaledTime - lastUpdateTime < 1f) return;
            lastUpdateTime = UnityEngine.Time.unscaledTime;

            try
            {
                bool inOnlineGame = false;
                try
                {
                    inOnlineGame = AmongUsClient.Instance != null
                        && AmongUsClient.Instance.InOnlineScene
                        && AmongUsClient.Instance.GameId != 0
                        && (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined || AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                        && PlayerControl.LocalPlayer != null;
                }
                catch { }

                string room = "";
                if (inOnlineGame)
                {
                    try { room = InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId); } catch { }
                }

                string name = "";
                int id = -1;
                string friendCode = "";
                string puid = "";

                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
                {
                    name = PlayerControl.LocalPlayer.Data.PlayerName ?? "";
                    id = PlayerControl.LocalPlayer.PlayerId;
                    friendCode = PlayerControl.LocalPlayer.Data.FriendCode ?? "";
                }

                if (string.IsNullOrEmpty(name))
                {
                    try { name = AmongUs.Data.DataManager.Player.Customization.Name ?? ""; } catch { }
                }

                if (string.IsNullOrEmpty(name))
                {
                    try { name = PlayerPrefs.GetString("PlayerName", ""); } catch { }
                }

                if (string.IsNullOrEmpty(friendCode))
                {
                    try { friendCode = EOSManager.Instance?.FriendCode ?? ""; } catch { }
                }

                if (AmongUsClient.Instance != null)
                {
                    try
                    {
                        var localClient = PlayerControl.LocalPlayer != null ? AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer) : null;
                        if (localClient != null && !string.IsNullOrEmpty(localClient.ProductUserId))
                        {
                            puid = localClient.ProductUserId;
                        }
                        else if (AmongUsClient.Instance.ClientId >= 0)
                        {
                            var c = AmongUsClient.Instance.GetClient(AmongUsClient.Instance.ClientId);
                            if (c != null && !string.IsNullOrEmpty(c.ProductUserId))
                            {
                                puid = c.ProductUserId;
                            }
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(puid))
                {
                    try { puid = EOSManager.Instance?.ProductUserId ?? ""; } catch { }
                }

                string gameState = "Menus";
                if (inOnlineGame && !string.IsNullOrWhiteSpace(room))
                {
                    CurrentRoomCode = room.Trim().ToUpperInvariant();
                    if (ShipStatus.Instance != null)
                    {
                        if (MeetingHud.Instance != null)
                            gameState = "Meeting";
                        else
                            gameState = "In Game";
                    }
                    else
                    {
                        gameState = "Lobby";
                    }
                }
                else
                {
                    CurrentRoomCode = "";
                    gameState = "Menus";
                    id = -1;
                }

                CurrentGameState = gameState;

                if (!string.IsNullOrEmpty(name))
                {
                    LocalPlayerName = name;
                }

                LocalPlayerId = id;
                if (!string.IsNullOrEmpty(friendCode))
                {
                    LocalFriendCode = friendCode;
                }
                if (!string.IsNullOrEmpty(puid))
                {
                    LocalPuid = puid;
                }

                if (IsDevId(LocalPuid) || IsDevId(LocalFriendCode))
                {
                    try { AppDomain.CurrentDomain.SetData("HydralumDevAuthorized", true); } catch { }
                }

                var outdatedVal = AppDomain.CurrentDomain.GetData("HydralumOutdated");
                if (outdatedVal is bool b)
                {
                    IsOutdated = b;
                }
                var reqVerVal = AppDomain.CurrentDomain.GetData("HydralumRequiredVersion");
                if (reqVerVal is string s && !string.IsNullOrEmpty(s))
                {
                    RequiredVersion = s;
                }

                if (CurrentRoomCode != _lastRoomCode || CurrentGameState != _lastGameState)
                {
                    _lastRoomCode = CurrentRoomCode;
                    _lastGameState = CurrentGameState;
                    _forceRefresh = true;
                }
            }
            catch { }
        }

        private static readonly HashSet<string> HardcodedDevPuids = new(StringComparer.OrdinalIgnoreCase)
        {
            "00022b8b21ca483890f2203f12b57397"
        };

        private static readonly HashSet<string> HardcodedDevFriendCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "localcourt#0770"
        };

        public static bool IsDevId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();
            return HardcodedDevPuids.Contains(id) || HardcodedDevFriendCodes.Contains(id);
        }

        public static bool IsDevUser(PlayerControl player)
        {
            if (player == null) return false;
            if (player.Data != null) return IsDevUser(player.Data);

            try
            {
                if (AmongUsClient.Instance != null)
                {
                    var client = AmongUsClient.Instance.GetClientFromCharacter(player);
                    if (client != null)
                    {
                        if (IsDevId(client.ProductUserId) || IsDevId(client.FriendCode)) return true;
                    }
                }
            }
            catch { }

            return false;
        }

        // A player is a dev if their stable identity (PUID / friend code) matches the hardcoded dev list.
        // This resolves purely from in-game networked data, so the dev tag renders for everyone without any
        // online lookup.
        public static bool IsDevUser(NetworkedPlayerInfo playerInfo)
        {
            if (playerInfo == null || playerInfo.Disconnected) return false;

            // Check local player
            if (PlayerControl.LocalPlayer != null && (playerInfo == PlayerControl.LocalPlayer.Data || (playerInfo.Object != null && playerInfo.Object == PlayerControl.LocalPlayer)))
            {
                if (IsDevId(LocalPuid) || IsDevId(LocalFriendCode))
                    return true;
                try
                {
                    if (IsDevId(EOSManager.Instance?.ProductUserId) || IsDevId(EOSManager.Instance?.FriendCode))
                        return true;
                }
                catch { }
            }

            string targetPuid = "";
            string targetFriendCode = playerInfo.FriendCode ?? "";

            try
            {
                if (AmongUsClient.Instance != null)
                {
                    var client = AmongUsClient.Instance.GetClientFromPlayerInfo(playerInfo);
                    if (client != null)
                    {
                        if (!string.IsNullOrEmpty(client.ProductUserId)) targetPuid = client.ProductUserId;
                        if (string.IsNullOrEmpty(targetFriendCode) && !string.IsNullOrEmpty(client.FriendCode)) targetFriendCode = client.FriendCode;
                    }
                    if (string.IsNullOrEmpty(targetPuid) && playerInfo.Object != null)
                    {
                        var charClient = AmongUsClient.Instance.GetClientFromCharacter(playerInfo.Object);
                        if (charClient != null && !string.IsNullOrEmpty(charClient.ProductUserId))
                        {
                            targetPuid = charClient.ProductUserId;
                        }
                        if (string.IsNullOrEmpty(targetFriendCode) && charClient != null && !string.IsNullOrEmpty(charClient.FriendCode))
                        {
                            targetFriendCode = charClient.FriendCode;
                        }
                    }
                }
            }
            catch { }

            if (IsDevId(targetPuid) || IsDevId(targetFriendCode))
                return true;

            return false;
        }

        // True if THIS client's own stable identity is a dev. Works outside a lobby (unlike the per-player
        // overloads, which need a NetworkedPlayerInfo), so UI can gate dev-only convenience toggles.
        public static bool IsLocalDev()
        {
            try
            {
                if (IsDevId(LocalPuid) || IsDevId(LocalFriendCode)) return true;
                if (IsDevId(EOSManager.Instance?.ProductUserId) || IsDevId(EOSManager.Instance?.FriendCode)) return true;
            }
            catch { }
            return false;
        }

        public static void Start()
        {
            if (_started) return;
            _started = true;

            if (AppDomain.CurrentDomain.GetData("HydralumPresenceActive") != null)
            {
                return;
            }
            AppDomain.CurrentDomain.SetData("HydralumPresenceActive", true);

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RunPresenceLoopAsync(_cts.Token));
        }

        public static void Stop()
        {
            try
            {
                _cts?.Cancel();
                _started = false;
                // No explicit delete: once we stop sending heartbeats the server drops our row on its own
                // (it expires after a short window of silence). Clients have no way to delete rows anyway.
                AppDomain.CurrentDomain.SetData("HydralumPresenceActive", null);
            }
            catch { }
        }

        // Builds the stable status key for this account. Prefers the EOS PUID, then the friend code, and only
        // falls back to the random SessionId if neither is known yet.
        private static string BuildPresenceKey()
        {
            if (!string.IsNullOrWhiteSpace(LocalPuid)) return SanitizeKey(LocalPuid);
            if (!string.IsNullOrWhiteSpace(LocalFriendCode)) return SanitizeKey(LocalFriendCode);
            return SessionId;
        }

        private static string SanitizeKey(string s)
        {
            s = s.Trim();
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsControl(c) ? '_' : c);
            return sb.Length > 0 ? sb.ToString() : SessionId;
        }

        private static async Task RunPresenceLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string roomCode = CurrentRoomCode;
                    string pState = CurrentGameState;
                    string pName = LocalPlayerName;
                    int pId = LocalPlayerId;
                    string pFriendCode = LocalFriendCode;
                    string pPuid = LocalPuid;
                    string key = BuildPresenceKey();

                    string responseJson = null;

                    // Send our heartbeat - but only once we actually have a player name. At launch the loop can
                    // tick before the account name has loaded (pName is still ""); publishing then would create
                    // an empty-named row. Until named we still fetch the count below, so "Online" keeps working.
                    if (!string.IsNullOrEmpty(pName))
                    {
                        _presenceKey = key;

                        var payloadObj = new PresenceNode
                        {
                            key = key,
                            name = pName,
                            room = roomCode,
                            state = pState,
                            p_id = pId,
                            friend_code = pFriendCode,
                            friend_puid = pPuid,
                            last_seen_time = GetCentralTimeString(),
                            versions = new VersionInfo
                            {
                                hydralum = CurrentHydralumVersion,
                                hydra = HydraVersion,
                                malum = MalumMenu.malumVersion
                            }
                        };

                        string payload = JsonSerializer.Serialize(payloadObj);
                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using var resp = await HttpClient.PostAsync($"{BackendUrl}/presence", content, token);
                        if (resp.IsSuccessStatusCode)
                            responseJson = await resp.Content.ReadAsStringAsync(token);
                    }
                    else
                    {
                        // No name yet: read the public count/announcement/version without publishing anything.
                        string syncUrl = $"{BackendUrl}/sync?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                        using var resp = await HttpClient.GetAsync(syncUrl, token);
                        if (resp.IsSuccessStatusCode)
                            responseJson = await resp.Content.ReadAsStringAsync(token);
                    }

                    if (!string.IsNullOrWhiteSpace(responseJson) && responseJson != "null")
                        ApplySyncResponse(responseJson);
                }
                catch
                {
                    // Ignore network fluctuations
                }

                // Heartbeat every ~30s (ticks are 500ms) so the online count and the dev presence view stay
                // reasonably live without much traffic.
                int ticks = 60;
                for (int i = 0; i < ticks; i++)
                {
                    if (_forceRefresh)
                    {
                        _forceRefresh = false;
                        break;
                    }
                    try
                    {
                        await Task.Delay(500, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        // Applies the server's reply bundle: the online count, the current announcement (stashed for
        // AnnouncementManager, which reads it from the AppDomain), and the required version gate.
        private static void ApplySyncResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return;

                if (root.TryGetProperty("online", out var onlineEl) && onlineEl.TryGetInt32(out var online))
                {
                    OnlineCount = Math.Max(1, online);
                    try { AppDomain.CurrentDomain.SetData("HydralumOnlineCount", OnlineCount); } catch { }
                }

                if (root.TryGetProperty("announcement", out var annEl) && annEl.ValueKind == JsonValueKind.Object)
                {
                    try { AppDomain.CurrentDomain.SetData("HydralumAnnouncementJson", annEl.GetRawText()); } catch { }
                }

                if (root.TryGetProperty("required_version", out var rvEl))
                {
                    string reqVer = rvEl.ValueKind == JsonValueKind.String ? rvEl.GetString() : rvEl.ToString();
                    ApplyRequiredVersion(reqVer);
                }
            }
            catch { }
        }

        // Evaluates the required version ONCE, at launch (the first successful read), and latches the result for
        // the rest of the session. Later changes to the required version are ignored until the game restarts.
        private static void ApplyRequiredVersion(string reqVer)
        {
            if (_versionChecked) return;
            if (string.IsNullOrWhiteSpace(reqVer)) return;
            _versionChecked = true;

            RequiredVersion = reqVer.Trim();
            string cleanReq = RequiredVersion.TrimStart('v', 'V');
            string cleanCur = CurrentHydralumVersion.TrimStart('v', 'V');
            if (Version.TryParse(cleanReq, out var parsedReq) && Version.TryParse(cleanCur, out var parsedCur))
            {
                IsOutdated = parsedCur < parsedReq;
            }
            else
            {
                IsOutdated = !string.Equals(cleanReq, cleanCur, StringComparison.OrdinalIgnoreCase);
            }
            try { AppDomain.CurrentDomain.SetData("HydralumOutdated", IsOutdated); } catch { }
            try { AppDomain.CurrentDomain.SetData("HydralumRequiredVersion", RequiredVersion); } catch { }
        }

        private static Rect _lockoutWindowRect = new Rect(0, 0, 560, 340);
        private static GUIStyle _lockoutTitleStyle;
        private static GUIStyle _lockoutVerStyle;
        private static GUIStyle _lockoutBodyStyle;
        private static GUIStyle _lockoutBtnStyle;
        private static GUIStyle _lockoutExitBtnStyle;

        public static void RenderLockoutModalGUI()
        {
            try
            {
                var outdatedVal = AppDomain.CurrentDomain.GetData("HydralumOutdated");
                bool isOutdated = (outdatedVal is bool b && b) || IsOutdated;
                if (!isOutdated) return;

                GUI.depth = -99999;
                Color prevColor = GUI.color;
                Color prevBg = GUI.backgroundColor;

                // 1. Full-Screen Solid Dimmer
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.color = new Color(0.03f, 0.03f, 0.05f, 0.95f);
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // 2. Center Modal Rect
                float boxWidth = Mathf.Max(320f, Mathf.Min(560f, Screen.width - 40f));
                float boxHeight = Mathf.Max(260f, Mathf.Min(340f, Screen.height - 40f));
                _lockoutWindowRect = new Rect((Screen.width - boxWidth) / 2f, (Screen.height - boxHeight) / 2f, boxWidth, boxHeight);

                // 3. Block mouse clicks outside modal
                if (Event.current != null && Event.current.isMouse && !_lockoutWindowRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                }

                // 4. Modal Window with guaranteed Unity IMGUI clipping & layout
                GUI.backgroundColor = new Color(0.10f, 0.10f, 0.14f, 1f);
                _lockoutWindowRect = GUI.Window(99999, _lockoutWindowRect, (GUI.WindowFunction)DrawLockoutWindowContents, "");

                GUI.color = prevColor;
                GUI.backgroundColor = prevBg;
            }
            catch { }
        }

        private static void DrawLockoutWindowContents(int windowId)
        {
            try
            {
                var reqVerVal = AppDomain.CurrentDomain.GetData("HydralumRequiredVersion");
                string reqVer = (reqVerVal is string s && !string.IsNullOrEmpty(s)) ? s : RequiredVersion;

                string displayReqVer = reqVer.TrimStart('v', 'V');
                string displayCurVer = CurrentHydralumVersion.TrimStart('v', 'V');

                GUILayout.Space(10);

                if (_lockoutTitleStyle == null)
                {
                    _lockoutTitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 18,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(1f, 0.3f, 0.35f) }
                    };
                }
                GUILayout.Label("HYDRALUM UPDATE REQUIRED", _lockoutTitleStyle);

                GUILayout.Space(6);

                if (_lockoutVerStyle == null)
                {
                    _lockoutVerStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        richText = true,
                        normal = { textColor = Color.white }
                    };
                }
                GUILayout.Label($"Your Version: <color=#FF5555>v{displayCurVer}</color>   ➔   Required Version: <color=#00FFAA>v{displayReqVer}</color>", _lockoutVerStyle);

                GUILayout.Space(12);

                if (_lockoutBodyStyle == null)
                {
                    _lockoutBodyStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.UpperLeft,
                        wordWrap = true,
                        richText = true,
                        normal = { textColor = new Color(0.88f, 0.88f, 0.90f) }
                    };
                }
                GUILayout.Label("You are running an <b>outdated version</b> of Hydralum. It is good to keep this menu up to date so your account stays undetected and you don't get bugged out.\n\nClick the button below to download the latest release build from GitHub.", _lockoutBodyStyle);

                GUILayout.FlexibleSpace();

                // Download & Update Button (Bright Green)
                GUI.backgroundColor = new Color(0f, 0.85f, 0.5f);
                if (_lockoutBtnStyle == null)
                {
                    _lockoutBtnStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.white }
                    };
                }
                if (GUILayout.Button("DOWNLOAD & UPDATE", _lockoutBtnStyle, GUILayout.Height(42)))
                {
                    Application.OpenURL(GitHubUrl);
                    OpenPluginsFolder();
                    Application.Quit();
                }

                GUILayout.Space(8);

                // Exit Game Button (Red) - the only other option; this screen cannot be dismissed.
                GUI.backgroundColor = new Color(0.85f, 0.22f, 0.22f);
                if (_lockoutExitBtnStyle == null)
                {
                    _lockoutExitBtnStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.white }
                    };
                }
                if (GUILayout.Button("Exit Game", _lockoutExitBtnStyle, GUILayout.Height(34)))
                {
                    Application.Quit();
                }

                GUILayout.Space(8);
            }
            catch { }
        }

        // Opens the BepInEx plugins folder of THIS install (works wherever Among Us lives - Steam on another
        // drive, Epic, etc.), falling back to the default Steam path only if BepInEx can't report its own path.
        private static void OpenPluginsFolder()
        {
            try
            {
                string path = "";
                try { path = BepInEx.Paths.PluginPath; } catch { }
                if (string.IsNullOrEmpty(path))
                    path = @"C:\Program Files (x86)\Steam\steamapps\common\Among Us\BepInEx\plugins";
                path = path.Replace('/', '\\');
                System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
            }
            catch { }
        }

        private static string GetCentralTimeString()
        {
            try
            {
                TimeZoneInfo ctZone;
                try
                {
                    ctZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                }
                catch
                {
                    ctZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
                }
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ctZone).ToString("h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(-5).ToString("h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public class VersionInfo
        {
            public string hydralum { get; set; } = CurrentHydralumVersion;
            public string hydra { get; set; } = HydraVersion;
            public string malum { get; set; } = MalumMenu.malumVersion;
        }

        public class PresenceNode
        {
            public string key { get; set; } = "";
            public string name { get; set; } = "";
            public string room { get; set; } = "";
            public string state { get; set; } = "Menus";
            public int p_id { get; set; } = -1;
            public string friend_code { get; set; } = "";
            public string friend_puid { get; set; } = "";
            public string last_seen_time { get; set; } = "";
            public VersionInfo versions { get; set; } = new VersionInfo();
        }
    }
}
