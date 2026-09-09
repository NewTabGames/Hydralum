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

	// Keep the Match Info button pinned just left of the Chat + Settings box. Letting the game place it
	// (vanilla) drops it far to the left with a big gap, so we always override the offset here.
	// x is the distance from the RIGHT edge: smaller x = further right (closer to the Chat box).
	// Tune this one value if the spacing needs nudging: lower it to close the gap, raise it to open it.
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
					dist.x = 2.85f;
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

			// Only intervene with chat when the user has explicitly enabled the "Enable Chat" cheat
			if (CheatToggles.enableChat)
			{
				if (__instance.Chat != null && __instance.Chat.gameObject != null && !__instance.Chat.gameObject.activeSelf)
				{
					__instance.Chat.gameObject.SetActive(true);
				}
			}

			MalumCheats.UseVentCheat(__instance);
			MalumESP.ZoomOut(__instance);
			MalumESP.FreecamCheat();
			MinimapHandler.TrackPositions(); // record positions so the map can freeze them during meetings
			KillCooldownEsp.Update(); // reset impostor cooldowns when gameplay resumes after a meeting/exile
			NocturneDoors.Tick(); // keep pinned doors (from the radar) shut

			// Close PlayerPickMenu if there is no PPM cheat enabled
			if (PlayerPickMenu.playerpickMenu != null && CheatToggles.ShouldPPMClose())
			{
				PlayerPickMenu.playerpickMenu.Close();
			}
		}
		catch { }
	}
}
