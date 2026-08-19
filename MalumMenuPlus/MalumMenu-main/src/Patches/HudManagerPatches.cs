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
			if (hud == null || hud.transform == null) return;

			// Find reference position from Chat or Map button
			Transform refTransform = hud.Chat != null ? hud.Chat.transform : (hud.MapButton != null ? hud.MapButton.transform : null);
			if (refTransform == null) return;

			// Look for Detective Notes / Notepad button container
			for (int i = 0; i < hud.transform.childCount; i++)
			{
				Transform child = hud.transform.GetChild(i);
				if (child == null || child == refTransform) continue;

				string name = child.name;
				if (name.IndexOf("Note", StringComparison.OrdinalIgnoreCase) >= 0 ||
				    name.IndexOf("Detective", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// If the Notes button is positioned too close to the right edge and overlaps Chat
					Vector3 pos = child.localPosition;
					float targetX = refTransform.localPosition.x - 0.95f;
					if (pos.x > targetX && Math.Abs(pos.y - refTransform.localPosition.y) < 0.5f)
					{
						pos.x = targetX;
						child.localPosition = pos;
					}
				}
			}
		}
		catch { }
	}
}
