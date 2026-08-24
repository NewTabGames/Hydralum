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

[HarmonyPatch(typeof(HudManager), "SetMapAndInfoButtonsEnabled")]
public static class HudManager_SetMapAndInfoButtonsEnabled
{
	// Keep Map and MatchInfo buttons enabled and properly aligned during meetings
	public static void Prefix(ref bool enabled)
	{
		enabled = true;
	}
}

[HarmonyPatch]
public static class MatchInfoHudButton_Update
{
	public static System.Reflection.MethodBase TargetMethod()
	{
		return AccessTools.Method("MatchInfoHudButton:Update");
	}

	// Keep MatchInfoHudButton placed neatly to the left of the Chat + Settings dual box (3.65f)
	public static bool Prefix(Component __instance)
	{
		try
		{
			if (__instance != null)
			{
				var aspect = __instance.GetComponent<AspectPosition>();
				if (aspect != null)
				{
					Vector3 dist = aspect.DistanceFromEdge;
					dist.x = 3.65f;
					dist.y = 0.505f;
					dist.z = -400f;
					aspect.DistanceFromEdge = dist;
					aspect.AdjustPosition();
				}
			}
		}
		catch { }
		return false;
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
}
