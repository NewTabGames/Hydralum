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

        public static int GetOnlineCount()
        {
            var val = AppDomain.CurrentDomain.GetData("HydralumOnlineCount");
            if (val is int count && count > 0)
            {
                return count;
            }
            return Math.Max(1, OnlineCount);
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

                    // 1. Send heartbeat
                    var payload = $"{{\"last_seen\":{now},\"version\":\"{MalumMenu.malumVersion}\"}}";
                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    {
                        await HttpClient.PutAsync($"{FirebaseUrl}/{SessionId}.json", content, token);
                    }

                    // 2. Fetch active presence nodes
                    var response = await HttpClient.GetAsync($"{FirebaseUrl}.json", token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(token);
                        if (!string.IsNullOrWhiteSpace(json) && json != "null")
                        {
                            var data = JsonSerializer.Deserialize<Dictionary<string, PresenceNode>>(json);
                            if (data != null)
                            {
                                int active = 0;
                                foreach (var entry in data)
                                {
                                    if (entry.Value != null && (now - entry.Value.last_seen) < 20)
                                    {
                                        active++;
                                    }
                                }
                                OnlineCount = Math.Max(1, active);
                                AppDomain.CurrentDomain.SetData("HydralumOnlineCount", OnlineCount);
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

                try
                {
                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private class PresenceNode
        {
            public long last_seen { get; set; }
            public string version { get; set; }
        }
    }
}
