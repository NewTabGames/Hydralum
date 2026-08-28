using HarmonyLib;
using UnityEngine;

namespace HydraMenu.routines
{
	internal class RoutineManager : MonoBehaviour
	{
		public AutoTriggerSporesRoutine autoTriggerSpores = new AutoTriggerSporesRoutine();
		public DiscoHostRoutine discoHost = new DiscoHostRoutine();
		public DoorTrollerRoutine doorTroller = new DoorTrollerRoutine();
		public JailPlayerRoutine jailPlayer = new JailPlayerRoutine();
		public PetPlayerRoutine petPlayer = new PetPlayerRoutine();
		public PlayerFollowerRoutine playerFollower = new PlayerFollowerRoutine();
		public ReportBodySpam reportBodySpam = new ReportBodySpam();
		public TeleportSpammer teleportSpammer = new TeleportSpammer();

		public readonly IRoutine[] routineList;

		public RoutineManager()
		{
			routineList = [ autoTriggerSpores, discoHost, doorTroller, jailPlayer, petPlayer, playerFollower, reportBodySpam, teleportSpammer ];
		}

		public void Update()
		{
			if (routineList == null) return;
			foreach(IRoutine routine in routineList)
			{
				if(routine == null || !routine.Enabled) continue;

				routine.Run();
			}
		}

		[HarmonyPatch(typeof(GameData), nameof(GameData.OnDisconnected))]
		class DisconnectHandler
		{
			static void Prefix()
			{
				Hydra.Log?.LogInfo("Player disconnected from the lobby, disabling relevant routines");

				if (Hydra.routines?.routineList == null) return;
				foreach(IRoutine routine in Hydra.routines.routineList)
				{
					if(routine == null || !routine.Enabled) continue;

					routine.OnDisconnect();
				}
			}
		}

		[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
		class ExitGameHandler
		{
			static void Prefix()
			{
				Hydra.Log?.LogInfo("Player exited game, disabling relevant routines");

				if (Hydra.routines?.routineList == null) return;
				foreach(IRoutine routine in Hydra.routines.routineList)
				{
					if(routine == null || !routine.Enabled) continue;

					routine.OnDisconnect();
				}
			}
		}
	}
}