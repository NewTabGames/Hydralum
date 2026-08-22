using HydraMenu.features;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class VisualSection : ISection
	{
		public VisualSection() : base("Visual") { }

		public override void Render()
		{
			Visuals.SkipShhhAnimation.Enabled = GUILayout.Toggle(Visuals.SkipShhhAnimation.Enabled, "Skip Shhh Animation");
			Visuals.NoSeekerAnimationPatch.Enabled = GUILayout.Toggle(Visuals.NoSeekerAnimationPatch.Enabled, "Skip Seeker Animation");
			Visuals.AccurateDisconnectReasons.Enabled = GUILayout.Toggle(Visuals.AccurateDisconnectReasons.Enabled, "Use more accurate disconnection reasons");

			bool newFullbright = GUILayout.Toggle(Visuals.Fullbright.Enabled, "Fullbright");
			if (newFullbright != Visuals.Fullbright.Enabled)
			{
				Visuals.Fullbright.Enabled = newFullbright;
				if (HydraConfig.Fullbright != null) HydraConfig.Fullbright.Value = newFullbright;
			}

			Visuals.ShowProtections.Enabled = GUILayout.Toggle(Visuals.ShowProtections.Enabled, "Show Guardian Angel Protections");

			bool newAlwaysChat = GUILayout.Toggle(Chat.AlwaysVisibleChat.Enabled, "Always Visible Chat");
			if (newAlwaysChat != Chat.AlwaysVisibleChat.Enabled)
			{
				Chat.AlwaysVisibleChat.Enabled = newAlwaysChat;
				if (HydraConfig.AlwaysVisibleChat != null) HydraConfig.AlwaysVisibleChat.Value = newAlwaysChat;
			}

			bool newShowGhosts = GUILayout.Toggle(Visuals.ShowGhosts.Enabled, "Show Ghosts");
			if (newShowGhosts != Visuals.ShowGhosts.Enabled)
			{
				Visuals.ShowGhosts.Enabled = newShowGhosts;
				if (HydraConfig.ShowGhosts != null) HydraConfig.ShowGhosts.Value = newShowGhosts;
			}

			bool newGhostChat = GUILayout.Toggle(Chat.OnChat.ShowMessagesByGhosts, "Show messages by ghosts");
			if (newGhostChat != Chat.OnChat.ShowMessagesByGhosts)
			{
				Chat.OnChat.ShowMessagesByGhosts = newGhostChat;
				if (HydraConfig.ShowMessagesByGhosts != null) HydraConfig.ShowMessagesByGhosts.Value = newGhostChat;
			}
		}
	}
}