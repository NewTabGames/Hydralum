using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui.sections
{
	internal class SabotageSection : ISection
	{
		public SabotageSection() : base("Sabotage") { }

		public override void Render()
		{
			if(ShipStatus.Instance == null)
			{
				GUILayout.Label("You are not currently in a game, or the game has not started yet. These options will not work.");
			}

			Sabotage.UpdateSystemsDirectly = GUILayout.Toggle(Sabotage.UpdateSystemsDirectly, "Update Sabotage Systems Directly");

			Dictionary<string, SystemTypes> sabotages = Sabotage.GetSabotages();
			Dictionary<string, SystemTypes> doors = Sabotage.GetDoors();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Sabotage All"))
			{
				Sabotage.SabotageAll();
				Hydra.notifications?.Send("Sabotage", "All sabotages have been enabled.", 5);
			}

			if(GUILayout.Button("Close All Doors"))
			{
				Sabotage.LockAll();
				Hydra.notifications?.Send("Sabotage", "All doors have been closed.", 5);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Fix All Sabotages"))
			{
				Sabotage.FixAllSabotages();
				Hydra.notifications?.Send("Sabotage", "All sabotages have been repaired.", 5);
			}

			if(GUILayout.Button("Unlock All Doors"))
			{
				if(Sabotage.CanUnlockDoors())
				{
					Sabotage.UnlockAll();
					Hydra.notifications?.Send("Sabotage", "All doors have been unlocked.", 5);
				}
				else
				{
					Hydra.notifications?.Send("Sabotage", "The map you are currently on does not support unlocking doors.", 10);
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.Label("Sabotages:");
			foreach(var (key, value) in sabotages)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(key, GUILayout.Width(130));
				if(GUILayout.Button("Sabotage"))
				{
					TriggerSabotage(value);
				}
				if(GUILayout.Button("Fix"))
				{
					FixSabotage(value);
				}
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(5);
			GUILayout.Label("Doors:");
			if(doors.Count == 0)
			{
				GUILayout.Label("This map has no doors that can be closed.");
			}
			else
			{
				foreach(var (key, value) in doors)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label(key, GUILayout.Width(130));
					if(GUILayout.Button("Close"))
					{
						CloseDoor(value);
					}
					if(Sabotage.CanUnlockDoors())
					{
						if(GUILayout.Button("Open"))
						{
							OpenDoor(value);
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void TriggerSabotage(SystemTypes system)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications?.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Hydra.notifications?.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			Sabotage.SabotageSystem(system);
			Hydra.notifications?.Send("Sabotage", $"{system} has been sabotaged.", 5);
		}

		private void FixSabotage(SystemTypes system)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications?.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Hydra.notifications?.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			Sabotage.FixSabotage(system);
			Hydra.notifications?.Send("Sabotage", $"{system} has been fixed.", 5);
		}

		private void CloseDoor(SystemTypes door)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications?.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Hydra.notifications?.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			Sabotage.LockDoor(door);
		}

		private void OpenDoor(SystemTypes door)
		{
			if(PlayerControl.LocalPlayer == null)
			{
				Hydra.notifications?.Send("Sabotage", "This option can only be used inside of a game.");
				return;
			}

			if(ShipStatus.Instance == null)
			{
				Hydra.notifications?.Send("Sabotage", "There must be an instance of ShipStatus for this feature to work.");
				return;
			}

			if(!Sabotage.CanUnlockDoors())
			{
				Hydra.notifications?.Send("Sabotage", "You can only unlock doors if you are the host or if the map is Polus, Airship, or Fungle.");
				return;
			}

			Sabotage.UnlockDoor(door);
		}
	}
}