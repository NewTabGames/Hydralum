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
        private const string FirebaseUrl = "https://hydralum-presence-default-rtdb.firebaseio.com/presence";
        private static readonly string SessionId = Guid.NewGuid().ToString("N");
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static CancellationTokenSource _cts;
        private static bool _started = false;

        public const string CurrentHydralumVersion = "1.2.6";
        public const string GitHubActionsUrl = "https://github.com/NewTabGames/Hydralum/actions";
        public static bool IsOutdated { get; set; } = false;
        public static string RequiredVersion { get; set; } = "1.2.0";

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

        // Structured list of active Hydralum peers in the current lobby
        private static readonly List<PeerData> _currentRoomPeers = new();
        private static readonly object _peerLock = new();

        public static int GetOnlineCount()
        {
            var val = AppDomain.CurrentDomain.GetData("HydralumOnlineCount");
            if (val is int count && count > 0)
            {
                return count;
            }
            return Math.Max(1, OnlineCount);
        }

        // Called on Unity Main Thread (e.g. MenuUI.Update)
        public static void UpdateMainThread()
        {
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

                    // Clear stale peer data from previous lobby
                    lock (_peerLock)
                    {
                        _currentRoomPeers.Clear();
                    }
                    try { AppDomain.CurrentDomain.SetData("HydralumPeersJson", "[]"); } catch { }
                }

                CurrentGameState = gameState;

                if (!string.IsNullOrEmpty(name))
                {
                    LocalPlayerName = name;
                }

                LocalPlayerId = id;
                LocalFriendCode = friendCode ?? "";
                LocalPuid = puid ?? "";

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

        private static readonly HashSet<string> RemoteDevIds = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsDevId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();
            if (HardcodedDevPuids.Contains(id) || HardcodedDevFriendCodes.Contains(id)) return true;
            lock (_peerLock)
            {
                if (RemoteDevIds.Contains(id)) return true;
            }
            return false;
        }

        public static bool IsDevUser(NetworkedPlayerInfo playerInfo)
        {
            if (playerInfo == null || playerInfo.Disconnected) return false;

            // Check local player
            if (PlayerControl.LocalPlayer != null && playerInfo == PlayerControl.LocalPlayer.Data)
            {
                if (IsDevId(LocalPuid) || IsDevId(LocalFriendCode))
                    return true;
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
                    }
                }
            }
            catch { }

            if (IsDevId(targetPuid) || IsDevId(targetFriendCode))
                return true;

            // Also check if any active room peer marked as dev matches this player
            lock (_peerLock)
            {
                foreach (var peer in _currentRoomPeers)
                {
                    if (peer == null) continue;
                    if (IsDevId(peer.Puid) || IsDevId(peer.FriendCode))
                    {
                        if (!string.IsNullOrWhiteSpace(peer.Puid) && !string.IsNullOrWhiteSpace(targetPuid) &&
                            string.Equals(peer.Puid.Trim(), targetPuid.Trim(), StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (!string.IsNullOrWhiteSpace(peer.FriendCode) && !string.IsNullOrWhiteSpace(targetFriendCode) &&
                            string.Equals(peer.FriendCode.Trim(), targetFriendCode.Trim(), StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (!string.IsNullOrWhiteSpace(peer.Name) && string.Equals(peer.Name, playerInfo.PlayerName, StringComparison.Ordinal) &&
                            peer.PlayerId == playerInfo.PlayerId && playerInfo.PlayerId >= 0)
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool IsHydralumUser(NetworkedPlayerInfo playerInfo)
        {
            if (playerInfo == null || playerInfo.Disconnected) return false;

            // Devs are always Hydralum users
            if (IsDevUser(playerInfo)) return true;

            // Local player is always running Hydralum
            if (PlayerControl.LocalPlayer != null && playerInfo == PlayerControl.LocalPlayer.Data)
            {
                return true;
            }

            string targetName = playerInfo.PlayerName ?? "";
            int targetId = playerInfo.PlayerId;
            string targetFriendCode = playerInfo.FriendCode ?? "";
            string targetPuid = "";

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
                    }
                }
            }
            catch { }

            // Check in-process cache
            lock (_peerLock)
            {
                if (CheckPeerMatch(_currentRoomPeers, targetName, targetId, targetFriendCode, targetPuid))
                    return true;
            }

            // Cross-plugin fallback via AppDomain
            try
            {
                if (AppDomain.CurrentDomain.GetData("HydralumPeersJson") is string peersJson && !string.IsNullOrEmpty(peersJson))
                {
                    var domainPeers = JsonSerializer.Deserialize<List<PeerData>>(peersJson);
                    if (domainPeers != null && CheckPeerMatch(domainPeers, targetName, targetId, targetFriendCode, targetPuid))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool CheckPeerMatch(List<PeerData> peers, string targetName, int targetId, string targetFriendCode, string targetPuid)
        {
            if (peers == null || peers.Count == 0) return false;

            foreach (var peer in peers)
            {
                if (peer == null) continue;

                // 1. Pinpoint Match via PUID (EOS Product User ID)
                if (!string.IsNullOrWhiteSpace(peer.Puid) && !string.IsNullOrWhiteSpace(targetPuid))
                {
                    if (string.Equals(peer.Puid.Trim(), targetPuid.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // 2. Pinpoint Match via Friend Code
                if (!string.IsNullOrWhiteSpace(peer.FriendCode) && !string.IsNullOrWhiteSpace(targetFriendCode))
                {
                    if (string.Equals(peer.FriendCode.Trim(), targetFriendCode.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // 3. Strict Fallback: Both exact Name AND PlayerId must match simultaneously (never name-alone or id-alone)
                if (!string.IsNullOrWhiteSpace(peer.Name) && !string.IsNullOrWhiteSpace(targetName))
                {
                    if (string.Equals(peer.Name, targetName, StringComparison.Ordinal) && peer.PlayerId == targetId && targetId >= 0)
                    {
                        return true;
                    }
                }
            }
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
                // Fire-and-forget cleanup (non-blocking)
                _ = Task.Run(async () => { try { using var r = await HttpClient.DeleteAsync($"{FirebaseUrl}/{SessionId}.json"); } catch { } });
                AppDomain.CurrentDomain.SetData("HydralumPresenceActive", null);
            }
            catch { }
        }

        private static async Task RunPresenceLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string roomCode = CurrentRoomCode;
                    string pState = CurrentGameState;
                    string pName = LocalPlayerName;
                    int pId = LocalPlayerId;
                    string pFriendCode = LocalFriendCode;
                    string pPuid = LocalPuid;

                    // 1. Send heartbeat
                    var payloadObj = new PresenceNode
                    {
                        name = pName,
                        room = roomCode,
                        state = pState,
                        p_id = pId,
                        friend_code = pFriendCode,
                        friend_puid = pPuid,
                        last_seen = now,
                        last_seen_time = GetCentralTimeString(),
                        versions = new VersionInfo
                        {
                            hydralum = CurrentHydralumVersion,
                            hydra = "1.10.0",
                            malum = "3.3.0"
                        }
                    };

                    string payload = JsonSerializer.Serialize(payloadObj);
                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    {
                        using var putResp = await HttpClient.PutAsync($"{FirebaseUrl}/{SessionId}.json", content, token);
                    }

                    // 2. Fetch active presence nodes
                    string fetchUrl = $"{FirebaseUrl}.json?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                    using var response = await HttpClient.GetAsync(fetchUrl, token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(token);
                        if (!string.IsNullOrWhiteSpace(json) && json != "null")
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, PresenceNode>>(json);
                            if (data != null)
                            {
                                int active = 0;
                                var matchedPeers = new List<PeerData>();

                                foreach (var entry in data)
                                {
                                    long age = entry.Value != null ? Math.Abs(now - entry.Value.last_seen) : 999;
                                    if (entry.Value != null && age < 90)
                                    {
                                        active++;

                                        // Match peers in the same lobby
                                        if (!string.IsNullOrEmpty(roomCode) &&
                                            string.Equals(entry.Value.room, roomCode, StringComparison.OrdinalIgnoreCase) &&
                                            entry.Key != SessionId)
                                        {
                                            matchedPeers.Add(new PeerData
                                            {
                                                Name = entry.Value.name ?? "",
                                                PlayerId = entry.Value.p_id,
                                                FriendCode = entry.Value.friend_code ?? "",
                                                Puid = !string.IsNullOrEmpty(entry.Value.friend_puid) ? entry.Value.friend_puid : ""
                                            });
                                        }
                                    }
                                    else if (entry.Key != SessionId && (entry.Value == null || age > 180))
                                    {
                                        // Prune stale session from Firebase (fire-and-forget with proper disposal)
                                        _ = Task.Run(async () => { try { using var r = await HttpClient.DeleteAsync($"{FirebaseUrl}/{entry.Key}.json", token); } catch { } });
                                    }
                                }

                                lock (_peerLock)
                                {
                                    _currentRoomPeers.Clear();
                                    _currentRoomPeers.AddRange(matchedPeers);
                                }

                                try
                                {
                                    string peersJson = JsonSerializer.Serialize(matchedPeers);
                                    AppDomain.CurrentDomain.SetData("HydralumPeersJson", peersJson);
                                }
                                catch { }

                                OnlineCount = Math.Max(1, active);
                                AppDomain.CurrentDomain.SetData("HydralumOnlineCount", OnlineCount);

                                // Update live summary in Firebase without overwriting required_version
                                try
                                {
                                    var statsPayload = $"{{\"online_players\":{OnlineCount},\"last_updated\":{now}}}";
                                    using var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"), "https://hydralum-presence-default-rtdb.firebaseio.com/stats.json")
                                    {
                                        Content = new StringContent(statsPayload, Encoding.UTF8, "application/json")
                                    };
                                    using var patchRes = await HttpClient.SendAsync(patchReq, token);
                                }
                                catch { }
                            }
                        }
                    }

                    // 3. Fetch announcement
                    await AnnouncementManager.RefreshAsync(token);

                    // 4. Fetch stats and verify version requirement
                    try
                    {
                        string statsUrl = $"https://hydralum-presence-default-rtdb.firebaseio.com/stats.json?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                        using var statsResp = await HttpClient.GetAsync(statsUrl, token);
                        if (statsResp.IsSuccessStatusCode)
                        {
                            var statsJson = await statsResp.Content.ReadAsStringAsync(token);
                            if (!string.IsNullOrWhiteSpace(statsJson) && statsJson != "null")
                            {
                                using var statsDoc = JsonDocument.Parse(statsJson);
                                if (statsDoc.RootElement.TryGetProperty("required_version", out var reqVerProp))
                                {
                                    string reqVer = reqVerProp.ValueKind == JsonValueKind.String ? (reqVerProp.GetString() ?? "") : reqVerProp.ToString();
                                    if (!string.IsNullOrWhiteSpace(reqVer))
                                    {
                                        RequiredVersion = reqVer.Trim();
                                        // Semantic version comparison: only lock out if current < required
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
                                        AppDomain.CurrentDomain.SetData("HydralumOutdated", IsOutdated);
                                        AppDomain.CurrentDomain.SetData("HydralumRequiredVersion", RequiredVersion);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                catch
                {
                    // Ignore network fluctuations
                }

                // Wait 5 seconds, or wake up sooner if room changed
                for (int i = 0; i < 10; i++)
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
                GUILayout.Label("You are running an <b>outdated version</b> of Hydralum. It is good to keep this menu up to date so your account stays undetected and you don't get bugged out.\n\nClick the button below to download the latest release build from GitHub Actions.", _lockoutBodyStyle);

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
                if (GUILayout.Button("DOWNLOAD & UPDATE (GitHub Actions)", _lockoutBtnStyle, GUILayout.Height(42)))
                {
                    Application.OpenURL(GitHubActionsUrl);
                }

                GUILayout.Space(8);

                // Exit Game Button (Red)
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
                if (GUILayout.Button("Exit Game", _lockoutExitBtnStyle, GUILayout.Height(28)))
                {
                    Application.Quit();
                }

                GUILayout.Space(8);
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
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ctZone).ToString("h:mm:ss tt");
            }
            catch
            {
                return DateTime.UtcNow.AddHours(-5).ToString("h:mm:ss tt");
            }
        }

        public class VersionInfo
        {
            public string hydralum { get; set; } = CurrentHydralumVersion;
            public string hydra { get; set; } = "1.10.0";
            public string malum { get; set; } = "3.3.0";
        }

        public class PeerData
        {
            public string Name { get; set; } = "";
            public int PlayerId { get; set; } = -1;
            public string FriendCode { get; set; } = "";
            public string Puid { get; set; } = "";
        }

        public class PresenceNode
        {
            public string name { get; set; } = "";
            public string room { get; set; } = "";
            public string state { get; set; } = "Menus";
            public int p_id { get; set; } = -1;
            public string friend_code { get; set; } = "";
            public string friend_puid { get; set; } = "";
            public long last_seen { get; set; }
            public string last_seen_time { get; set; } = "";
            public VersionInfo versions { get; set; } = new VersionInfo();
        }
    }
}
