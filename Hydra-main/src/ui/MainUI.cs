using HydraMenu.ui.sections;
using Il2CppInterop.Runtime.Attributes;
using System;
using UnityEngine;

namespace HydraMenu.ui
{
	public class MainUI : MonoBehaviour
	{
		// Current window
		public static bool visible = false;
		public static float scale = 1.0f;

		private bool isDragging = false;
		private Vector2 mouseDelta = new Vector2();

		public static Vector2 windowPosition = new Vector2(250, 100);
		public static Vector2 WindowSize
		{
			get { return new Vector2(520, 470) * scale; }
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
		private readonly ISection[] sections = { new GeneralSection(), new SelfSection(), new TrollSection(), new SabotageSection(), new HostSection(), new PlayersSection(), new MovementSection(), new VisualSection(), new ProtectionsSection(), new AnticheatSection(), new SpooferSection(), new MenuSection(), new InfoSection() };
		public byte activeTab = 0;

		public static Vector2 SectionListSize
		{
			get { return new Vector2(120 * scale, WindowSize.y - HeaderSize.y); }
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
			AnnouncementManager.Update();
			PresenceTracker.UpdateMainThread();

			if(Input.GetKeyDown(KeyCode.F6))
			{
				if (IsMalumOpen())
				{
					CloseMalumMenu();
					visible = false;
					HydraConfig.Save();
				}
				else
				{
					visible = !visible;
					if(visible)
					{
						CloseMalumMenu();
						SetMalumLastOpened(true);

						if(HydraConfig.OpenOnCursor != null && HydraConfig.OpenOnCursor.Value)
						{
							Vector2 mousePos = Input.mousePosition;
							float x = Mathf.Clamp(mousePos.x, 0, Mathf.Max(0, Screen.width - WindowSize.x));
							float y = Mathf.Clamp(Screen.height - mousePos.y, 0, Mathf.Max(0, Screen.height - WindowSize.y));
							windowPosition = new Vector2(x, y);
						}
					}
					else
					{
						HydraConfig.Save();
					}
				}
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
		}

		public void OnDisable()
		{
			HydraConfig.Save();
		}

		public void OnGUI()
		{
			try
			{
				AnnouncementManager.RenderToastGUI();
			}
			catch { }

			// https://docs.unity3d.com/6000.3/Documentation/Manual/GUIScriptingGuide.html
			if(!visible) return;

			try
			{
				GUI.skin.label.fontSize = (int)(13 * scale);

				// Render UI box
				GUI.Box(new Rect(windowPosition.x, windowPosition.y, WindowSize.x, WindowSize.y), $"Hydralum v1.1.5 - Hydra v{MyPluginInfo.PLUGIN_VERSION}  |  Online: {PresenceTracker.GetOnlineCount()}", Styles.MainBox);

				// Switch button on top header matching the Hydralum mock design
				Rect switchBtnRect = new Rect(windowPosition.x + WindowSize.x - 95 * scale, windowPosition.y + 2 * scale, 90 * scale, 20 * scale);
				Color previousColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(0.6f, 0.2f, 0.8f);
				if(GUI.Button(switchBtnRect, "Switch"))
				{
					SwitchToMalum();
				}
				GUI.backgroundColor = previousColor;

				for(byte i = 0; i < sections.Length; i++)
				{
					ISection section = sections[i];

					// Add the tab to the left-pane
					RenderTab(i, section);

					if(i == activeTab)
					{
						GUILayout.BeginArea(new Rect(FeaturePanePosition.x, FeaturePanePosition.y, FeaturePaneSize.x, FeaturePaneSize.y));
						section.scrollVector = GUILayout.BeginScrollView(section.scrollVector);

						try
						{
							section.Render();
						}
						catch (Exception ex)
						{
							GUILayout.Label($"<color=red>Error rendering {section.name} section:</color>\n<size=11>{ex.Message}</size>");
						}

						GUILayout.EndScrollView();
						GUILayout.EndArea();
					}
				}

				HandleBoxMovement();
			}
			catch { }
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
					if (isDragging)
					{
						isDragging = false;
						HydraConfig.Save();
					}
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

		[HideFromIl2Cpp]
		private void RenderTab(byte position, ISection section)
		{
			Rect rect = new Rect(
				SectionListPosition.x,
				SectionListPosition.y + (position * SectionButtonSize.y),
				SectionButtonSize.x,
				SectionButtonSize.y
			);

			Color prevBg = GUI.backgroundColor;
			if(activeTab == position)
			{
				GUI.backgroundColor = Styles.GetActiveColor(position * 35f);
			}

			GUIStyle style = activeTab == position ? Styles.SectionBoxActive : Styles.SectionBox;
			if(GUI.Button(rect, section.name, style))
			{
				activeTab = position;
			}

			GUI.backgroundColor = prevBg;
		}

		private static Type _cachedMalumUIType;
		public static Type GetMalumUIType()
		{
			if (_cachedMalumUIType != null) return _cachedMalumUIType;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					_cachedMalumUIType = asm.GetType("MalumMenu.MenuUI");
					if (_cachedMalumUIType != null) break;
				}
				catch { }
			}
			return _cachedMalumUIType;
		}

		public static void SwitchToMalum()
		{
			HydraConfig.Save();
			visible = false;
			try
			{
				Type malumUIType = GetMalumUIType();

				if (malumUIType != null)
				{
					// Seamless in-place switch: match HydraMenu's current window position
					var rectField = malumUIType.GetField("_windowRect", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
					if (rectField != null)
					{
						Rect r = (Rect)rectField.GetValue(null);
						r.x = windowPosition.x;
						r.y = windowPosition.y;
						rectField.SetValue(null, r);
					}

					var lastOpenedField = malumUIType.GetField("lastOpenedWasHydra", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
					if (lastOpenedField != null)
					{
						lastOpenedField.SetValue(null, false);
					}

					var field = malumUIType.GetField("isGUIActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
					if (field != null)
					{
						field.SetValue(null, true);
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
				Type malumUIType = GetMalumUIType();

				if (malumUIType != null)
				{
					var field = malumUIType.GetField("isGUIActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (field != null)
					{
						field.SetValue(null, false);
					}
				}
			}
			catch { }
		}

		public static bool IsMalumOpen()
		{
			try
			{
				Type malumUIType = GetMalumUIType();
				if (malumUIType != null)
				{
					var field = malumUIType.GetField("isGUIActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (field != null)
					{
						return (bool)field.GetValue(null);
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
				Type malumUIType = GetMalumUIType();
				if (malumUIType != null)
				{
					var field = malumUIType.GetField("lastOpenedWasHydra", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (field != null)
					{
						field.SetValue(null, wasHydra);
					}
				}
			}
			catch { }
		}
	}
}