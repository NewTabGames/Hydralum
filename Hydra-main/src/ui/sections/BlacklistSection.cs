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

		public override void Render()
		{
			// Wrapping style so the line fits inside the narrow feature pane instead of being clipped.
			GUIStyle wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };

			GUILayout.Label("<b>Blacklist</b>");
			GUILayout.Label("Flagged players get a red \"Flagged\" tag above their name.", wrap);

			bool auto = GUILayout.Toggle(Blacklist.AutoDetect, "Auto-Flag");
			if (auto != Blacklist.AutoDetect)
			{
				Blacklist.AutoDetect = auto;
				Blacklist.Save();
			}

			GUILayout.Space(5);
			GUILayout.Label("Online Players:");

			var players = PlayerControl.AllPlayerControls;
			if (players == null || players.Count == 0)
			{
				GUILayout.Label("<color=#888888>No online players.</color>");
			}
			else
			{
				for (int i = 0; i < players.Count; i++)
				{
					var p = players[i];
					if (p == null || p.Data == null || p.Data.Disconnected) continue;

					bool flagged = Blacklist.IsBlacklisted(p);

					GUILayout.BeginHorizontal();
					GUILayout.Label((flagged ? "<color=#FF3B3B>[Flagged]</color> " : "") + StripTags(p.Data.PlayerName));
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(flagged ? "Unflag" : "Flag", GUILayout.Width(80)))
					{
						if (flagged) Blacklist.RemovePlayer(p);
						else if (!Blacklist.AddManual(p))
							Hydra.notifications.Send("Blacklist", "Can't flag this player - no PUID or friend code available.", 5);
					}
					GUILayout.EndHorizontal();
				}
			}

			GUILayout.Space(8);
			var entries = Blacklist.Entries;
			GUILayout.Label($"Blacklist ({entries.Count}):");

			if (entries.Count == 0)
			{
				GUILayout.Label("<color=#888888>Empty.</color>");
			}
			else
			{
				// Snapshot so a Remove click doesn't mutate the list we're iterating this frame.
				foreach (var e in entries.ToList())
				{
					string idKind = !string.IsNullOrEmpty(e.Puid) ? "PUID" : "FC";
					string shownName = string.IsNullOrEmpty(e.Name) ? "(unknown)" : StripTags(e.Name);

					GUILayout.BeginHorizontal();
					GUILayout.Label($"{shownName} <color=#888888>[{idKind}]{(e.Auto ? " auto" : "")}</color>");
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Remove", GUILayout.Width(80)))
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
