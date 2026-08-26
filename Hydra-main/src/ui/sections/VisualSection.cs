using HydraMenu.features;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class VisualSection : ISection
	{
		public VisualSection() : base("Visual") { }

		public override void Render()
		{
			bool prevShhh = Visuals.SkipShhhAnimation.Enabled;
			Visuals.SkipShhhAnimation.Enabled = GUILayout.Toggle(Visuals.SkipShhhAnimation.Enabled, "Skip Shhh Animation");
			if (Visuals.SkipShhhAnimation.Enabled != prevShhh) HydraConfig.Save();

			bool prevSeeker = Visuals.NoSeekerAnimationPatch.Enabled;
			Visuals.NoSeekerAnimationPatch.Enabled = GUILayout.Toggle(Visuals.NoSeekerAnimationPatch.Enabled, "Skip Seeker Animation");
			if (Visuals.NoSeekerAnimationPatch.Enabled != prevSeeker) HydraConfig.Save();

			bool prevAcc = Visuals.AccurateDisconnectReasons.Enabled;
			Visuals.AccurateDisconnectReasons.Enabled = GUILayout.Toggle(Visuals.AccurateDisconnectReasons.Enabled, "Use more accurate disconnection reasons");
			if (Visuals.AccurateDisconnectReasons.Enabled != prevAcc) HydraConfig.Save();

			bool prevProt = Visuals.ShowProtections.Enabled;
			Visuals.ShowProtections.Enabled = GUILayout.Toggle(Visuals.ShowProtections.Enabled, "Show Guardian Angel Protections");
			if (Visuals.ShowProtections.Enabled != prevProt) HydraConfig.Save();

		}
	}
}