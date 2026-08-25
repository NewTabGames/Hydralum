using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
        private static readonly object _syncLock = new object();

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
            lock (_syncLock)
            {
                var raw = AppDomain.CurrentDomain.GetData("HydralumAnnouncementJson") as string;
                if (raw != _lastSyncedJson)
                {
                    _lastSyncedJson = raw;
                    Current = AnnouncementData.FromJson(raw);
                }
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

            string dismissed = AppDomain.CurrentDomain.GetData("HydralumDismissedAnnouncement") as string;
            if (!string.IsNullOrEmpty(dismissed) && dismissed == $"{data.title}|{data.message}")
            {
                return false;
            }

            return true;
        }

        public static void Dismiss()
        {
            var data = Current;
            if (data != null)
            {
                AppDomain.CurrentDomain.SetData("HydralumDismissedAnnouncement", $"{data.title}|{data.message}");
            }
        }

        public static void Update()
        {
            // Sync any new announcement state from AppDomain on frame update
            SyncFromAppDomain();
        }

        public static void RenderToastGUI()
        {
            try
            {
                if (!ShouldShow()) return;

                var ann = Current;
                if (ann == null) return;

                float width = 360f;
                float textWidth = width - 20f;
                bool hasLink = !string.IsNullOrWhiteSpace(ann.link);

                // Message Body Style
                GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    wordWrap = true,
                    richText = true,
                    alignment = TextAnchor.UpperLeft
                };
                msgStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);

                // Dynamically calculate the required height for the message
                float calculatedTextHeight = msgStyle.CalcHeight(new GUIContent(ann.message ?? ""), textWidth);
                float msgHeight = Mathf.Clamp(calculatedTextHeight, 20f, 250f);

                // Total box height based on header (32px), message height, link button (32px if present), and padding
                float totalHeight = 32f + msgHeight + (hasLink ? 34f : 8f);

                float x = Screen.width - width - 20f;
                float y = Screen.height - totalHeight - 20f;

                // Background box
                GUI.Box(new Rect(x, y, width, totalHeight), GUIContent.none, GUI.skin.box);

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

                // Close 'X' Button
                GUIStyle closeBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                if (GUI.Button(new Rect(x + width - 28f, y + 6f, 20f, 20f), "✕", closeBtnStyle))
                {
                    Dismiss();
                }

                // Message Body
                GUI.Label(new Rect(x + 10f, y + 30f, textWidth, msgHeight + 4f), ann.message ?? "", msgStyle);

                // Action Button
                if (hasLink)
                {
                    string btnText = !string.IsNullOrWhiteSpace(ann.linkText) ? ann.linkText : "Open Link";
                    if (GUI.Button(new Rect(x + 10f, y + 32f + msgHeight + 4f, textWidth, 24f), btnText))
                    {
                        Application.OpenURL(ann.link);
                    }
                }
            }
            catch { }
        }

        public static async Task RefreshAsync(CancellationToken token = default)
        {
            try
            {
                string url = $"{FirebaseUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                using var response = await HttpClient.GetAsync(url, token);
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
