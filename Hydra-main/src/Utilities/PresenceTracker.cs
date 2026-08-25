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
        public static string LocalFriendCode { get; private set; } = "";

        private static string _lastRoomCode = "";
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
                string friendCode = "";

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

                LocalFriendCode = friendCode ?? "";

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
            if (playerInfo == null || playerInfo.Disconnected) return false;

            // Local player is always running Hydralum
            if (PlayerControl.LocalPlayer != null && playerInfo == PlayerControl.LocalPlayer.Data)
            {
                return true;
            }

            string targetName = playerInfo.PlayerName ?? "";
            int targetId = playerInfo.PlayerId;
            string targetFriendCode = playerInfo.FriendCode ?? "";

            if (string.IsNullOrEmpty(targetFriendCode))
            {
                try
                {
                    var client = AmongUsClient.Instance != null ? AmongUsClient.Instance.GetClientFromPlayerInfo(playerInfo) : null;
                    if (client != null && !string.IsNullOrEmpty(client.FriendCode))
                    {
                        targetFriendCode = client.FriendCode;
                    }
                }
                catch { }
            }

            // Check in-process cache
            lock (_peerLock)
            {
                if (CheckPeerMatch(_currentRoomPeers, targetName, targetId, targetFriendCode))
                    return true;
            }

            // Cross-plugin fallback via AppDomain
            try
            {
                if (AppDomain.CurrentDomain.GetData("HydralumPeersJson") is string peersJson && !string.IsNullOrEmpty(peersJson))
                {
                    var domainPeers = JsonSerializer.Deserialize<List<PeerData>>(peersJson);
                    if (domainPeers != null && CheckPeerMatch(domainPeers, targetName, targetId, targetFriendCode))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool CheckPeerMatch(List<PeerData> peers, string targetName, int targetId, string targetFriendCode)
        {
            if (peers == null || peers.Count == 0) return false;

            foreach (var peer in peers)
            {
                if (peer == null) continue;

                // Primary: Pinpoint match on unique Friend Code if available on both ends
                if (!string.IsNullOrWhiteSpace(peer.FriendCode) && !string.IsNullOrWhiteSpace(targetFriendCode))
                {
                    if (string.Equals(peer.FriendCode.Trim(), targetFriendCode.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Strict Fallback: Both exact Name AND PlayerId must match simultaneously (never name-alone or id-alone)
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
                    string pFriendCode = LocalFriendCode;

                    // 1. Send heartbeat
                    var payloadObj = new PresenceNode
                    {
                        name = pName,
                        room = roomCode,
                        p_id = pId,
                        friend_code = pFriendCode,
                        last_seen = now,
                        last_seen_time = GetCentralTimeString(),
                        versions = new VersionInfo
                        {
                            hydralum = "1.1.5",
                            hydra = "1.9.0",
                            malum = "3.3.0"
                        }
                    };

                    string payload = JsonSerializer.Serialize(payloadObj);
                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    {
                        await HttpClient.PutAsync($"{FirebaseUrl}/{SessionId}.json", content, token);
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
                                    if (entry.Value != null && (now - entry.Value.last_seen) < 30)
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
                                                FriendCode = entry.Value.friend_code ?? ""
                                            });
                                        }
                                    }
                                    else if (entry.Value == null || (now - entry.Value.last_seen) > 45)
                                    {
                                        // Prune stale session from Firebase
                                        _ = HttpClient.DeleteAsync($"{FirebaseUrl}/{entry.Key}.json", token);
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
            public string hydralum { get; set; } = "1.1.5";
            public string hydra { get; set; } = "1.9.0";
            public string malum { get; set; } = "3.3.0";
        }

        public class PeerData
        {
            public string Name { get; set; } = "";
            public int PlayerId { get; set; } = -1;
            public string FriendCode { get; set; } = "";
        }

        public class PresenceNode
        {
            public string name { get; set; } = "";
            public string room { get; set; } = "";
            public int p_id { get; set; } = -1;
            public string friend_code { get; set; } = "";
            public long last_seen { get; set; }
            public string last_seen_time { get; set; } = "";
            public VersionInfo versions { get; set; } = new VersionInfo();
        }
    }
}
