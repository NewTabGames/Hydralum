namespace MalumMenu;

public static class MalumSabotageCheats
{
    private static bool _reactorSab;
    private static bool _oxygenSab;
    private static bool _commsSab;
    private static bool _elecSab;
    private static bool _unfixableLights;
    private static bool _unfixableComms;

    public static void HandleReactor(ShipStatus shipStatus, byte mapId)
    {
        switch (mapId)
        {
            case 2:
            {
                // Polus uses SystemTypes.Laboratory instead of SystemTypes.Reactor

                var labSys = shipStatus.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>();

                if (CheatToggles.reactorSab != _reactorSab)
                {
                    shipStatus.RpcUpdateSystem(SystemTypes.Laboratory, _reactorSab ? (byte)16 : (byte)128);
                    _reactorSab = CheatToggles.reactorSab;
                }

                CheatToggles.reactorSab = _reactorSab = labSys.IsActive;
                break;
            }
            case 4:
            {
                // Airship uses HeliSabotageSystem to sabotage reactor

                var heliSys = shipStatus.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>();

                if (CheatToggles.reactorSab != _reactorSab)
                {
                    if (_reactorSab)
                    {
                        shipStatus.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 0); // Repair
                        shipStatus.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 1);
                    }
                    else
                    {
                        shipStatus.RpcUpdateSystem(SystemTypes.HeliSabotage, 128); // Sabotage
                    }

                    _reactorSab = CheatToggles.reactorSab;
                }

                CheatToggles.reactorSab = _reactorSab = heliSys.IsActive;
                break;
            }
            default:
            {
                // Other maps behave normally

                var reactorSys = shipStatus.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>();

                if (CheatToggles.reactorSab != _reactorSab)
                {
                    shipStatus.RpcUpdateSystem(SystemTypes.Reactor, _reactorSab ? (byte)16 : (byte)128);
                    _reactorSab = CheatToggles.reactorSab;
                }

                CheatToggles.reactorSab = _reactorSab = reactorSys.IsActive;
                break;
            }
        }
    }

    public static void HandleOxygen(ShipStatus shipStatus, byte mapId)
    {
        if (mapId != 4 && mapId != 2 && mapId != 5) { // Maps without Oxygen system: Airship, MiraHQ, Fungle

            var oxygenSys = shipStatus.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>();

            if (CheatToggles.oxygenSab != _oxygenSab)
            {
                shipStatus.RpcUpdateSystem(SystemTypes.LifeSupp, _oxygenSab ? (byte)16 : (byte)128);
                _oxygenSab = CheatToggles.oxygenSab;
            }

            CheatToggles.oxygenSab = _oxygenSab = oxygenSys.IsActive;

            return;

        }

        // Notify the player if they try to activate the cheat in a map without an oxygen system
        if (!CheatToggles.oxygenSab) return;
        HudManager.Instance.Notifier.AddDisconnectMessage("Oxygen system not present on this map");
        CheatToggles.oxygenSab = false;
    }

    public static void HandleComms(ShipStatus shipStatus, byte mapId)
    {
        var isHqHud = mapId is 1 or 5; // MiraHQ & Fungle use HqHudSystemType instead of HudOverrideSystemType

        var commsActive = isHqHud
            ? shipStatus.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive
            : shipStatus.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive;

        // Unfixable Comms: comms has no "poison" state like the electrical switches, so instead we
        // keep it down by re-triggering the sabotage the instant crew repairs it.
        if (CheatToggles.unfixableComms)
        {
            CheatToggles.commsSab = _commsSab = false; // suppress the normal Comms toggle while active

            if (!commsActive)
            {
                shipStatus.RpcUpdateSystem(SystemTypes.Comms, 128);
            }

            _unfixableComms = true;
            return;
        }

        // Unfixable Comms was just turned off → stop re-sabotaging but leave comms in its current
        // (sabotaged) state so the crew can actually repair it themselves
        if (_unfixableComms)
        {
            _unfixableComms = false;
        }

        // Normal Comms sabotage toggle
        if (CheatToggles.commsSab != _commsSab)
        {
            if (_commsSab)
            {
                RepairComms(shipStatus, isHqHud);
            }
            else
            {
                shipStatus.RpcUpdateSystem(SystemTypes.Comms, 128);
            }

            _commsSab = CheatToggles.commsSab;
        }

        CheatToggles.commsSab = _commsSab = commsActive;
    }

    private static void RepairComms(ShipStatus shipStatus, bool isHqHud)
    {
        if (isHqHud)
        {
            // The two-console (MiraHQ / Fungle) comms is repaired one console at a time
            shipStatus.RpcUpdateSystem(SystemTypes.Comms, 16 | 0);
            shipStatus.RpcUpdateSystem(SystemTypes.Comms, 16 | 1);
        }
        else
        {
            shipStatus.RpcUpdateSystem(SystemTypes.Comms, 16);
        }
    }

    public static void HandleElectrical(ShipStatus shipStatus, byte mapId)
    {
        if (mapId != 5) // Fungle has no electrical system
        {

            var elecSys = shipStatus.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();

            // Handle unfixableLights cheat first to avoid the cheats messing with each other
            HandleUnfixLights(shipStatus);

            if (CheatToggles.elecSab != _elecSab)
            {
                if (_elecSab)   // Repair
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var switchMask = 1 << (i & 0x1F);

                        if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                        {
                            shipStatus.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                        }
                    }

                }
                else // Sabotage
                {
                    CheatToggles.unfixableLights = false; // Replace unfixableLights cheat if it is already active

                    byte b = 4;
                    for (var i = 0; i < 5; i++)
                    {
                        if (BoolRange.Next(0.5f))
                        {
                            b |= (byte)(1 << i);
                        }
                    }

                    shipStatus.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128));
                }

                _elecSab = CheatToggles.elecSab;
            }

            CheatToggles.elecSab = _elecSab = elecSys.IsActive && !_unfixableLights;

            return;

        }

        // Notify the player if they try to activate the cheat in a map without an eletrical system
        if (!CheatToggles.elecSab && !CheatToggles.unfixableLights) return;

        HudManager.Instance.Notifier.AddDisconnectMessage("Electrical system not present on this map");
        CheatToggles.elecSab = CheatToggles.unfixableLights = false;
    }

    public static void HandleUnfixLights(ShipStatus shipStatus)
    {
        if (CheatToggles.unfixableLights == _unfixableLights) return;

        // Apparently most values you put for amount in RpcUpdateSystem will break lights completely
        // They are unfixable through regular means (toggling switches)
        // They can only be repaired by repeating RpcUpdateSystem with the same amount

        if (!_unfixableLights)
        {
            CheatToggles.elecSab = false;
        }

        shipStatus.RpcUpdateSystem(SystemTypes.Electrical, 69); // Repair or Sabotage

        _unfixableLights = CheatToggles.unfixableLights;
    }

    public static void HandleMushMix(ShipStatus shipStatus, byte mapId)
    {
        if (!CheatToggles.mushSab) return;

        if (mapId == 5) // MushroomMixup only works on Fungle
        {

            shipStatus.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 1); // Sabotage

        }
        else
        {
            // Notify the player if they try to activate the cheat in a map without mushrooms

            HudManager.Instance.Notifier.AddDisconnectMessage("Mushrooms not present on this map");
        }

        // Repair (bugged)
        // var mushSys = shipStatus.Systems[SystemTypes.MushroomMixupSabotage].Cast<MushroomMixupSabotageSystem>();
        // mushSys.Deteriorate(mushSys.currentSecondsUntilHeal);

        CheatToggles.mushSab = false;
    }

    public static void HandleSpores(FungleShipStatus shipStatus, byte mapId)
    {
        if (!CheatToggles.mushSpore) return;

        if (mapId == 5)
        {
            foreach (var mushroom in shipStatus.sporeMushrooms.Values)
            {
                PlayerControl.LocalPlayer.CmdCheckSporeTrigger(mushroom);
            }
        }
        else
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Mushrooms not present on this map");
        }

        CheatToggles.mushSpore = false;
    }

    public static void HandleDoors(ShipStatus shipStatus)
    {
        if (CheatToggles.closeAllDoors)
        {
            DoorsHandler.CloseAllDoors();
            CheatToggles.closeAllDoors = false;
        }
        if (CheatToggles.openAllDoors)
        {
            DoorsHandler.OpenAllDoors();
            CheatToggles.openAllDoors = false;
        }

        if (CheatToggles.spamCloseAllDoors)
        {
            DoorsHandler.CloseAllDoors();
        }
        if (CheatToggles.spamOpenAllDoors)
        {
            DoorsHandler.OpenAllDoors();
        }
    }

    public static void Process(ShipStatus shipStatus)
    {
        try
        {
            if (shipStatus == null || shipStatus.Systems == null) return;

            var currentMapID = Utils.GetCurrentMapID();

            // Disable Sabotage is a toggle: while on, it keeps clearing/blocking sabotages every tick
            if (CheatToggles.disableSabotage)
            {
                DisableAllSabotages(shipStatus, currentMapID);
            }

            if (CheatToggles.sabotageAll)
            {
                EnableAllSabotages();
                CheatToggles.sabotageAll = false;
            }

            // Handle all sabotage systems
            HandleReactor(shipStatus, currentMapID);
            HandleOxygen(shipStatus, currentMapID);
            HandleComms(shipStatus, currentMapID);
            HandleElectrical(shipStatus, currentMapID);
            HandleDoors(shipStatus);
        }
        catch { }
    }

    // Turns off every sabotage/unfixable and repairs what's fixable. Reactor/Oxygen/Lights repair
    // on their handlers' next transition (their toggles just went off); comms is repaired directly
    // because turning off Unfixable Comms deliberately leaves it broken. Mushroom Mixup can't be
    // force-repaired (its repair is bugged) but self-heals on a timer.
    private static float _lastSabotageClear;

    private static void DisableAllSabotages(ShipStatus shipStatus, byte mapId)
    {
        try
        {
            // Throttle so continuously clearing doesn't spam repair RPCs while one is still registering
            if (UnityEngine.Time.time - _lastSabotageClear < 0.5f) return;
            _lastSabotageClear = UnityEngine.Time.time;

            CheatToggles.reactorSab = false;
            CheatToggles.oxygenSab = false;
            CheatToggles.elecSab = false;
            CheatToggles.commsSab = false;
            CheatToggles.unfixableLights = false;
            CheatToggles.unfixableComms = false;
            CheatToggles.mushSab = false;
            CheatToggles.mushSpore = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;

            if (shipStatus != null && shipStatus.Systems != null && shipStatus.Systems.ContainsKey(SystemTypes.Comms))
            {
                var commsActive = (mapId is 1 or 5)
                    ? shipStatus.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive
                    : shipStatus.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive;

                if (commsActive) RepairComms(shipStatus, mapId is 1 or 5);
            }
        }
        catch { }
    }

    // Turns on Unfixable Lights, Unfixable Comms, Reactor and Oxygen at once (and, if the setting
    // is on, spam-closes all doors). The per-system handlers actually fire the sabotages.
    private static void EnableAllSabotages()
    {
        CheatToggles.unfixableLights = true;
        CheatToggles.unfixableComms = true;
        CheatToggles.reactorSab = true;
        CheatToggles.oxygenSab = true;

        if (CheatToggles.sabotageAllDoors)
            CheatToggles.spamCloseAllDoors = true;
    }

    public static void ProcessFungle(FungleShipStatus shipStatus)
    {
        try
        {
            if (shipStatus == null || shipStatus.Systems == null) return;

            var currentMapID = Utils.GetCurrentMapID();

            // Handle Fungle sabotage systems
            HandleMushMix(shipStatus, currentMapID);
            HandleSpores(shipStatus, currentMapID);
        }
        catch { }
    }
}
