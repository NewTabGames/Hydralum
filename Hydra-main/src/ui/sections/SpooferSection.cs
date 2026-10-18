using HydraMenu.modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class SpooferSection : Section
	{
		public SpooferSection() : base("Spoofer") { }

		public readonly Dictionary<string, int> versions = new Dictionary<string, int>()
		{
			// Current version at runtime
			// VersionShower::Start uses ReferenceDataManager.Refdata.userFacingVersion to get version strings such as "17.1"
			// however that doesn't seem to before the game fully loads, so we have to use Constants::AddressablesVersion to get a less human-understandable version string
			{ $"{Constants.AddressablesVersion} (Current)", Constants.GetBroadcastVersion() },
			{ "16.1.0", 50632950 },
			{ "17.1", 50643450 },
			{ "17.1.2", 50647000 },
			{ "17.2", 50645050 },
			{ "17.2.1", 50652900 },
			{ "17.2.2", 50653700 },
			{ "17.3", 50652400 },
			{ "17.4", 50656300 },
			{ "18.0", 50663350 }
		};

		private int versionSelection = 0;

		// Click-to-type state for the exact-level field. GUILayout.TextField is unstripped under IL2CPP and
		// crashes ("Method unstripping failed"), so (like the config name field) this is a click-to-type
		// button backed by manual key capture, restricted to digits.
		private string levelInput = "";
		private bool typingLevel = false;

		public override void Render()
		{
			GUILayout.Label("Version Spoofer:");
			ModuleManager.spoofVersion.Enabled = GUILayout.Toggle(ModuleManager.spoofVersion.Enabled, "Enable Version Spoofing");

			GUILayout.Label($"Spoofed Version: {versions.ElementAt(versionSelection).Key} ({ModuleManager.spoofVersion.spoofedVersion})");
			versionSelection = (int)GUILayout.HorizontalSlider(versionSelection, 0, versions.Count - 1);
			ModuleManager.spoofVersion.spoofedVersion = versions.ElementAt(versionSelection).Value;

			ModuleManager.spoofVersion.UseModdedProtocol = GUILayout.Toggle(ModuleManager.spoofVersion.UseModdedProtocol, "Use Modded Protocol");

			GUILayout.Space(5);
			GUILayout.Label("Level Spoofer:");

			ModuleManager.spoofLevel.Enabled = GUILayout.Toggle(ModuleManager.spoofLevel.Enabled, "Enabled");
			GUILayout.Label($"Spoofed Level: {ModuleManager.spoofLevel.SpoofedLevel}");
			ModuleManager.spoofLevel.SpoofedLevel = (uint)GUILayout.HorizontalSlider(ModuleManager.spoofLevel.SpoofedLevel, 1, 200);

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("-100"))
			{
				ClampSelectedLevel(ModuleManager.spoofLevel.SpoofedLevel - 100);
			}

			if(GUILayout.Button("-10"))
			{
				ClampSelectedLevel(ModuleManager.spoofLevel.SpoofedLevel - 10);
			}

			if(GUILayout.Button("+10"))
			{
				ClampSelectedLevel(ModuleManager.spoofLevel.SpoofedLevel + 10);
			}

			if(GUILayout.Button("+100"))
			{
				ClampSelectedLevel(ModuleManager.spoofLevel.SpoofedLevel + 100);
			}
			GUILayout.EndHorizontal();

			// Type an exact level, then Enter (or Set). Lets you reach any level directly instead of nudging
			// with the slider/buttons. The value is clamped the same way the +/- buttons are.
			GUILayout.BeginHorizontal();
			string levelLabel = typingLevel
				? (string.IsNullOrEmpty(levelInput) ? "Type a level, then Enter..." : levelInput + "_")
				: "Type a level (click)";
			if(GUILayout.Button(levelLabel)) typingLevel = !typingLevel;
			if(GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
			{
				ApplyTypedLevel();
				typingLevel = false;
			}
			GUILayout.EndHorizontal();
			if(typingLevel) CaptureLevelInput();

			if(GUILayout.Button("Send Level Update"))
			{
				PlayerControl.LocalPlayer.RpcSetLevel(ModuleManager.spoofLevel.SpoofedLevel - 1);
				Hydra.notifications.Send("Level Updater", $"Your level has been changed to {ModuleManager.spoofLevel.SpoofedLevel}", 5);
			}

			GUILayout.Space(5);
			GUILayout.Label("Platform Spoofer:");

			ModuleManager.spoofDevice.Enabled = GUILayout.Toggle(ModuleManager.spoofDevice.Enabled, "Enable Platform Spoofing");
			var platforms = HydraMenu.modules.spoofer.SpoofDevice.SpoofablePlatforms;
			int platformIdx = Array.IndexOf(platforms, ModuleManager.spoofDevice.SpoofedPlatform);
			if(platformIdx < 0) platformIdx = 0;
			GUILayout.Label($"Spoofed Platform: {HydraMenu.modules.spoofer.SpoofDevice.PlatformName(ModuleManager.spoofDevice.SpoofedPlatform)}");
			platformIdx = Mathf.Clamp((int)Mathf.Round(GUILayout.HorizontalSlider(platformIdx, 0, platforms.Length - 1)), 0, platforms.Length - 1);
			ModuleManager.spoofDevice.SpoofedPlatform = platforms[platformIdx];
		}

		// Builds the typed level from raw GUI key events (digits only). Enter/Set applies it; Escape cancels.
		private void CaptureLevelInput()
		{
			var e = Event.current;
			if(e == null || e.type != EventType.KeyDown) return;

			if(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
			{
				ApplyTypedLevel();
				typingLevel = false;
				e.Use();
				return;
			}

			if(e.keyCode == KeyCode.Escape)
			{
				typingLevel = false;
				e.Use();
				return;
			}

			if(e.keyCode == KeyCode.Backspace)
			{
				if(!string.IsNullOrEmpty(levelInput))
					levelInput = levelInput.Substring(0, levelInput.Length - 1);
				e.Use();
				return;
			}

			char c = e.character;
			// Digits only, and cap the length so it can't overflow the parse below.
			if(c >= '0' && c <= '9' && levelInput.Length < 10)
			{
				levelInput += c;
				e.Use();
			}
		}

		// Parses the typed digits and applies them as the spoofed level (clamped like the +/- buttons).
		private void ApplyTypedLevel()
		{
			if(string.IsNullOrEmpty(levelInput)) return;

			if(ulong.TryParse(levelInput, out ulong typed))
			{
				// Keep it inside uint before ClampSelectedLevel narrows it to the anticheat-aware ceiling.
				uint capped = typed > uint.MaxValue - 1 ? uint.MaxValue - 1 : (uint)typed;
				ClampSelectedLevel(capped);
			}

			levelInput = "";
		}

		private void ClampSelectedLevel(uint newLevel)
		{
			// Do we really need to have an upper bounds on the level value?
			// I doubt anyone will press the +100 that much anyway
			uint maxLevel = Utilities.IsAnticheatPresent() ? 100001 : uint.MaxValue - 1;

			ModuleManager.spoofLevel.SpoofedLevel = Math.Clamp(newLevel, 0, maxLevel);
		}
	}
}