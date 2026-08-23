using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MalumMenu
{
    public static class PresenceTracker
    {
        private const string FirebaseUrl = "https://hydralum-presence-default-rtdb.firebaseio.com/presence";
        private static readonly string SessionId = Guid.NewGuid().ToString("N");
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static CancellationTokenSource _cts;
        private static bool _started = false;

        public static int OnlineCount { get; private set; } = 1;

        // Thread-safe cached local player and lobby state (updated on Unity main thread)
        public static string CurrentRoomCode { get; private set; } = "";
        public static string LocalPlayerName { get; private set; } = "";
        public static int LocalPlayerId { get; private set; } = -1;
        public static string LocalFriendCode { get; private set; } = "";
        public static string LocalPuid { get; private set; } = "";

        private static string _lastRoomCode = "";
        private static volatile bool _forceRefresh = false;

        // Set of active Hydralum user identifiers in the current lobby/game
        private static readonly HashSet<string> _currentRoomPeers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<byte> _currentRoomPeerIds = new();

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
                string room = "";
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.GameId != 0)
                {
                    try { room = InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId); } catch { }
                }

                string name = "";
                int id = -1;
                string fc = "";
                string puid = "";

                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
                {
                    name = PlayerControl.LocalPlayer.Data.PlayerName ?? "";
                    id = PlayerControl.LocalPlayer.PlayerId;
                    fc = PlayerControl.LocalPlayer.Data.FriendCode ?? "";
                    puid = PlayerControl.LocalPlayer.Data.Puid ?? "";
                }

                if (!string.IsNullOrEmpty(room))
                {
                    CurrentRoomCode = room;
                }
                else if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameId == 0)
                {
                    CurrentRoomCode = "";
                }

                if (!string.IsNullOrEmpty(name))
                {
                    LocalPlayerName = name;
                }

                if (id >= 0)
                {
                    LocalPlayerId = id;
                }

                if (!string.IsNullOrEmpty(fc))
                {
                    LocalFriendCode = fc;
                }

                if (!string.IsNullOrEmpty(puid))
                {
                    LocalPuid = puid;
                }

                if (CurrentRoomCode != _lastRoomCode)
                {
                    _lastRoomCode = CurrentRoomCode;
                    _forceRefresh = true;
                }
            }
            catch { }
        }

        public static bool IsHydralumUser(NetworkedPlayerInfo playerInfo)
        {
            if (playerInfo == null) return false;

            // Local player is always running Hydralum
            if (PlayerControl.LocalPlayer != null && playerInfo == PlayerControl.LocalPlayer.Data)
            {
                return true;
            }

            lock (_currentRoomPeers)
            {
                if (!string.IsNullOrEmpty(playerInfo.PlayerName) && _currentRoomPeers.Contains(playerInfo.PlayerName))
                    return true;

                if (!string.IsNullOrEmpty(playerInfo.FriendCode) && _currentRoomPeers.Contains(playerInfo.FriendCode))
                    return true;

                if (!string.IsNullOrEmpty(playerInfo.Puid) && _currentRoomPeers.Contains(playerInfo.Puid))
                    return true;

                if (_currentRoomPeerIds.Contains(playerInfo.PlayerId))
                    return true;
            }

            // Cross-plugin fallback via AppDomain
            try
            {
                if (AppDomain.CurrentDomain.GetData("HydralumPeerNames") is HashSet<string> domainPeers)
                {
                    if (!string.IsNullOrEmpty(playerInfo.PlayerName) && domainPeers.Contains(playerInfo.PlayerName))
                        return true;
                    if (!string.IsNullOrEmpty(playerInfo.FriendCode) && domainPeers.Contains(playerInfo.FriendCode))
                        return true;
                    if (!string.IsNullOrEmpty(playerInfo.Puid) && domainPeers.Contains(playerInfo.Puid))
                        return true;
                }
                if (AppDomain.CurrentDomain.GetData("HydralumPeerIds") is HashSet<byte> domainPeerIds)
                {
                    if (domainPeerIds.Contains(playerInfo.PlayerId))
                        return true;
                }
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
                _ = HttpClient.DeleteAsync($"{FirebaseUrl}/{SessionId}.json");
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
                    string pName = LocalPlayerName;
                    int pId = LocalPlayerId;
                    string friendCode = LocalFriendCode;
                    string puid = LocalPuid;

                    // 1. Send heartbeat
                    var payloadObj = new PresenceNode
                    {
                        last_seen = now,
                        version = MalumMenu.malumVersion,
                        room = roomCode,
                        name = pName,
                        p_id = pId,
                        friend_code = friendCode,
                        puid = puid
                    };

                    string payload = JsonSerializer.Serialize(payloadObj);
                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    {
                        await HttpClient.PutAsync($"{FirebaseUrl}/{SessionId}.json", content, token);
                    }

                    // 2. Fetch active presence nodes
                    string fetchUrl = $"{FirebaseUrl}.json?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                    var response = await HttpClient.GetAsync(fetchUrl, token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(token);
                        if (!string.IsNullOrWhiteSpace(json) && json != "null")
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, PresenceNode>>(json);
                            if (data != null)
                            {
                                int active = 0;
                                var matchedPeers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                var matchedPeerIds = new HashSet<byte>();

                                foreach (var entry in data)
                                {
                                    if (entry.Value != null && (now - entry.Value.last_seen) < 45)
                                    {
                                        active++;

                                        // Match peers in the same lobby
                                        if (!string.IsNullOrEmpty(roomCode) &&
                                            string.Equals(entry.Value.room, roomCode, StringComparison.OrdinalIgnoreCase) &&
                                            entry.Key != SessionId)
                                        {
                                            if (!string.IsNullOrEmpty(entry.Value.name))
                                                matchedPeers.Add(entry.Value.name);
                                            if (!string.IsNullOrEmpty(entry.Value.friend_code))
                                                matchedPeers.Add(entry.Value.friend_code);
                                            if (!string.IsNullOrEmpty(entry.Value.puid))
                                                matchedPeers.Add(entry.Value.puid);
                                            if (entry.Value.p_id >= 0 && entry.Value.p_id <= 255)
                                                matchedPeerIds.Add((byte)entry.Value.p_id);
                                        }
                                    }
                                }

                                lock (_currentRoomPeers)
                                {
                                    _currentRoomPeers.Clear();
                                    foreach (var p in matchedPeers) _currentRoomPeers.Add(p);
                                    _currentRoomPeerIds.Clear();
                                    foreach (var id in matchedPeerIds) _currentRoomPeerIds.Add(id);
                                }

                                AppDomain.CurrentDomain.SetData("HydralumPeerNames", matchedPeers);
                                AppDomain.CurrentDomain.SetData("HydralumPeerIds", matchedPeerIds);

                                OnlineCount = Math.Max(1, active);
                                AppDomain.CurrentDomain.SetData("HydralumOnlineCount", OnlineCount);

                                // Update live summary in Firebase Console
                                var statsPayload = $"{{\"online_players\":{OnlineCount},\"last_updated\":{now}}}";
                                using (var statsContent = new StringContent(statsPayload, Encoding.UTF8, "application/json"))
                                {
                                    await HttpClient.PutAsync("https://hydralum-presence-default-rtdb.firebaseio.com/stats.json", statsContent, token);
                                }
                            }
                        }
                    }

                    // 3. Fetch announcement
                    await AnnouncementManager.RefreshAsync(token);
                }
                catch
                {
                    // Ignore network fluctuations
                }

                // Wait 15 seconds, or wake up sooner if room changed
                for (int i = 0; i < 30; i++)
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

        public class PresenceNode
        {
            public long last_seen { get; set; }
            public string version { get; set; }
            public string room { get; set; }
            public string name { get; set; }
            public int p_id { get; set; } = -1;
            public string friend_code { get; set; }
            public string puid { get; set; }
        }
    }
}
