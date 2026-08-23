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
            Current = data;
            return true;
        }

        public static void Update()
        {
            // Persistent announcement - controlled purely via Firebase toggle
        }

        public static void RenderToastGUI()
        {
            if (!ShouldShow()) return;

            var ann = Current;
            if (ann == null) return;

            float width = 340f;
            bool hasLink = !string.IsNullOrWhiteSpace(ann.link);
            float height = hasLink ? 115f : 85f;

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
            GUI.Label(new Rect(x + 10f, y + 6f, width - 20f, 22f), $"<color={colorHex}>📢 {ann.title}</color>", titleStyle);

            // Message Body
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft
            };
            msgStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            float msgHeight = hasLink ? 45f : 50f;
            GUI.Label(new Rect(x + 10f, y + 30f, width - 20f, msgHeight), ann.message ?? "", msgStyle);

            // Action Button
            if (hasLink)
            {
                string btnText = !string.IsNullOrWhiteSpace(ann.linkText) ? ann.linkText : "Open Link";
                if (GUI.Button(new Rect(x + 10f, y + 78f, width - 20f, 24f), btnText))
                {
                    Application.OpenURL(ann.link);
                }
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
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var data = JsonSerializer.Deserialize<AnnouncementData>(json, options);
                        if (data != null)
                        {
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
