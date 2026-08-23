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

        public static AnnouncementData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                var data = new AnnouncementData();

                if (root.TryGetProperty("enabled", out var propEnabled))
                {
                    if (propEnabled.ValueKind == JsonValueKind.True) data.enabled = true;
                    else if (propEnabled.ValueKind == JsonValueKind.False) data.enabled = false;
                    else if (propEnabled.ValueKind == JsonValueKind.String)
                        data.enabled = bool.TryParse(propEnabled.GetString(), out var b) && b;
                    else if (propEnabled.ValueKind == JsonValueKind.Number)
                        data.enabled = propEnabled.GetInt32() != 0;
                }

                if (root.TryGetProperty("title", out var propTitle))
                    data.title = propTitle.ToString();

                if (root.TryGetProperty("message", out var propMsg))
                    data.message = propMsg.ToString();

                if (root.TryGetProperty("color", out var propColor))
                    data.color = propColor.ToString();

                if (root.TryGetProperty("link", out var propLink))
                {
                    string l = propLink.ToString();
                    if (!string.IsNullOrWhiteSpace(l) && l != "Value") data.link = l;
                }

                if (root.TryGetProperty("linkText", out var propLinkText))
                {
                    string lt = propLinkText.ToString();
                    if (!string.IsNullOrWhiteSpace(lt) && lt != "Value") data.linkText = lt;
                }

                return data;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class AnnouncementManager
    {
        private const string FirebaseUrl = "https://hydralum-presence-default-rtdb.firebaseio.com/announcement.json";
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private static string _lastSyncedJson = null;

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

        public static void SyncFromAppDomain()
        {
            var raw = AppDomain.CurrentDomain.GetData("HydralumAnnouncementJson") as string;
            if (raw != _lastSyncedJson)
            {
                _lastSyncedJson = raw;
                Current = AnnouncementData.FromJson(raw);
            }
        }

        public static bool ShouldShow()
        {
            SyncFromAppDomain();
            var data = Current;
            if (data == null || !data.enabled || string.IsNullOrWhiteSpace(data.title))
            {
                return false;
            }
            return true;
        }

        public static void Update()
        {
            // Sync any new announcement state from AppDomain on frame update
            SyncFromAppDomain();
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
                    AppDomain.CurrentDomain.SetData("HydralumAnnouncementJson", json);
                    SyncFromAppDomain();
                }
            }
            catch
            {
                // Ignore network errors
            }
        }
    }
}
