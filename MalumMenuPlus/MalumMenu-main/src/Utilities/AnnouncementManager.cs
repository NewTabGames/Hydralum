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
            float height = 110f;
            if (!string.IsNullOrEmpty(ann.link)) height += 30f;

            float x = Screen.width - width - 20f;
            float y = 20f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, GUI.skin.window);

            GUILayout.BeginArea(new Rect(x + 10f, y + 8f, width - 20f, height - 16f));

            GUILayout.BeginHorizontal();
            string titleColor = !string.IsNullOrEmpty(ann.color) ? ann.color : "#00FFAA";
            GUILayout.Label($"<b><color={titleColor}>📢 {ann.title}</color></b>");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
            {
                Dismiss();
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(ann.message))
            {
                GUILayout.Label($"<size=12>{ann.message}</size>");
            }

            if (!string.IsNullOrEmpty(ann.link))
            {
                GUILayout.Space(2);
                string btnText = !string.IsNullOrEmpty(ann.linkText) ? ann.linkText : "Open Link";
                if (GUILayout.Button(btnText, GUILayout.Height(24)))
                {
                    Application.OpenURL(ann.link);
                }
            }

            // Time remaining progress bar
            GUILayout.FlexibleSpace();
            float progress = Mathf.Clamp01(ToastRemainingTime / ToastTotalDuration);
            Rect progressRect = GUILayoutUtility.GetRect(width - 20f, 4f);
            GUI.color = new Color(0f, 1f, 0.6f, 0.7f);
            GUI.DrawTexture(new Rect(progressRect.x, progressRect.y, progressRect.width * progress, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.EndArea();
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
