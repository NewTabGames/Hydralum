using HydraMenu.features;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class SpooferSection : ISection
	{
		public SpooferSection() : base("Spoofer") { }

		public readonly Dictionary<string, int> versions = new Dictionary<string, int>()
		{
			// Current version at runtime
			// VersionShower::Start uses ReferenceDataManager.Refdata.userFacingVersion to get version strings such as "17.1" however that doesn't seem to before the game fully loads, so we have to use Constants::AddressablesVersion to get a less human-understandable version string
			{ $"{Constants.AddressablesVersion} (Current)", Constants.GetBroadcastVersion() },
			{ "16.1.0", 50632950 },
			{ "17.1", 50643450 },
			{ "17.1.2", 50647000 },
			{ "17.2", 50645050 },
			{ "17.2.1", 50652900 },
			{ "17.2.2", 50653700 },
			{ "17.3", 50652400 },
			{ "17.4", 50656300 }
		};

		public static int versionSelection = 0;

		public override void Render()
		{
			GUILayout.Label("Version Spoofer:");
			bool prevSpoof = Spoofer.shouldSpoofVersion;
			Spoofer.shouldSpoofVersion = GUILayout.Toggle(Spoofer.shouldSpoofVersion, "Enable Version Spoofing");
			if (Spoofer.shouldSpoofVersion != prevSpoof) HydraConfig.Save();

			if (versionSelection >= versions.Count) versionSelection = 0;
			GUILayout.Label($"Spoofed Version: {versions.ElementAt(versionSelection).Key} ({Spoofer.spoofedVersion})");
			int prevIdx = versionSelection;
			versionSelection = (int)GUILayout.HorizontalSlider(versionSelection, 0, versions.Count - 1);
			Spoofer.spoofedVersion = versions.ElementAt(versionSelection).Value;
			if (versionSelection != prevIdx) HydraConfig.Save();

			bool prevProto = Spoofer.useModdedProtocol;
			Spoofer.useModdedProtocol = GUILayout.Toggle(Spoofer.useModdedProtocol, "Use Modded Protocol");
			if (Spoofer.useModdedProtocol != prevProto) HydraConfig.Save();
		}
	}
}