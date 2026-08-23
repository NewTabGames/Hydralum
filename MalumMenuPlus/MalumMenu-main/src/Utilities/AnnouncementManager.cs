using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MalumMenu
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
        private static string _lastNotifiedTitle = "";

        public static float ToastRemainingTime = 0f;
        public const float ToastTotalDuration = 15f;

        public static AnnouncementData Current { get; private set; }

        public static string SanitizeColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return "#00FFAA";
            hex = hex.Trim().Replace("O", "0").Replace("o", "0");
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (hex.Length != 4 && hex.Length != 7 && hex.Length != 9) return "#00FFAA";
            for (int i = 1; i < hex.Length; i++)
            {
                char c = hex[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                {
                    return "#00FFAA";
                }
            }
            return hex;
        }

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
            ToastRemainingTime = 0f;
            if (Current != null)
            {
                _lastDismissedTitle = Current.title;
            }
        }

        public static void Update()
        {
            if (ToastRemainingTime > 0f)
            {
                ToastRemainingTime -= Time.deltaTime;
                if (ToastRemainingTime < 0f) ToastRemainingTime = 0f;
            }
        }

        public static void RenderToastGUI()
        {
            if (ToastRemainingTime <= 0f) return;

            var ann = Current ?? (AppDomain.CurrentDomain.GetData("HydralumAnnouncement") as AnnouncementData);
            if (ann == null || !ann.enabled || string.IsNullOrWhiteSpace(ann.title)) return;

            float width = 340f;
            bool hasLink = !string.IsNullOrWhiteSpace(ann.link);
            float height = hasLink ? 120f : 90f;

            float x = Screen.width - width - 20f;
            float y = Screen.height - height - 20f;

            // Background box
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, GUI.skin.box);

            // Title Style
            string colorHex = SanitizeColor(ann.color);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 10f, y + 6f, width - 45f, 22f), $"<color={colorHex}>📢 {ann.title}</color>", titleStyle);

            // Dismiss Button [✕]
            if (GUI.Button(new Rect(x + width - 30f, y + 6f, 22f, 20f), "✕"))
            {
                Dismiss();
            }

            // Message Body
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };
            msgStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            float msgHeight = hasLink ? 42f : 50f;
            GUI.Label(new Rect(x + 10f, y + 30f, width - 20f, msgHeight), ann.message ?? "", msgStyle);

            // Action Button
            if (hasLink)
            {
                string btnText = !string.IsNullOrWhiteSpace(ann.linkText) ? ann.linkText : "Open Link";
                if (GUI.Button(new Rect(x + 10f, y + 74f, width - 20f, 24f), btnText))
                {
                    Application.OpenURL(ann.link);
                }
            }

            // Time slider progress bar
            float progress = Mathf.Clamp01(ToastRemainingTime / ToastTotalDuration);
            GUI.HorizontalSlider(new Rect(x + 10f, y + height - 12f, width - 20f, 8f), progress, 0f, 1f);
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
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var data = JsonSerializer.Deserialize<AnnouncementData>(json, options);
                        if (data != null)
                        {
                            if (Current == null || Current.title != data.title)
                            {
                                _dismissed = false; // Reset dismissal on new announcement
                            }

                            // Trigger toast notification if this is a fresh announcement
                            if (data.enabled && _lastNotifiedTitle != data.title)
                            {
                                _lastNotifiedTitle = data.title;
                                ToastRemainingTime = ToastTotalDuration;
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
