using HarmonyLib;
using System;
using UnityEngine;

namespace MalumMenu;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
public static class HudManager_Start
{
	// Postfix patch of HudManager.Start to give minimap access to impostors too
	public static void Postfix(HudManager __instance)
	{
		try
		{
			if (__instance.MapButton != null && __instance.MapButton.OnClick != null)
			{
				__instance.MapButton.OnClick.RemoveAllListeners(); // Remove previous OnClick action

				// Always open normal map when map button is clicked
				// To access sabotage map, sabotage button can be used
				__instance.MapButton.OnClick.AddListener((Action)(() =>
				{
					__instance.ToggleMapVisible(new MapOptions
					{
						Mode = MapOptions.Modes.Normal
					});
				}));
			}
		}
		catch { }
	}
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManager_Update
{
	public static void Postfix(HudManager __instance)
	{
		try
		{
			if (__instance == null) return;

			if (__instance.ShadowQuad != null && __instance.ShadowQuad.gameObject != null)
			{
				__instance.ShadowQuad.gameObject.SetActive(!MalumESP.IsFullbrightActive()); // Fullbright
			}

			// Only intervene with chat when the user has explicitly enabled the "Enable Chat" cheat
			if (CheatToggles.enableChat)
			{
				if (__instance.Chat != null && __instance.Chat.gameObject != null && !__instance.Chat.gameObject.activeSelf)
				{
					__instance.Chat.gameObject.SetActive(true);
				}
			}

			FixTopRightLayout(__instance);

			MalumCheats.UseVentCheat(__instance);
			MalumESP.ZoomOut(__instance);
			MalumESP.FreecamCheat();

			// Close PlayerPickMenu if there is no PPM cheat enabled
			if (PlayerPickMenu.playerpickMenu != null && CheatToggles.ShouldPPMClose())
			{
				PlayerPickMenu.playerpickMenu.Close();
			}
		}
		catch { }
	}

	private static void FixTopRightLayout(HudManager hud)
	{
		try
		{
			if (hud == null) return;

			bool mapActive = hud.MapButton != null && hud.MapButton.gameObject != null && hud.MapButton.gameObject.activeInHierarchy;
			
			// 1. Position MapButton on the far right
			if (hud.MapButton != null)
			{
				var mapAspect = hud.MapButton.GetComponent<AspectPosition>();
				if (mapAspect != null)
				{
					Vector3 d = mapAspect.DistanceFromEdge;
					if (Math.Abs(d.x - 0.65f) > 0.05f)
					{
						d.x = 0.65f;
						mapAspect.DistanceFromEdge = d;
						mapAspect.AdjustPosition();
					}
				}
			}

			// 2. Position Chat button & Settings gear (dual box)
			float chatDistX = mapActive ? 2.05f : 0.65f;

			if (hud.Chat != null && hud.Chat.chatButtonAspectPosition != null)
			{
				Vector3 d = hud.Chat.chatButtonAspectPosition.DistanceFromEdge;
				if (Math.Abs(d.x - chatDistX) > 0.05f)
				{
					d.x = chatDistX;
					hud.Chat.chatButtonAspectPosition.DistanceFromEdge = d;
					hud.Chat.chatButtonAspectPosition.AdjustPosition();
				}
			}

			if (hud.SettingsButton != null)
			{
				var settingsAspect = hud.SettingsButton.GetComponent<AspectPosition>();
				if (settingsAspect != null)
				{
					Vector3 d = settingsAspect.DistanceFromEdge;
					if (Math.Abs(d.x - chatDistX) > 0.05f)
					{
						d.x = chatDistX;
						settingsAspect.DistanceFromEdge = d;
						settingsAspect.AdjustPosition();
					}
				}
			}

			// 3. Position MatchInfoButton (Notepad with ?) cleanly to the left of the Chat+Settings dual box
			float matchDistX = chatDistX + 1.4f; // 3.45f when map is active, 2.05f when map is inactive

			try
			{
				var matchProp = typeof(HudManager).GetProperty("MatchInfoButton");
				if (matchProp != null)
				{
					var matchBtn = matchProp.GetValue(hud) as Component;
					if (matchBtn != null)
					{
						var matchAspect = matchBtn.GetComponent<AspectPosition>();
						if (matchAspect != null)
						{
							Vector3 d = matchAspect.DistanceFromEdge;
							if (Math.Abs(d.x - matchDistX) > 0.05f)
							{
								d.x = matchDistX;
								matchAspect.DistanceFromEdge = d;
								matchAspect.AdjustPosition();
							}
						}
					}
				}
			}
			catch { }

			// Also handle any child object matching MatchInfo / Detective Notes / Notepad
			if (hud.transform != null)
			{
				for (int i = 0; i < hud.transform.childCount; i++)
				{
					Transform child = hud.transform.GetChild(i);
					if (child == null) continue;

					string name = child.name;
					if (name.IndexOf("MatchInfo", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    name.IndexOf("Detective", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    name.IndexOf("Book", StringComparison.OrdinalIgnoreCase) >= 0 ||
					    name.IndexOf("Note", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						var aspect = child.GetComponent<AspectPosition>();
						if (aspect != null)
						{
							Vector3 dist = aspect.DistanceFromEdge;
							if (Math.Abs(dist.x - matchDistX) > 0.05f)
							{
								dist.x = matchDistX;
								aspect.DistanceFromEdge = dist;
								aspect.AdjustPosition();
							}
						}
					}
				}
			}
		}
		catch { }
	}
}
