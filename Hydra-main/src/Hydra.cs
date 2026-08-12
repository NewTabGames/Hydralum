using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HydraMenu.features;
using HydraMenu.routines;
using HydraMenu.ui;
using UnityEngine;

namespace HydraMenu;

[BepInPlugin("com.mrd.hydramenu", "Hydra", "1.9.0.0")]
[BepInProcess("Among Us.exe")]
internal class Hydra : BasePlugin
{
	internal static new ManualLogSource Log;
	private static readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

	private static MainUI mainUI;
	public static RoutineManager routines;
	public static NotificationManager notifications;

	public override void Load()
	{
		Log = base.Log;

		HydraConfig.Init(Config);

		mainUI = AddComponent<MainUI>();
		notifications = AddComponent<NotificationManager>();
		routines = AddComponent<RoutineManager>();
		notifications.DisableNotifications = HydraConfig.DisableNotifications.Value;

		try
		{
			harmony.PatchAll();
		}
		catch
		{
			notifications.Send("Fatal Error", "Harmony patches failed to load, you are likely using an unsupported version. Check https://github.com/MrDiamond64/Hydra for more information.", 9999);
			throw;
		}

		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} has loaded!");
	}

	public static void Eject()
	{
		harmony.UnpatchSelf();

		notifications.ClearNotifications();

		// Some routines include cleanup in the OnDisable method, which we need to trigger
		foreach(IRoutine routine in routines.routineList)
		{
			routine.Enabled = false;
		}

		Object.Destroy(mainUI);
		Object.Destroy(notifications);
		Object.Destroy(routines);

		ModManager.Instance.ModStamp.enabled = false;
		ModManager.Instance.gameObject.SetActive(false);

		// Eject MalumMenu simultaneously if present
		try
		{
			var malumType = System.Type.GetType("MalumMenu.Utils, MalumMenu");
			if (malumType == null)
			{
				foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
				{
					if (asm.GetName().Name == "MalumMenu")
					{
						malumType = asm.GetType("MalumMenu.Utils");
						break;
					}
				}
			}
			if (malumType != null)
			{
				var ejectMethod = malumType.GetMethod("Eject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				ejectMethod?.Invoke(null, null);
			}
		}
		catch { }
	}

	[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
	class OnGameLoad
	{
		public static void Postfix()
		{
			Log.LogInfo("Adding mod stamp");
			ModManager.Instance.ShowModStamp();
		}
	}
}