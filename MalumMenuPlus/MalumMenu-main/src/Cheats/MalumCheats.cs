using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace MalumMenu;
public static class MalumCheats
{
    private static bool _isScanAnimActive;
    private static bool _isCamsAnimActive;

    public static void CloseMeetingCheat()
    {
        if (!CheatToggles.closeMeeting) return;

        if (Utils.isMeeting) // Closes MeetingHud window if it's open
        {

            // Destroy MeetingHud window gameobject
            MeetingHud.Instance.DespawnOnDestroy = false;
            Object.Destroy(MeetingHud.Instance.gameObject);

            // Gameplay must be reenabled
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false));
            PlayerControl.LocalPlayer.SetKillTimer(GameManager.Instance.LogicOptions.GetKillCooldown());
            ShipStatus.Instance.EmergencyCooldown = GameManager.Instance.LogicOptions.GetEmergencyCooldown();
            Camera.main.GetComponent<FollowerCamera>().Locked = false;
            if (DestroyableSingleton<HudManager>.Instance != null && DestroyableSingleton<HudManager>.Instance.MapButton != null && DestroyableSingleton<HudManager>.Instance.MapButton.gameObject != null)
            {
                DestroyableSingleton<HudManager>.Instance.MapButton.gameObject.SetActive(true);
            }
            DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
            ControllerManager.Instance.CloseAndResetAll();

        }
        else if (ExileController.Instance) // Ends exile cutscene if it's playing
        {
            ExileController.Instance.ReEnableGameplay();
            ExileController.Instance.WrapUp();
        }

        CheatToggles.closeMeeting = false;
    }

    public static void SkipMeetingCheat()
    {
        if (!CheatToggles.skipMeeting) return;

        if (Utils.isMeeting && MeetingHud.Instance != null)
        {
            var rpcMethod = typeof(MeetingHud).GetMethod(nameof(MeetingHud.RpcVotingComplete));
            if (rpcMethod != null)
            {
                var states = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(0L);
                var pars = rpcMethod.GetParameters();
                if (pars.Length >= 5)
                {
                    rpcMethod.Invoke(MeetingHud.Instance, new object[] { states, null, true, false, (ushort)0 });
                }
                else
                {
                    rpcMethod.Invoke(MeetingHud.Instance, new object[] { states, null, true });
                }
            }
        }

        CheatToggles.skipMeeting = false;
    }

    public static void CallMeetingCheat()
    {
        if (!CheatToggles.callMeeting) return;

        if (Utils.isHost)
        {
            // Same as PlayerControl.ReportDeadBody but without additional checks
            MeetingRoomManager.Instance.AssignSelf(PlayerControl.LocalPlayer, null);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcStartMeeting(null);
        }
        else
        {
            PlayerControl.LocalPlayer.CmdReportDeadBody(null);
        }

        CheatToggles.callMeeting = false;
    }

    public static void ForceStartGameCheat()
    {
        if (!CheatToggles.forceStartGame) return;

        if (Utils.isHost && Utils.isLobby)
        {
            AmongUsClient.Instance.SendStartGame();
        }

        CheatToggles.forceStartGame = false;
    }

    public static void CompleteMyTasksCheat()
    {
        if (CheatToggles.completeMyTasks)
        {
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
            {
                Utils.CompleteTask(task);
            }

            CheatToggles.completeMyTasks = false;
        }
    }

    public static void OpenSabotageMapCheat()
    {
        if (!CheatToggles.sabotageMap) return;

        DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Sabotage
        });

        CheatToggles.sabotageMap = false;
    }

    public static void HandleEngineerCheats(EngineerRole engineerRole)
    {
        if (CheatToggles.endlessVentTime)
        {
            // Makes vent time incredibly long (float.MaxValue) so that it never ends
            engineerRole.inVentTimeRemaining = float.MaxValue;
        }
        else if (engineerRole.inVentTimeRemaining > engineerRole.GetCooldown())
        {
            // Vent time is reset to normal value after the cheat is disabled
            engineerRole.inVentTimeRemaining = engineerRole.GetCooldown();
        }

        if (CheatToggles.noVentCooldown)
        {
            if (engineerRole.cooldownSecondsRemaining > 0f)
            {
                engineerRole.cooldownSecondsRemaining = 0f;

                DestroyableSingleton<HudManager>.Instance.AbilityButton.ResetCoolDown();
                DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCooldownFill(0f);
            }
        }
    }

    public static void HandleShapeshifterCheats(ShapeshifterRole shapeshifterRole)
    {
        if (CheatToggles.endlessSsDuration)
        {
            // Makes shapeshift duration so incredibly long (float.MaxValue) so that it never ends
            shapeshifterRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (shapeshifterRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration))
        {
            // Shapeshift duration is reset to normal value after the cheat is disabled
            shapeshifterRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration);

        }
    }

    public static void HandleScientistCheats(ScientistRole scientistRole)
    {
        if (CheatToggles.noVitalsCooldown)
        {
            scientistRole.currentCooldown = 0f;
        }

        if (CheatToggles.endlessBattery)
        {
            // Makes vitals battery so incredibly long (float.MaxValue) so that it never ends
            scientistRole.currentCharge = float.MaxValue;
        }
        else if (scientistRole.currentCharge > scientistRole.RoleCooldownValue)
        {
            // Battery charge is reset to normal value after the cheat is disabled
            scientistRole.currentCharge = scientistRole.RoleCooldownValue;
        }
    }

    public static void HandleTrackerCheats(TrackerRole trackerRole)
    {
        if (CheatToggles.noTrackingCooldown)
        {
            trackerRole.cooldownSecondsRemaining = 0f;
            trackerRole.delaySecondsRemaining = 0f;

            DestroyableSingleton<HudManager>.Instance.AbilityButton.ResetCoolDown();
            DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCooldownFill(0f);
        }

        if (CheatToggles.noTrackingDelay && MapBehaviour.Instance != null)
        {
            MapBehaviour.Instance.trackedPointDelayTime = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDelay);
        }

        if (CheatToggles.endlessTracking)
        {
            // Makes vitals battery so incredibly long (float.MaxValue) so that it never ends
            trackerRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (trackerRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration))
        {
            // Battery charge is reset to normal value after the cheat is disabled
            trackerRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration);
        }
    }

    public static void UseVentCheat(HudManager hudManager)
    {
        // try-catch to prevent errors when role is null
        try
        {

			// Engineers & Impostors don't need this cheat so it is disabled for them
			// Ghost venting causes issues so it is also disabled

			if (!PlayerControl.LocalPlayer.Data.Role.CanVent && !PlayerControl.LocalPlayer.Data.IsDead)
            {
				hudManager.ImpostorVentButton.gameObject.SetActive(CheatToggles.unlockVents);
			}

        } catch { }
    }

    public static void WalkInVentCheat()
    {
        try
        {
            if (!CheatToggles.walkInVents) return;

            PlayerControl.LocalPlayer.inVent = false;
            PlayerControl.LocalPlayer.moveable = true;

        } catch { }
    }

    public static void KickVentsCheat()
    {
        if (!CheatToggles.kickVents) return;

        foreach(var vent in ShipStatus.Instance.AllVents)
        {
            VentilationSystem.Update(VentilationSystem.Operation.BootImpostors, vent.Id);
        }

        CheatToggles.kickVents = false;
    }

    private static float _lastVentBoot;

    public static void DisableVentsCheat()
    {
        if (!CheatToggles.disableVents) return;

        // Continuously does what "Kick All From Vents" does whenever anybody is inside a vent.
        // With "Exclude Yourself" on, your own venting won't trigger it (so you can still vent);
        // otherwise it boots everyone including you.
        var anyoneInVent = false;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.inVent) continue;
            if (CheatToggles.ventsExcludeSelf && player.AmOwner) continue;
            anyoneInVent = true;
            break;
        }

        if (!anyoneInVent) return;

        // Small throttle so network latency (before the boot registers) can't burst-fire the RPC
        if (Time.time - _lastVentBoot < 0.1f) return;
        _lastVentBoot = Time.time;

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            VentilationSystem.Update(VentilationSystem.Operation.BootImpostors, vent.Id);
        }
    }

    // Vent Network: while inside any vent, press the Right arrow to hop to the next vent and the
    // Left arrow for the previous one, cycling through EVERY vent on the map, not just physically
    // connected ones.
    // The normal directional arrows are left completely untouched, so the real connected-vent paths
    // still show and stay clickable; this only layers a keyboard shortcut on top. Vents are visited
    // in a nearest-neighbour order (each step is the closest not-yet-visited vent) so the cycle
    // sweeps the map in short hops instead of jumping around at random. The hop reuses the game's own
    // connected-vent move by briefly pointing the current vent's arrow at the target, so it travels
    // exactly like a normal vent-to-vent move. Poll this from a per-frame Update (GetKeyDown).
    public static void VentNetworkInput()
    {
        if (!CheatToggles.ventNetwork) return;

        var forward = Input.GetKeyDown(KeyCode.RightArrow);
        var backward = Input.GetKeyDown(KeyCode.LeftArrow);
        if (!forward && !backward) return;

        if (PlayerControl.LocalPlayer == null || ShipStatus.Instance == null) return;

        var current = Vent.currentVent;
        if (current == null)
        {
            float minDst = float.MaxValue;
            foreach (var v in ShipStatus.Instance.AllVents)
            {
                if (v == null) continue;
                float dst = Vector2.Distance(PlayerControl.LocalPlayer.transform.position, v.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    current = v;
                }
            }
        }

        if (current == null) return;

        var tour = BuildNearestVentTour();
        if (tour.Count < 2) return;

        var index = -1;
        for (var i = 0; i < tour.Count; i++)
        {
            if (tour[i].Id == current.Id) { index = i; break; }
        }

        if (index < 0) return;

        var step = forward ? 1 : -1;
        var target = tour[(index + step + tour.Count) % tour.Count];

        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local.inVent)
            {
                var original = current.Right;
                current.Right = target;
                current.ClickRight();
                current.Right = original;
            }
            else
            {
                local.MyPhysics.RpcEnterVent(target.Id);
                Vent.currentVent = target;
            }
        }
        catch { }
    }

    // Orders every vent into a nearest-neighbour tour: start from the first vent, then repeatedly
    // walk to the closest vent not yet visited. Consecutive entries are physically close, so cycling
    // through the tour feels like short hops around the map rather than random teleports. Recomputed
    // on demand (deterministic from vent positions, so the order stays stable between presses).
    private static List<Vent> BuildNearestVentTour()
    {
        var remaining = new List<Vent>();
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent != null) remaining.Add(vent);
        }

        var tour = new List<Vent>();
        if (remaining.Count == 0) return tour;

        var currentVent = remaining[0];
        remaining.RemoveAt(0);
        tour.Add(currentVent);

        while (remaining.Count > 0)
        {
            var here = (Vector2)currentVent.transform.position;
            var bestIndex = 0;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < remaining.Count; i++)
            {
                var distance = ((Vector2)remaining[i].transform.position - here).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            currentVent = remaining[bestIndex];
            remaining.RemoveAt(bestIndex);
            tour.Add(currentVent);
        }

        return tour;
    }

    public static void KillAllCheat()
    {
        if (!CheatToggles.killAll) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
            }
        }

        CheatToggles.killAll = false;
    }

    public static void KillAllCrewCheat()
    {
        if (!CheatToggles.killAllCrew) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.Role.TeamType == RoleTeamTypes.Crewmate)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllCrew = false;
    }

    public static void KillAllImpsCheat()
    {
        if (!CheatToggles.killAllImps) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {
            // Kill all players by sending a successful MurderPlayer RPC call
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Data.Role.TeamType == RoleTeamTypes.Impostor)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllImps = false;
    }

    public static void ProtectCheat()
    {
        if (!Utils.isHost || Utils.isLobby) return;

        foreach (var player in ProtectUI.playersToProtect)
        {
            if (player.protectedByGuardianId == -1) // -1 means no protection is currently active
            {
                //PlayerControl.LocalPlayer.TurnOnProtection(true, PlayerControl.LocalPlayer.cosmetics.ColorId, PlayerControl.LocalPlayer.PlayerId);
                PlayerControl.LocalPlayer.RpcProtectPlayer(player, PlayerControl.LocalPlayer.cosmetics.ColorId);
            }
        }
    }

    private static float _lastRightClickTpTime = 0f;

    public static void TeleportCursorCheat()
    {
        if (PlayerControl.LocalPlayer?.NetTransform == null || Camera.main == null) return;
        if (!CheatToggles.teleportCursor) return;

        // Prevent TPing when clicking over active menu GUI
        if (MalumESP.IsMouseOverActiveMenuGUI()) return;

        // Teleport player to cursor's in-world position on right-click with rate limit to prevent RPC spam kick
        if (Input.GetMouseButtonDown(1))
        {
            if (Time.time - _lastRightClickTpTime < 0.15f) return;
            _lastRightClickTpTime = Time.time;

            Vector3 mousePos = Input.mousePosition;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            MalumTeleport.TeleportTo(worldPos);
        }
    }

    public static void NoClipCheat()
    {
        try
        {

            PlayerControl.LocalPlayer.Collider.enabled = !(CheatToggles.noClip || PlayerControl.LocalPlayer.onLadder);

        } catch { }
    }

    public static void PlayScannerCheat()
    {
        if (CheatToggles.animMedScan && !_isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, true);
            _isScanAnimActive = true;
        }
        else if (!CheatToggles.animMedScan && _isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, false);
            _isScanAnimActive = false;
        }
    }

    public static void PlayAnimationCheat()
    {
        if (CheatToggles.animPet && Utils.isPlayer && PlayerControl.LocalPlayer.cosmetics != null && PlayerControl.LocalPlayer.cosmetics.CurrentPet != null)
        {
            // Don't move LocalPlayer, just send the RPC so others see the petting animation
            RpcPetMessage rpcMessage = new(PlayerControl.LocalPlayer.MyPhysics.NetId,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.PettingPlayerPosition,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.transform.position);
            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }

        byte mapId = Utils.GetCurrentMapID();

        if (mapId == byte.MaxValue) return;

        var map = (MapNames)mapId;

        if (CheatToggles.animShields)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.PrimeShields);
            }
            CheatToggles.animShields = false;
        }

        if (CheatToggles.animAsteroids)
        {
            if (map is MapNames.Skeld or MapNames.Dleks or MapNames.Polus)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.ClearAsteroids);
            }
            else
            {
                CheatToggles.animAsteroids = false;
            }
        }

        if (CheatToggles.animEmptyGarbage)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.EmptyGarbage);
            }

            CheatToggles.animEmptyGarbage = false;
        }

        if (map is not (MapNames.MiraHQ or MapNames.Fungle))
        {
            if (CheatToggles.animCamsInUse && !_isCamsAnimActive)
            {
                // ShipStatus.Instance.UpdateSystem(SystemTypes.Security, PlayerControl.LocalPlayer, (byte)(CheatToggles.animCamsInUse ? 1 : 0));
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
                _isCamsAnimActive = true;
            }
            else if (!CheatToggles.animCamsInUse && _isCamsAnimActive)
            {
                // Turn off cams if the cheat was used before and is now disabled
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
                _isCamsAnimActive = false;
            }
        }
        else
        {
            CheatToggles.animCamsInUse = false;
        }
    }

    public static void StopShipAnimCheats()
    {
        CheatToggles.animShields = false;
        CheatToggles.animAsteroids = false;
        CheatToggles.animEmptyGarbage = false;
        CheatToggles.animMedScan = false;
        CheatToggles.animCamsInUse = false;

        // This ensures cams and scan animations don't remain marked as active if the player
        // disconnects while the toggles are on (as this may cause unusual RPCs in lobbies)

        _isCamsAnimActive = false;
        _isScanAnimActive = false;
    }
}
