using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HydraMenu
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

        // Called on Unity Main Thread (e.g. MainUI.Update)
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

                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
                {
                    name = PlayerControl.LocalPlayer.Data.PlayerName ?? "";
                    id = PlayerControl.LocalPlayer.PlayerId;
                }

                if (string.IsNullOrEmpty(name))
                {
                    try { name = AmongUs.Data.DataManager.Player.Customization.Name ?? ""; } catch { }
                }

                if (string.IsNullOrEmpty(name))
                {
                    try { name = PlayerPrefs.GetString("PlayerName", ""); } catch { }
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
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{FirebaseUrl}/{SessionId}.json");
                HttpClient.Send(request);
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

                    // 1. Send heartbeat
                    var payloadObj = new PresenceNode
                    {
                        name = pName,
                        room = roomCode,
                        p_id = pId,
                        last_seen = now,
                        last_seen_time = GetCentralTimeString(),
                        versions = new VersionInfo
                        {
                            hydralum = "1.0.0",
                            hydra = "1.9.0",
                            malum = "3.3.0"
                        },
                        chat_msg = LocalChatMsg,
                        chat_time = LocalChatTime,
                        chat_ts = LocalChatTs
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

                                        // Ingest peer chat messages
                                        if (!string.IsNullOrEmpty(entry.Value.chat_msg) && entry.Value.chat_ts > 0)
                                        {
                                            ChatManager.IngestMessage(new ChatMessage
                                            {
                                                id = entry.Key,
                                                name = entry.Value.name,
                                                text = entry.Value.chat_msg,
                                                room = entry.Value.room,
                                                time = entry.Value.chat_time,
                                                timestamp = entry.Value.chat_ts,
                                                version = entry.Value.versions?.hydralum ?? "1.0.0"
                                            });
                                        }

                                        // Match peers in the same lobby
                                        if (!string.IsNullOrEmpty(roomCode) &&
                                            string.Equals(entry.Value.room, roomCode, StringComparison.OrdinalIgnoreCase) &&
                                            entry.Key != SessionId)
                                        {
                                            if (!string.IsNullOrEmpty(entry.Value.name))
                                                matchedPeers.Add(entry.Value.name);
                                            if (entry.Value.p_id >= 0 && entry.Value.p_id <= 255)
                                                matchedPeerIds.Add((byte)entry.Value.p_id);
                                        }
                                    }
                                    else if (entry.Value == null || (now - entry.Value.last_seen) > 60)
                                    {
                                        // Prune stale session from Firebase
                                        _ = HttpClient.DeleteAsync($"{FirebaseUrl}/{entry.Key}.json", token);
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

        public static string LocalChatMsg { get; set; } = "";
        public static string LocalChatTime { get; set; } = "";
        public static long LocalChatTs { get; set; } = 0;

        public static void BroadcastChatMessage(string message)
        {
            LocalChatMsg = message;
            LocalChatTime = GetCentralTimeString();
            LocalChatTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _forceRefresh = true;
        }

        public static void TriggerRefresh()
        {
            _forceRefresh = true;
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
            public string hydralum { get; set; } = "1.0.0";
            public string hydra { get; set; } = "1.9.0";
            public string malum { get; set; } = "3.3.0";
        }

        public class PresenceNode
        {
            public string name { get; set; }
            public string room { get; set; }
            public int p_id { get; set; } = -1;
            public long last_seen { get; set; }
            public string last_seen_time { get; set; }
            public VersionInfo versions { get; set; } = new VersionInfo();
            public string chat_msg { get; set; } = "";
            public string chat_time { get; set; } = "";
            public long chat_ts { get; set; } = 0;
        }
    }
}
