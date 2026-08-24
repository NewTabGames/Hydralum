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
    public class ChatMessage
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "Anonymous";
        public string text { get; set; } = "";
        public string room { get; set; } = "";
        public string time { get; set; } = "";
        public long timestamp { get; set; }
        public string version { get; set; } = "1.0.0";
    }

    public static class ChatManager
    {
        private const string FirebaseChatUrl = "https://hydralum-presence-default-rtdb.firebaseio.com/chat/messages";
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static CancellationTokenSource _cts;
        private static bool _started = false;

        private static readonly object _lock = new object();
        private static List<ChatMessage> _messages = new List<ChatMessage>();
        private static int _unreadCount = 0;
        private static long _lastSeenTimestamp = 0;
        private static string _statusMessage = "";
        private static float _statusMessageTimer = 0f;

        public static List<ChatMessage> GetMessages()
        {
            lock (_lock)
            {
                return new List<ChatMessage>(_messages);
            }
        }

        public static int GetUnreadCount()
        {
            return _unreadCount;
        }

        public static void MarkAsRead()
        {
            _unreadCount = 0;
            lock (_lock)
            {
                if (_messages.Count > 0)
                {
                    _lastSeenTimestamp = _messages[_messages.Count - 1].timestamp;
                }
            }
        }

        public static string StatusMessage => _statusMessage;

        public static void Start()
        {
            if (_started) return;

            var alreadyRunning = AppDomain.CurrentDomain.GetData("HydralumChatActive");
            if (alreadyRunning is true)
            {
                _started = true;
                return;
            }

            _started = true;
            AppDomain.CurrentDomain.SetData("HydralumChatActive", true);
            _cts = new CancellationTokenSource();
            Task.Run(() => WorkerLoop(_cts.Token));
        }

        public static void Stop()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
            catch { }
            finally
            {
                _started = false;
                AppDomain.CurrentDomain.SetData("HydralumChatActive", false);
            }
        }

        public static void UpdateMainThread()
        {
            var rawJson = AppDomain.CurrentDomain.GetData("HydralumChatMessagesJson") as string;
            if (!string.IsNullOrEmpty(rawJson))
            {
                try
                {
                    var synced = JsonSerializer.Deserialize<List<ChatMessage>>(rawJson);
                    if (synced != null)
                    {
                        lock (_lock)
                        {
                            if (synced.Count > _messages.Count)
                            {
                                int diff = synced.Count - _messages.Count;
                                _unreadCount += diff;
                            }
                            _messages = synced;
                        }
                    }
                }
                catch { }
            }

            if (_statusMessageTimer > 0f)
            {
                _statusMessageTimer -= Time.deltaTime;
                if (_statusMessageTimer <= 0f)
                {
                    _statusMessage = "";
                }
            }
        }

        public static void IngestMessage(ChatMessage msg)
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.text)) return;

            lock (_lock)
            {
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].timestamp == msg.timestamp &&
                        _messages[i].name == msg.name &&
                        _messages[i].text == msg.text)
                    {
                        return;
                    }
                }

                _messages.Add(msg);
                _messages.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

                if (_messages.Count > 60)
                {
                    _messages.RemoveAt(0);
                }

                if (msg.timestamp > _lastSeenTimestamp)
                {
                    _unreadCount++;
                }
            }

            try
            {
                string listJson = JsonSerializer.Serialize(GetMessages());
                AppDomain.CurrentDomain.SetData("HydralumChatMessagesJson", listJson);
            }
            catch { }
        }

        public static async Task<bool> SendMessageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();
            if (text.Length > 140)
            {
                text = text.Substring(0, 140);
            }

            try
            {
                string pName = PresenceTracker.LocalPlayerName;
                if (string.IsNullOrWhiteSpace(pName)) pName = "Hydralum User";

                string roomCode = PresenceTracker.CurrentRoomCode ?? "";
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string timeStr = GetCentralTimeString();

                var msg = new ChatMessage
                {
                    id = "local_" + Guid.NewGuid().ToString("N"),
                    name = pName,
                    text = text,
                    room = roomCode,
                    time = timeStr,
                    timestamp = now,
                    version = "1.0.0"
                };

                IngestMessage(msg);
                PresenceTracker.BroadcastChatMessage(text);

                _statusMessage = "Message sent!";
                _statusMessageTimer = 3f;
                return true;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Failed to send: {ex.Message}";
                _statusMessageTimer = 4f;
            }

            return false;
        }

        public static void Refresh()
        {
            PresenceTracker.TriggerRefresh();
            _statusMessage = "Refreshed!";
            _statusMessageTimer = 2f;
        }

        public static async Task RefreshMessagesAsync(CancellationToken token = default)
        {
            try
            {
                string url = $"{FirebaseChatUrl}.json?orderBy=\"$key\"&limitToLast=50&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var response = await HttpClient.GetAsync(url, token);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(token);
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, ChatMessage>>(json);
                        if (dict != null)
                        {
                            var list = new List<ChatMessage>();
                            foreach (var kv in dict)
                            {
                                if (kv.Value != null)
                                {
                                    kv.Value.id = kv.Key;
                                    list.Add(kv.Value);
                                }
                            }
                            list.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

                            lock (_lock)
                            {
                                if (list.Count > 0 && _messages.Count > 0)
                                {
                                    long newest = list[list.Count - 1].timestamp;
                                    if (newest > _lastSeenTimestamp)
                                    {
                                        _unreadCount++;
                                    }
                                }
                                _messages = list;
                            }

                            string listJson = JsonSerializer.Serialize(list);
                            AppDomain.CurrentDomain.SetData("HydralumChatMessagesJson", listJson);
                        }
                    }
                }
            }
            catch
            {
                // Ignore transient network errors
            }
        }

        private static async Task WorkerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await RefreshMessagesAsync(token);

                try
                {
                    await Task.Delay(2500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
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
    }
}
