using HydraMenu.ui.sections;
using System;
using UnityEngine;

namespace HydraMenu.ui
{
	public class MainUI : MonoBehaviour
	{
		// Current window
		public KeyCode menuKey = KeyCode.Insert;
		public bool visible = false;
		public static float scale = 1.0f;

		private bool isDragging = false;
		private Vector2 mouseDelta = new Vector2();

		public static Vector2 windowPosition = new Vector2(250, 100);
		public static Vector2 WindowSize
		{
			get { return new Vector2(500, 470) * scale; }
		}

		// UI Header
		public static Vector2 HeaderSize
		{
			get { return new Vector2(WindowSize.x, 20 * scale); }
		}

		public static Vector2 HeaderPosition
		{
			get { return new Vector2(windowPosition.x, windowPosition.y); }
		}

		// UI Section Pane
		private readonly Section[] sections = { new GeneralSection(), new SelfSection(), new TrollSection(), new HostSection(), new RolesSection(), new PlayersSection(), new VisualSection(), new ProtectionsSection(), new AnticheatSection(), new SpooferSection(), new ThemesSection(), new MenuSection(), new InfoSection() };
		public byte activeTab = 0;

		public static Vector2 SectionListSize
		{
			get { return new Vector2(100 * scale, WindowSize.y - HeaderSize.y); }
		}

		public static Vector2 SectionListPosition
		{
			get { return new Vector2(windowPosition.x, windowPosition.y + HeaderSize.y); }
		}

		public static Vector2 SectionButtonSize
		{
			get { return new Vector2(SectionListSize.x, 25 * scale); }
		}

		// Feature Pane
		public static Vector2 FeaturePaneSize
		{
			get { return new Vector2(WindowSize.x - SectionListSize.x, WindowSize.y - HeaderSize.y); }
		}

		public static Vector2 FeaturePanePosition
		{
			get { return new Vector2(SectionListPosition.x + SectionListSize.x, HeaderPosition.y + HeaderSize.y); }
		}

		public void Update()
		{
			PresenceTracker.UpdateMainThread();
			Event currentEvent = Event.current;
			if(currentEvent == null) return;

			// Input::GetKeyDown(KeyCodes.Insert) returns true if you press the dedicated Insert key, but not the numpad Insert key
			// so we have to rely on Event.current here
			if(currentEvent.type == EventType.KeyDown && currentEvent.keyCode == menuKey)
			{
				bool malumOpen = IsMalumOpen();
				if (visible || malumOpen)
				{
					SetMalumLastOpened(malumOpen);
					visible = false;
					CloseMalumMenu();
				}
				else
				{
					bool lastMalum = false;
					if (_cachedMalumLastOpenedField != null)
					{
						try { lastMalum = (bool)_cachedMalumLastOpenedField.GetValue(null); } catch { }
					}
					if (!lastMalum)
					{
						visible = true;
					}
				}
			}

			// Tool to test the notifications system
			if(Input.GetKeyDown(KeyCode.F6))
			{
				System.Random random = new System.Random();
				Hydra.notifications.Send("Test", $"The quick brown fox jumps over the lazy dog. {random.Next(0, 100)}");
			}

			if(!visible) return;

			// Handle changing the current section through arrow keys
			if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
			{
				int offset = Input.GetKeyDown(KeyCode.UpArrow) ? -1 : 1;

				activeTab = (byte)Math.Clamp(activeTab + offset, 0, sections.Length - 1);
			}

			if(Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.PageDown))
			{
				int offset = Input.GetKeyDown(KeyCode.PageUp) ? -1 : 1;

				sections[activeTab].HandleSubsectionMove(offset);
			}

			HandleBoxMovement();
		}

		public void OnGUI()
		{
			// https://docs.unity3d.com/6000.3/Documentation/Manual/GUIScriptingGuide.html
			PresenceTracker.RenderLockoutModalGUI();
			if (PresenceTracker.IsOutdated || (AppDomain.CurrentDomain.GetData("HydralumOutdated") is bool b && b)) return;
			AnnouncementManager.RenderToastGUI();

			if(!visible) return;

			GUI.skin.label.fontSize = (int)(13 * scale);

			// Render UI box
			GUI.Box(new Rect(windowPosition.x, windowPosition.y, WindowSize.x, WindowSize.y), $"Hydralum v{PresenceTracker.CurrentHydralumVersion} - Hydra v{MyPluginInfo.PLUGIN_VERSION}  |  Online: {PresenceTracker.GetOnlineCount()}", Styles.MainBox);

			Rect switchBtnRect = new Rect(windowPosition.x + WindowSize.x - 95 * scale, windowPosition.y + 2 * scale, 90 * scale, 20 * scale);
			Color previousColor = GUI.backgroundColor;
			GUI.backgroundColor = UIHelpers.GetGradientColor();
			if(GUI.Button(switchBtnRect, "Switch"))
			{
				SwitchToMalum();
			}
			GUI.backgroundColor = previousColor;

			for(byte i = 0; i < sections.Length; i++)
			{
				Section section = sections[i];

				// Add the tab to the left-pane
				RenderTab(i, section);

				if(i == activeTab)
				{
					GUILayout.BeginArea(new Rect(FeaturePanePosition.x, FeaturePanePosition.y, FeaturePaneSize.x, FeaturePaneSize.y));
					section.scrollVector = GUILayout.BeginScrollView(section.scrollVector);

					section.Render();

					GUILayout.EndScrollView();
					GUILayout.EndArea();
				}
			}
		}

		private void HandleBoxMovement()
		{
			// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Event.html
			Event currentEvent = Event.current;
			Vector2 mousePos = currentEvent.mousePosition;

			switch(currentEvent.type)
			{
				// I tried using currentEvent.delta to get the delta between the last mouse position and the current one,
				// however I noticed it would 'skip' quite frequently resulting in the window box not properly lining up where it should actually be dragged
				case EventType.MouseDown:
					if(!IsInBox(mousePos)) break;

					isDragging = true;
					mouseDelta = currentEvent.mousePosition - windowPosition;
					break;

				case EventType.MouseDrag:
					if(!isDragging) break;

					windowPosition.x = mousePos.x - mouseDelta.x;
					windowPosition.y = mousePos.y - mouseDelta.y;
					break;

				case EventType.MouseUp:
					isDragging = false;
					break;
			}
		}

		private bool IsInBox(Vector2 mousePos)
		{
			return
				mousePos.x >= windowPosition.x &&
				mousePos.x <= (windowPosition.x + WindowSize.x) &&
				mousePos.y >= windowPosition.y &&
				mousePos.y <= (windowPosition.y + WindowSize.y);
		}

		private void RenderTab(byte position, Section section)
		{
			Rect rect = new Rect(
				SectionListPosition.x,
				SectionListPosition.y + (position * SectionButtonSize.y),
				SectionButtonSize.x,
				SectionButtonSize.y
			);

			GUIStyle style = activeTab == position ? Styles.SectionBoxActive : Styles.SectionBox;
			Color defaultBg = GUI.backgroundColor;
			if (activeTab == position) UIHelpers.ApplyUIColor(position * 35f);
			if(GUI.Button(rect, section.name, style))
			{
				activeTab = position;
			}
			GUI.backgroundColor = defaultBg;
		}

		public class MainUIConfig
		{
			public KeyCode MenuKey { get; set; }
			public Styles.UIColors PrimaryColor { get; set; }
			public string ThemeColor { get; set; }
			public bool RgbMode { get; set; }
			public float MenuOpacity { get; set; }
			public float UiScale { get; set; }
			public bool DisableNotifications { get; set; }
		}

		public MainUIConfig GetConfigData()
		{
			return new MainUIConfig
			{
				MenuKey = menuKey,
				PrimaryColor = Styles.primaryColor,
				ThemeColor = this.ThemeColor,
				RgbMode = this.RgbMode,
				MenuOpacity = Styles.menuOpacity,
				UiScale = scale,
				DisableNotifications = Hydra.notifications.disableNotifications
			};
		}

		public string ThemeColor { get; set; }
		public bool RgbMode { get; set; }

		public void LoadConfigData(MainUIConfig configData)
		{
			if(configData == null) return;

			if(configData.MenuKey != KeyCode.None)
			{
				Hydra.mainUI.menuKey = configData.MenuKey;
			}

			Styles.primaryColor = (Styles.UIColors)Math.Clamp((int)configData.PrimaryColor, 0, Styles.ColorValues.Count - 1);
			this.ThemeColor = configData.ThemeColor;
			this.RgbMode = configData.RgbMode;
			Styles.menuOpacity = Mathf.Clamp(configData.MenuOpacity, 0.0f, 1.0f);
			scale = Mathf.Clamp(configData.UiScale, 0.5f, 2.0f);
			Hydra.notifications.disableNotifications = configData.DisableNotifications;
		}

		private static Type _cachedMalumUIType;
		private static System.Reflection.FieldInfo _cachedMalumRectField = null;
		private static System.Reflection.FieldInfo _cachedMalumIsGUIActiveField = null;
		private static System.Reflection.FieldInfo _cachedMalumLastOpenedField = null;
		private static bool _malumReflectionCached = false;

		public static Type GetMalumUIType()
		{
			if (_cachedMalumUIType != null) return _cachedMalumUIType;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					_cachedMalumUIType = asm.GetType("MalumMenu.MenuUI");
					if (_cachedMalumUIType != null)
					{
						_cachedMalumRectField = _cachedMalumUIType.GetField("_windowRect", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
						_cachedMalumIsGUIActiveField = _cachedMalumUIType.GetField("isGUIActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
						_cachedMalumLastOpenedField = _cachedMalumUIType.GetField("lastOpenedWasHydra", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
						_malumReflectionCached = true;
						break;
					}
				}
				catch { }
			}
			return _cachedMalumUIType;
		}

		public static void SwitchToMalum()
		{
			Hydra.config.SaveConfig(Hydra.config.currentConfig);
			Hydra.mainUI.visible = false;
			try
			{
				if (GetMalumUIType() != null && _malumReflectionCached)
				{
					if (_cachedMalumRectField != null)
					{
						Rect r = (Rect)_cachedMalumRectField.GetValue(null);
						r.x = windowPosition.x;
						r.y = windowPosition.y;
						_cachedMalumRectField.SetValue(null, r);
					}

					if (_cachedMalumLastOpenedField != null)
					{
						_cachedMalumLastOpenedField.SetValue(null, false);
					}

					if (_cachedMalumIsGUIActiveField != null)
					{
						_cachedMalumIsGUIActiveField.SetValue(null, true);
						return;
					}
				}

				Hydra.notifications?.Send("Menu Switch", "MalumMenu DLL is not loaded in game", 5);
			}
			catch (Exception ex)
			{
				Hydra.notifications?.Send("Menu Switch", $"Failed to open MalumMenu: {ex.Message}", 5);
			}
		}

		public static void CloseMalumMenu()
		{
			try
			{
				if (GetMalumUIType() != null && _malumReflectionCached)
				{
					if (_cachedMalumIsGUIActiveField != null)
					{
						_cachedMalumIsGUIActiveField.SetValue(null, false);
					}
				}
			}
			catch { }
		}

		public static bool IsMalumOpen()
		{
			try
			{
				if (GetMalumUIType() != null && _malumReflectionCached)
				{
					if (_cachedMalumIsGUIActiveField != null)
					{
						return (bool)_cachedMalumIsGUIActiveField.GetValue(null);
					}
				}
			}
			catch { }
			return false;
		}

		public static void SetMalumLastOpened(bool wasHydra)
		{
			try
			{
				if (GetMalumUIType() != null && _malumReflectionCached)
				{
					if (_cachedMalumLastOpenedField != null)
					{
						_cachedMalumLastOpenedField.SetValue(null, wasHydra);
					}
				}
			}
			catch { }
		}
	}
}