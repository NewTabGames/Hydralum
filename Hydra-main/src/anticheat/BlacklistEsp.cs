using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HydraMenu.anticheat
{
	// Draws a red "[Flagged]" label above a blacklisted player's floating world name.
	//
	// It's rendered as its OWN TextMeshPro child object (not by editing cosmetics.nameText). MalumMenu
	// rewrites every player's nameText in PlayerPhysics.LateUpdate, so prepending to the name would fight
	// it and flicker; a separate object sidesteps that entirely and works whether or not Malum is loaded.
	// Driven per-player from PlayerControl.FixedUpdate.
	internal static class BlacklistEsp
	{
		private const string TagObjectName = "HydraFlagTag";
		private const string TagText = "<color=#FF3B3B>[Flagged]</color>";

		// IsBlacklisted does a client lookup + string work; calling it every frame for every player was
		// steady per-frame churn (a likely contributor to the periodic GC stutter). Cache the result per
		// player instead - a flag/unflag shows up within a few seconds, which is fine for a label.
		private static readonly Dictionary<byte, (bool flagged, float at)> _flagCache = new Dictionary<byte, (bool flagged, float at)>();
		private const float FlagCacheTtl = 3.0f;

		private static bool IsFlaggedCached(PlayerControl p)
		{
			if (p == null || p.Data == null || p.Data.Disconnected) return false;
			byte id = p.PlayerId;
			float now = Time.unscaledTime;
			if (_flagCache.TryGetValue(id, out var c) && now - c.at < FlagCacheTtl) return c.flagged;
			bool f = Blacklist.IsBlacklisted(p);
			_flagCache[id] = (f, now);
			return f;
		}

		// Players that currently own a tag object. Lets the unflagged fast-path skip the transform lookups
		// entirely instead of calling Find() (which allocates an interop wrapper) for everyone every tick.
		private static readonly HashSet<byte> _active = new HashSet<byte>();

		public static void Update(PlayerControl player)
		{
			try
			{
				if (player == null || player.Data == null || player.cosmetics == null) return;

				byte id = player.PlayerId;
				bool flagged = IsFlaggedCached(player);

				if (!flagged)
				{
					// Fast path for the common case (not flagged): only touch the transform tree if we
					// actually created a tag for this player earlier, so unflagged players cost ~nothing.
					if (_active.Contains(id))
					{
						var t = player.transform.Find(TagObjectName);
						if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
						_active.Remove(id);
					}
					return;
				}

				var nameText = player.cosmetics.nameText;
				if (nameText == null) return;

				var tagTransform = player.transform.Find(TagObjectName);
				_active.Add(id);

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
					tmp.enableWordWrapping = false;
					tmp.text = TagText;

					tagTransform = go.transform;
				}
				else if (!tagTransform.gameObject.activeSelf)
				{
					tagTransform.gameObject.SetActive(true);
				}

				// Sit above the TOP of the whole name block, not just the name line. Other menus (and Malum)
				// stack role / friend code / cooldown tags as extra lines that grow upward inside nameText,
				// so we read the name's world-space rendered bounds and place "[Flagged]" just above the
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
