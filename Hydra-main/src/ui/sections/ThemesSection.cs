using System;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class ThemesSection : Section
	{
		public ThemesSection() : base("Themes") { }
		private static float _pendingUiScale = 0f;

		private const float ButtonGap = 8f;

		// Custom-color type-in state. GUILayout.TextField is unstripped and crashes under IL2CPP (see
		// MenuSection), so we capture typed characters ourselves. Only one field is active at a time.
		private enum HexField { None, Solid, GradA, GradB, Name }
		private static HexField _typing = HexField.None;
		private static string _solidInput = "";
		private static string _gradAInput = "";
		private static string _gradBInput = "";
		private static string _nameInput = "";

		// Saved custom themes come from the shared CThemes folder (see CustomThemeStore), so a theme saved
		// in Malum shows up here too. Cache the disk listing and refresh at most once a second so drawing
		// this section every frame doesn't hit disk each time.
		private static System.Collections.Generic.List<(string name, string value)> _savedCache = new System.Collections.Generic.List<(string name, string value)>();
		private static float _savedCacheUntil = 0f;

		private static void RefreshSaved(bool force = false)
		{
			if (!force && Time.unscaledTime < _savedCacheUntil) return;
			_savedCache = CustomThemeStore.LoadAll();
			_savedCacheUntil = Time.unscaledTime + 1f;
		}

		private static readonly (string name, string hex)[] Themes =
		{
			("Default", ""),
			("Malum", "#8A2BE2"),
			("Ocean", "#1E90FF"),
			("Emerald", "#2ECC71"),
			("Crimson", "#E74C3C"),
			("Sunset", "#FF8C42"),
			("Gold", "#FFC107"),
			("Bubblegum", "#FF6FB5"),
		};

		private static readonly (string name, string a, string b)[] Gradients =
		{
			("Fire", "#FF4E00", "#FFC400"),
			("Aurora", "#00C9FF", "#92FE9D"),
			("Galaxy", "#7F00FF", "#E100FF"),
			("Ocean", "#2E3192", "#1BFFFF"),
			("Sunset", "#FF5F6D", "#FFC371"),
			("Mint", "#11998E", "#38EF7D"),
			("Cyberpunk", "#FF007F", "#00F0FF"),
			("Vaporwave", "#FF71CE", "#01CDFE"),
			("Solar Flare", "#FF0844", "#FFB199"),
			("Matrix", "#00FF87", "#60EFFF"),
			("Midnight", "#0F2027", "#2C5364"),
			("Amethyst", "#8E2DE2", "#4A00E0"),
			("Blood Orange", "#F12711", "#F5AF19"),
			("Neon Lime", "#F9D423", "#A8FF78"),
			("Lavender", "#A18CD1", "#FBC2EB"),
			("Iceberg", "#56CCF2", "#2F80ED"),
			("Sakura", "#EE9CA7", "#FFDDE1"),
			("Synthwave", "#833AB4", "#FD1D1D"),
			("Cosmic", "#3A1C71", "#D76D77"),
			("Emerald Forest", "#0BA360", "#3CBA92"),
			("Electric Rose", "#F857A6", "#FF5858"),
			("Gold Mirage", "#FFE259", "#FFA751"),
			("Abyss", "#000428", "#004E92"),
			("Tropical", "#00F260", "#0575E6"),
		};

		public override void Render()
		{
			GUILayout.Label($"Menu Opacity: {Styles.menuOpacity * 100:F0}%");
			float newOpacity = (float)Math.Round(GUILayout.HorizontalSlider(Styles.menuOpacity, 0, 1), 4);
			if (newOpacity != Styles.menuOpacity)
			{
				Styles.menuOpacity = newOpacity;
				Styles.ClearCache();
			}

			if (GUIUtility.hotControl == 0)
			{
				if (Math.Abs(MainUI.scale - _pendingUiScale) > 0.001f && _pendingUiScale != 0f)
				{
					MainUI.scale = (float)Math.Round(_pendingUiScale, 2);
					var config = Hydra.mainUI.GetConfigData();
					config.UiScale = MainUI.scale;
					Hydra.mainUI.LoadConfigData(config);
				}
				else
				{
					_pendingUiScale = MainUI.scale;
				}
			}

			GUILayout.Label($"UI Scale: {_pendingUiScale:F2}x");
			_pendingUiScale = GUILayout.HorizontalSlider(_pendingUiScale, 0.5f, 2.0f);

			GUILayout.Space(12);

			GUILayout.Label("RGB Mode");
			DrawRgbButton();

			GUILayout.Space(12);
			GUILayout.Label("Custom Color");
			DrawCustom();

			GUILayout.Space(12);
			GUILayout.Label("Solid Themes");
			DrawSolidThemes();

			GUILayout.Space(14);
			GUILayout.Label($"Gradients ({Gradients.Length})");
			DrawGradients();
		}

		// Custom hex + custom gradient entry. Applying persists to the current config immediately (via
		// ApplyTheme/ApplyGradient below), so typed colors survive a restart.
		private void DrawCustom()
		{
			if (_typing != HexField.None) CaptureHex();

			float h = 30 * MainUI.scale;

			// --- Solid custom color: [ #hex field ] [swatch] [Apply] ---
			GUILayout.BeginHorizontal();
			string solidLabel = _typing == HexField.Solid
				? $"<color=yellow>{(string.IsNullOrEmpty(_solidInput) ? "#RRGGBB" : _solidInput)}_</color>"
				: (string.IsNullOrEmpty(_solidInput) ? "Hex: click to type" : $"Hex: {_solidInput}");
			if (GUILayout.Button(solidLabel, GUILayout.ExpandWidth(true), GUILayout.Height(h)))
				SetTyping(HexField.Solid);

			bool solidOk = NormalizeHex(_solidInput, out var solidColor, out var solidNorm);
			var prevBg = GUI.backgroundColor;
			if (solidOk) GUI.backgroundColor = solidColor; // Apply button previews the chosen color
			if (GUILayout.Button("Apply", GUILayout.Width(70 * MainUI.scale), GUILayout.Height(h)))
			{
				if (solidOk)
				{
					ApplyTheme(solidNorm);
					Hydra.notifications.Send("Themes", $"Applied {solidNorm}");
				}
				else Hydra.notifications.Send("Themes", "Invalid hex color.");
			}
			GUI.backgroundColor = prevBg;
			GUILayout.EndHorizontal();

			// --- Custom gradient: [ Start ] [ End ] on one row, preview + Apply on the next ---
			GUILayout.Space(6);
			GUILayout.Label("Custom Gradient");

			GUILayout.BeginHorizontal();
			string aLabel = _typing == HexField.GradA
				? $"<color=yellow>{(string.IsNullOrEmpty(_gradAInput) ? "#Start" : _gradAInput)}_</color>"
				: (string.IsNullOrEmpty(_gradAInput) ? "Start: click" : $"Start: {_gradAInput}");
			if (GUILayout.Button(aLabel, GUILayout.ExpandWidth(true), GUILayout.Height(h)))
				SetTyping(HexField.GradA);

			string bLabel = _typing == HexField.GradB
				? $"<color=yellow>{(string.IsNullOrEmpty(_gradBInput) ? "#End" : _gradBInput)}_</color>"
				: (string.IsNullOrEmpty(_gradBInput) ? "End: click" : $"End: {_gradBInput}");
			if (GUILayout.Button(bLabel, GUILayout.ExpandWidth(true), GUILayout.Height(h)))
				SetTyping(HexField.GradB);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			bool aOk = NormalizeHex(_gradAInput, out var ga, out var gaNorm);
			bool bOk = NormalizeHex(_gradBInput, out var gb, out var gbNorm);
			var prevBgG = GUI.backgroundColor;
			if (aOk && bOk)
			{
				float wave = (Mathf.Sin(Time.time * 2.2f) + 1f) * 0.5f;
				GUI.backgroundColor = Color.Lerp(ga, gb, wave); // Apply Gradient button animates the gradient
			}
			if (GUILayout.Button("Apply Gradient", GUILayout.ExpandWidth(true), GUILayout.Height(h)))
			{
				if (aOk && bOk)
				{
					ApplyGradient(gaNorm, gbNorm);
					Hydra.notifications.Send("Themes", "Applied custom gradient");
				}
				else Hydra.notifications.Send("Themes", "Enter two valid hex colors.");
			}
			GUI.backgroundColor = prevBgG;
			GUILayout.EndHorizontal();

			// --- Save current theme to the shared CThemes folder: [ Name field ] [Save] ---
			GUILayout.Space(10);
			GUILayout.Label("Saved Themes <size=10><color=#888888>(shared with Malum)</color></size>");

			GUILayout.BeginHorizontal();
			string nameLabel = _typing == HexField.Name
				? $"<color=yellow>{(string.IsNullOrEmpty(_nameInput) ? "Name..." : _nameInput)}_</color>"
				: (string.IsNullOrEmpty(_nameInput) ? "Name: click to type" : $"Name: {_nameInput}");
			if (GUILayout.Button(nameLabel, GUILayout.ExpandWidth(true), GUILayout.Height(h)))
				SetTyping(HexField.Name);
			if (GUILayout.Button("Save", GUILayout.Width(70 * MainUI.scale), GUILayout.Height(h)))
				SaveCurrentTheme();
			GUILayout.EndHorizontal();

			DrawSavedList(h);
		}

		// The current accent, as stored in ThemeColor: "#RRGGBB", "grad:#a,#b", or "" for default.
		private static string CurrentThemeValue() => Hydra.mainUI.GetConfigData()?.ThemeColor ?? "";

		private void SaveCurrentTheme()
		{
			if (Hydra.mainUI.GetConfigData()?.RgbMode ?? false)
			{
				Hydra.notifications.Send("Themes", "Turn off RGB Mode first.");
				return;
			}
			string value = CurrentThemeValue();
			if (string.IsNullOrEmpty(value))
			{
				Hydra.notifications.Send("Themes", "Apply a color first.");
				return;
			}
			string name = CustomThemeStore.Sanitize(_nameInput);
			if (name == null)
			{
				Hydra.notifications.Send("Themes", "Enter a name.");
				return;
			}

			if (CustomThemeStore.Save(name, value))
			{
				Hydra.notifications.Send("Themes", $"Saved '{name}'.");
				_nameInput = "";
				_typing = HexField.None;
				RefreshSaved(true);
			}
			else Hydra.notifications.Send("Themes", "Save failed.");
		}

		// Two-column list of saved themes: click the name to apply, x to delete. Colors preview the value.
		private void DrawSavedList(float h)
		{
			RefreshSaved();
			if (_savedCache.Count == 0)
			{
				GUILayout.Label("<size=11><color=#888888>None yet - apply a color, name it, then Save.</color></size>");
				return;
			}

			for (int i = 0; i < _savedCache.Count; i += 2)
			{
				GUILayout.BeginHorizontal();
				DrawSavedEntry(_savedCache[i], h);
				if (i + 1 < _savedCache.Count)
				{
					GUILayout.Space(ButtonGap);
					DrawSavedEntry(_savedCache[i + 1], h);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(4);
			}
		}

		private void DrawSavedEntry((string name, string value) theme, float h)
		{
			var prev = GUI.backgroundColor;
			GUI.backgroundColor = PreviewColor(theme.value);
			if (GUILayout.Button(theme.name, GUILayout.ExpandWidth(true), GUILayout.Height(h)))
			{
				ApplySavedValue(theme.value);
				Hydra.notifications.Send("Themes", $"Applied '{theme.name}'.");
			}
			GUI.backgroundColor = prev;

			if (GUILayout.Button("x", GUILayout.Width(26 * MainUI.scale), GUILayout.Height(h)))
			{
				CustomThemeStore.Delete(theme.name);
				Hydra.notifications.Send("Themes", $"Deleted '{theme.name}'.");
				RefreshSaved(true);
			}
		}

		// Applies a stored value (hex or "grad:#a,#b") via the same paths the preset buttons use.
		private void ApplySavedValue(string value)
		{
			if (!string.IsNullOrEmpty(value) && value.StartsWith("grad:"))
			{
				var parts = value.Substring(5).Split(',');
				if (parts.Length == 2) { ApplyGradient(parts[0], parts[1]); return; }
			}
			ApplyTheme(value);
		}

		// Static preview color for a stored value (animated for gradients).
		private static Color PreviewColor(string value)
		{
			if (string.IsNullOrEmpty(value)) return Color.white;
			if (value.StartsWith("grad:"))
			{
				var parts = value.Substring(5).Split(',');
				if (parts.Length == 2
					&& ColorUtility.TryParseHtmlString(parts[0], out var a)
					&& ColorUtility.TryParseHtmlString(parts[1], out var b))
				{
					float wave = (Mathf.Sin(Time.time * 2.2f) + 1f) * 0.5f;
					return Color.Lerp(a, b, wave);
				}
				return Color.white;
			}
			return ColorUtility.TryParseHtmlString(value, out var c) ? c : Color.white;
		}

		private static void SetTyping(HexField field)
		{
			_typing = _typing == field ? HexField.None : field;
		}

		// Adds '#' if missing and validates.
		private static bool NormalizeHex(string raw, out Color color, out string normalized)
		{
			color = Color.white;
			normalized = null;
			if (string.IsNullOrWhiteSpace(raw)) return false;
			string s = raw.Trim();
			if (!s.StartsWith("#")) s = "#" + s;
			if (ColorUtility.TryParseHtmlString(s, out color)) { normalized = s; return true; }
			return false;
		}

		// Manual hex-key capture (IL2CPP-safe). Accepts 0-9 a-f A-F and '#'; caps at 9 chars (#RRGGBBAA).
		private void CaptureHex()
		{
			var e = Event.current;
			if (e == null || e.type != EventType.KeyDown) return;

			if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
			{
				_typing = HexField.None;
				e.Use();
				return;
			}

			if (e.keyCode == KeyCode.Backspace)
			{
				ref string buf = ref ActiveBuffer();
				if (!string.IsNullOrEmpty(buf)) buf = buf.Substring(0, buf.Length - 1);
				e.Use();
				return;
			}

			char c = e.character;
			if (_typing == HexField.Name)
			{
				if (c != ' ' && !char.IsControl(c))
				{
					ref string nb = ref ActiveBuffer();
					if ((nb?.Length ?? 0) < 24) { nb += c; e.Use(); }
				}
				return;
			}
			bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == '#';
			if (isHex)
			{
				ref string buf = ref ActiveBuffer();
				if ((buf?.Length ?? 0) < 9) { buf += c; e.Use(); }
			}
		}

		private ref string ActiveBuffer()
		{
			switch (_typing)
			{
				case HexField.GradA: return ref _gradAInput;
				case HexField.GradB: return ref _gradBInput;
				case HexField.Name: return ref _nameInput;
				default: return ref _solidInput;
			}
		}

		private void DrawRgbButton()
		{
			var previous = GUI.backgroundColor;
			GUI.backgroundColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f, 1f), 1f, 1f);

			if (GUILayout.Button("RGB Mode (Animated Rainbow)", GUILayout.Height(32 * MainUI.scale)))
			{
				var config = Hydra.mainUI.GetConfigData();
				config.RgbMode = true;
				Hydra.mainUI.LoadConfigData(config);
				PersistTheme();
			}

			GUI.backgroundColor = previous;
		}

		private void DrawSolidThemes()
		{
			for (var i = 0; i < Themes.Length; i += 3)
			{
				GUILayout.BeginHorizontal();
				ThemeButton(Themes[i]);
				if (i + 1 < Themes.Length)
				{
					GUILayout.Space(ButtonGap);
					ThemeButton(Themes[i + 1]);
				}
				if (i + 2 < Themes.Length)
				{
					GUILayout.Space(ButtonGap);
					ThemeButton(Themes[i + 2]);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(4);
			}
		}

		private void DrawGradients()
		{
			for (var i = 0; i < Gradients.Length; i += 3)
			{
				GUILayout.BeginHorizontal();
				float t1 = (Mathf.Sin(Time.time * 2.2f + (i * 0.4f)) + 1f) * 0.5f;
				GradientButton(Gradients[i], t1);

				if (i + 1 < Gradients.Length)
				{
					GUILayout.Space(ButtonGap);
					float t2 = (Mathf.Sin(Time.time * 2.2f + ((i + 1) * 0.4f)) + 1f) * 0.5f;
					GradientButton(Gradients[i + 1], t2);
				}

				if (i + 2 < Gradients.Length)
				{
					GUILayout.Space(ButtonGap);
					float t3 = (Mathf.Sin(Time.time * 2.2f + ((i + 2) * 0.4f)) + 1f) * 0.5f;
					GradientButton(Gradients[i + 2], t3);
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(4);
			}
		}

		private void ThemeButton((string name, string hex) theme)
		{
			var previous = GUI.backgroundColor;
			if (!string.IsNullOrEmpty(theme.hex) && ColorUtility.TryParseHtmlString(theme.hex, out var swatch))
				GUI.backgroundColor = swatch;
			else if (string.IsNullOrEmpty(theme.hex))
				GUI.backgroundColor = Styles.ColorValues.ContainsKey(Styles.primaryColor) ? Styles.ColorValues[Styles.primaryColor] : new Color(0.0f, 0.50f, 1f);

			if (GUILayout.Button(theme.name, GUILayout.ExpandWidth(true), GUILayout.Height(30 * MainUI.scale)))
				ApplyTheme(theme.hex);

			GUI.backgroundColor = previous;
		}

		private void GradientButton((string name, string a, string b) grad, float t)
		{
			var previous = GUI.backgroundColor;
			if (ColorUtility.TryParseHtmlString(grad.a, out var ca) && ColorUtility.TryParseHtmlString(grad.b, out var cb))
				GUI.backgroundColor = Color.Lerp(ca, cb, t);

			if (GUILayout.Button(grad.name, GUILayout.ExpandWidth(true), GUILayout.Height(30 * MainUI.scale)))
				ApplyGradient(grad.a, grad.b);

			GUI.backgroundColor = previous;
		}

		private void ApplyTheme(string hex)
		{
			var config = Hydra.mainUI.GetConfigData();
			config.RgbMode = false;
			config.ThemeColor = hex;
			Hydra.mainUI.LoadConfigData(config);
			PersistTheme();
		}

		private void ApplyGradient(string hexA, string hexB)
		{
			var config = Hydra.mainUI.GetConfigData();
			config.RgbMode = false;
			config.ThemeColor = $"grad:{hexA},{hexB}";
			Hydra.mainUI.LoadConfigData(config);
			PersistTheme();
		}

		// LoadConfigData only updates the in-memory menu state, so on its own a theme choice is lost on
		// restart unless the user manually hits Save. Writing the current config here makes every theme
		// pick (preset or custom) persist automatically, which is what "saveable" colors need.
		private static void PersistTheme()
		{
			try { Hydra.config.SaveConfig(Hydra.config.currentConfig); } catch { }
		}
	}
}
