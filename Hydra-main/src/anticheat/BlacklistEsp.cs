using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HydraMenu.anticheat
{
	// Draws a red "Flagged" label just above a blacklisted player's floating name.
	//
	// It's rendered as its OWN TextMeshPro child object (not by editing cosmetics.nameText). MalumMenu
	// rewrites every player's nameText in PlayerPhysics.LateUpdate, so prepending text to the name would
	// fight it and flicker; a separate object sidesteps that entirely and works whether or not Malum is
	// loaded. Driven per-player from PlayerControl.FixedUpdate.
	internal static class BlacklistEsp
	{
		private const string TagObjectName = "HydraFlagTag";

		public static void Update(PlayerControl player)
		{
			try
			{
				if (player == null || player.Data == null || player.cosmetics == null) return;

				var nameText = player.cosmetics.nameText;
				if (nameText == null) return;

				var tagTransform = player.transform.Find(TagObjectName);
				bool flagged = !player.Data.Disconnected && Blacklist.IsBlacklisted(player);

				if (!flagged)
				{
					if (tagTransform != null && tagTransform.gameObject.activeSelf) tagTransform.gameObject.SetActive(false);
					return;
				}

				if (tagTransform == null)
				{
					var go = new GameObject(TagObjectName);
					go.transform.SetParent(player.transform, false);
					go.transform.localScale = Vector3.one;

					var tmp = go.AddComponent<TextMeshPro>();
					tmp.alignment = TextAlignmentOptions.Center;
					tmp.fontSize = nameText.fontSize;
					tmp.font = nameText.font;
					tmp.fontSharedMaterial = nameText.fontSharedMaterial;
					tmp.sortingLayerID = nameText.sortingLayerID;
					tmp.sortingOrder = 32767; // always on top of walls/shadows/other players
					tmp.text = "<color=#FF3B3B>Flagged</color>";

					tagTransform = go.transform;
				}
				else if (!tagTransform.gameObject.activeSelf)
				{
					tagTransform.gameObject.SetActive(true);
				}

				// Sit above the TOP of the whole name block, not just the name line. Other menus (and Malum)
				// stack role / friend code / cooldown tags as extra lines that grow upward inside nameText,
				// so we read the name's world-space rendered bounds and place "Flagged" just above the
				// highest pixel. Using the renderer's world bounds makes this anchor-, scale- and line-count-
				// agnostic and automatically accounts for however the name has been lifted.
				var nameRenderer = nameText.GetComponent<Renderer>();
				Vector3 namePos = nameText.transform.position;
				if (nameRenderer != null && nameRenderer.bounds.size.y > 0f)
				{
					var b = nameRenderer.bounds;
					tagTransform.position = new Vector3(b.center.x, b.max.y + 0.12f, namePos.z);
				}
				else
				{
					// Fallback if the name hasn't rendered a mesh yet this frame.
					tagTransform.position = namePos + new Vector3(0f, 0.3f, 0f);
				}
			}
			catch { }
		}

		[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
		private static class PlayerControl_FixedUpdate_FlagTag
		{
			private static void Postfix(PlayerControl __instance) => Update(__instance);
		}
	}
}
