using HydraMenu.anticheat;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class BlacklistSection : Section
	{
		public BlacklistSection() : base("Blacklist") { }

		private static string StripTags(string s) => string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, "<.*?>", string.Empty);

		// Which list is shown. Both lists used to stack (long scroll); now only one shows at a time.
		private bool showFlaggedView;
		private static GUIStyle _badgeStyle;

		public override void Render()
		{
			var players = PlayerControl.AllPlayerControls;
			var entries = Blacklist.Entries;

			int onlineCount = 0;
			if (players != null)
			{
				for (int i = 0; i < players.Count; i++)
				{
					var pc = players[i];
					// Exclude yourself - you can't flag yourself, so don't count/list your own row.
					if (pc != null && pc.Data != null && !pc.Data.Disconnected && pc != PlayerControl.LocalPlayer) onlineCount++;
				}
			}

			// Title + Auto-Flag on one row.
			GUILayout.BeginHorizontal();
			GUILayout.Label("<b>Blacklist</b>");
			GUILayout.FlexibleSpace();
			bool auto = GUILayout.Toggle(Blacklist.AutoDetect, "Auto-Flag");
			if (auto != Blacklist.AutoDetect)
			{
				Blacklist.AutoDetect = auto;
				Blacklist.Save();
			}
			GUILayout.EndHorizontal();

			// Segmented switch: pick a list instead of showing both at once.
			GUILayout.BeginHorizontal();
			bool pickOnline = GUILayout.Toggle(!showFlaggedView, $"Online ({onlineCount})", GUI.skin.button);
			bool pickFlagged = GUILayout.Toggle(showFlaggedView, $"Flagged ({entries.Count})", GUI.skin.button);
			if (pickOnline && showFlaggedView) showFlaggedView = false;        // clicked the Online tab
			else if (pickFlagged && !showFlaggedView) showFlaggedView = true;  // clicked the Flagged tab
			GUILayout.EndHorizontal();

			GUILayout.Space(3);

			if (!showFlaggedView)
			{
				if (onlineCount == 0)
				{
					GUILayout.Label("<color=#888888>No online players.</color>");
				}
				else
				{
					for (int i = 0; i < players.Count; i++)
					{
						var p = players[i];
						if (p == null || p.Data == null || p.Data.Disconnected) continue;
						if (p == PlayerControl.LocalPlayer) continue; // can't flag yourself

						bool flagged = Blacklist.IsBlacklisted(p);
						string nm = StripTags(p.Data.PlayerName);

						GUILayout.BeginHorizontal();
						GUILayout.Label((flagged ? "<color=#FF3B3B>[Flagged]</color> " : "") + nm);
						GUILayout.FlexibleSpace();
						if (GUILayout.Button(flagged ? "Unflag" : "Flag", GUILayout.Width(70)))
						{
							if (flagged) Blacklist.RemovePlayer(p);
							else if (!Blacklist.AddManual(p))
								Hydra.notifications.Send("Blacklist", "Can't flag this player - no PUID or friend code available.", 5);
						}
						GUILayout.EndHorizontal();
					}
				}
			}
			else
			{
				if (entries.Count == 0)
				{
					GUILayout.Label("<color=#888888>Nothing flagged.</color>");
				}
				else
				{
					// Snapshot so a Remove click doesn't mutate the list we're iterating this frame.
					if (_badgeStyle == null)
						_badgeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, richText = true };

					foreach (var e in entries.ToList())
					{
						string shownName = string.IsNullOrEmpty(e.Name) ? "(unknown)" : StripTags(e.Name);
						// A clean right-aligned badge: amber "Auto" for auto-flags, blue "Manual" for manual ones.
						string badge = e.Auto
							? "<color=#E0A62E>Auto</color>"
							: "<color=#7FB0E8>Manual</color>";

						GUILayout.BeginHorizontal();
						GUILayout.Label(shownName);
						GUILayout.FlexibleSpace();
						GUILayout.Label(badge, _badgeStyle, GUILayout.Width(58));
						if (GUILayout.Button("Remove", GUILayout.Width(70)))
						{
							Blacklist.Remove(e);
						}
						GUILayout.EndHorizontal();
					}
				}

				GUILayout.Space(5);
				if (GUILayout.Button("Open Blacklist Folder"))
				{
					try { Process.Start("explorer.exe", Blacklist.FOLDER_PATH); } catch { }
				}
			}
		}
	}
}
