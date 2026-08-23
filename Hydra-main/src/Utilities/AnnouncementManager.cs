using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HydraMenu
{
    public class AnnouncementData
    {
        public bool enabled { get; set; } = false;
        public string title { get; set; } = "";
        public string message { get; set; } = "";
        public string color { get; set; } = "#00FFAA";
        public string link { get; set; } = "";
        public string linkText { get; set; } = "Open Link";
    }

    public static class AnnouncementManager
    {
        private const string FirebaseUrl = "https://hydralum-presence-default-rtdb.firebaseio.com/announcement.json";
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static bool _dismissed = false;
        private static string _lastDismissedTitle = "";

        public static AnnouncementData Current { get; private set; }

        public static bool ShouldShow()
        {
            var data = Current ?? (AppDomain.CurrentDomain.GetData("HydralumAnnouncement") as AnnouncementData);
            if (data == null || !data.enabled || string.IsNullOrWhiteSpace(data.title))
            {
                return false;
            }
            if (_dismissed && _lastDismissedTitle == data.title)
            {
                return false;
            }
            Current = data;
            return true;
        }

        public static void Dismiss()
        {
            _dismissed = true;
            if (Current != null)
            {
                _lastDismissedTitle = Current.title;
            }
        }

        public static async Task RefreshAsync(CancellationToken token = default)
        {
            try
            {
                var response = await HttpClient.GetAsync(FirebaseUrl, token);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(token);
                    if (!string.IsNullOrWhiteSpace(json) && json != "null")
                    {
                        var data = JsonSerializer.Deserialize<AnnouncementData>(json);
                        if (data != null)
                        {
                            if (Current == null || Current.title != data.title)
                            {
                                _dismissed = false; // Reset dismissal on new announcement
                            }
                            Current = data;
                            AppDomain.CurrentDomain.SetData("HydralumAnnouncement", data);
                        }
                    }
                    else
                    {
                        Current = null;
                        AppDomain.CurrentDomain.SetData("HydralumAnnouncement", null);
                    }
                }
            }
            catch
            {
                // Ignore network errors
            }
        }
    }
}
