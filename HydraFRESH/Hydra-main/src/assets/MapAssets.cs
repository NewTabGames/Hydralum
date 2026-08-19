using System.Collections.Generic;

namespace HydraMenu.assets
{
	internal class MapAssets
	{
		public static Dictionary<string, TaskTypes> skeldAnimations = new Dictionary<string, TaskTypes>()
		{
			{ "Clear Asteroids", TaskTypes.ClearAsteroids },
			{ "Empty Garbage", TaskTypes.EmptyGarbage },
			{ "Prime Shields", TaskTypes.PrimeShields }
		};

		public static Dictionary<string, TaskTypes> polusAnimations = new Dictionary<string, TaskTypes>()
		{
			{ "Clear Asteroids", TaskTypes.ClearAsteroids }
		};

		public static Dictionary<int, string> skeldVents = new Dictionary<int, string>()
		{
			{ 0, "Admin" },
			{ 1, "Hallway" },
			{ 2, "Cafeteria" },
			{ 3, "Electrical" },
			{ 4, "Upper Engine" },
			{ 5, "Security" },
			{ 6, "Medbay" },
			{ 7, "Weapons" },
			{ 8, "Lower Reactor" },
			{ 9, "Lower Engine" },
			{ 10, "Shields" },
			{ 11, "Upper Reactor" },
			{ 12, "Upper Navigation"},
			{ 13, "Lower Navigation" }
		};

		public static Dictionary<int, string> polusVents = new Dictionary<int, string>()
		{
			{ 0, "Electrical" },
			{ 1, "Outside Electrical" },
			{ 2, "Oxygen" },
			{ 3, "Outside Comms" },
			{ 4, "Office" },
			{ 5, "Comms" },
			{ 6, "Laboratory" },
			{ 7, "Outside Laboratory" },
			{ 8, "Storage" },
			{ 9, "Outside Rocket" },
			{ 10, "Above Electrical" },
			{ 11, "Outside Office" }
		};

		public static Dictionary<string, TaskTypes> GetAnimations()
		{
			MapNames currentMap = Utilities.GetCurrentMap();

			return currentMap switch
			{
				MapNames.Skeld or MapNames.Dleks => skeldAnimations,
				MapNames.Polus => polusAnimations,
				// These maps do not have any task animations, other than medbay scan
				MapNames.MiraHQ or MapNames.Airship or MapNames.Fungle => [],
				// If we do not any known animations for the current map then just default to the Skeld ones
				_ => skeldAnimations
			};
		}

		public static Dictionary<int, string> GetVents()
		{
			MapNames currentMap = Utilities.GetCurrentMap();

			return currentMap switch
			{
				MapNames.Skeld => skeldVents,
				MapNames.Polus => polusVents,
				// If we do not any known vents for the current map then just default to the Skeld ones
				_ => skeldVents
			};
		}
	}
}