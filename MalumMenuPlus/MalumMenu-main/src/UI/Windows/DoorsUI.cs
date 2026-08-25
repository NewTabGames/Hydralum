using UnityEngine;
using Il2CppSystem.Collections.Generic;

namespace MalumMenu;

public class DoorsUI : MonoBehaviour
{
    public static int windowHeight = 270;
    public static int windowWidth = 480;
    public static Rect windowRect;

    private List<SystemTypes> _doorsToSpamOpen = new();
    private List<SystemTypes> _doorsToSpamClose = new();
    private readonly System.Collections.Generic.Dictionary<int, float> _lastCloseTime = new();

    private void Start()
    {
        // Instantiate 2D area of DoorsUI
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        bool keepOpen = MalumMenu.menuKeepSubwindowsOpen?.Value ?? false;
        if (!CheatToggles.showDoorsMenu || !(MenuUI.isGUIActive || keepOpen) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        windowRect = GUI.Window((int)WindowId.DoorsUI, windowRect, (GUI.WindowFunction)DoorsWindow, "Doors");
    }

    private void DoorsWindow(int windowID)
    {
        try
        {
            if (!Utils.isShip)
            {
                GUI.DragWindow();
                return;
            }

            var map = (MapNames)Utils.GetCurrentMapID();

            if (map is MapNames.MiraHQ)
            {
                GUI.DragWindow();
                return;
            }

            GUILayout.BeginVertical();

        foreach (var doorRoom in DoorsHandler.GetRoomsWithDoors())
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label($"{doorRoom.ToString()}", GUILayout.Width(110f));

            GUILayout.BeginHorizontal();

            GUILayout.Label($"{DoorsHandler.GetStatusOfDoorsInRoom(doorRoom, true)}");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", GUIStylePreset.NormalButton, GUILayout.Width(50f)))
            {
                DoorsHandler.CloseDoorsInRoom(doorRoom);
            }

            if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
            {
                if (GUILayout.Button("Open", GUIStylePreset.NormalButton, GUILayout.Width(50f)))
                {
                    DoorsHandler.OpenDoorsInRoom(doorRoom);
                }
            }

            // Spam Close is available to everyone — if you can close a door, you can keep re-closing it.
            // The re-close loop (see Update) only fires once a door has actually reopened, so it holds
            // doors shut without flooding the host every frame.
            var spamClose = _doorsToSpamClose.Contains(doorRoom);
            spamClose = GUILayout.Toggle(spamClose, "Spam Close", GUIStylePreset.NormalToggle);

            if (spamClose && !_doorsToSpamClose.Contains(doorRoom))
            {
                _doorsToSpamClose.Add(doorRoom);
            }
            else if (!spamClose && _doorsToSpamClose.Contains(doorRoom))
            {
                _doorsToSpamClose.Remove(doorRoom);
            }

            if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
            {
                var spamOpen = _doorsToSpamOpen.Contains(doorRoom);
                spamOpen = GUILayout.Toggle(spamOpen, "Spam Open", GUIStylePreset.NormalToggle);

                if (spamOpen && !_doorsToSpamOpen.Contains(doorRoom))
                {
                    _doorsToSpamOpen.Add(doorRoom);
                }
                else if (!spamOpen && _doorsToSpamOpen.Contains(doorRoom))
                {
                    _doorsToSpamOpen.Remove(doorRoom);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
        GUILayout.Space(1f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Close All", GUIStylePreset.NormalButton))
        {
            CheatToggles.closeAllDoors = true;
        }

        if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
        {
            if (GUILayout.Button("Open All", GUIStylePreset.NormalButton))
            {
                CheatToggles.openAllDoors = true;
            }
        }

        GUILayout.FlexibleSpace();

        // Spam Close All — available to everyone; drives the same throttled re-close as the per-room toggles
        var allRooms = DoorsHandler.GetRoomsWithDoors();
        var spamCloseAll = allRooms.Count > 0;
        foreach (var r in allRooms)
        {
            if (!_doorsToSpamClose.Contains(r))
            {
                spamCloseAll = false;
                break;
            }
        }

        var newSpamCloseAll = GUILayout.Toggle(spamCloseAll, "Spam Close All", GUIStylePreset.NormalToggle);
        if (newSpamCloseAll && !spamCloseAll)
        {
            foreach (var r in allRooms)
            {
                if (!_doorsToSpamClose.Contains(r)) _doorsToSpamClose.Add(r);
            }
        }
        else if (!newSpamCloseAll && spamCloseAll)
        {
            _doorsToSpamClose.Clear();
        }

        // Spam Open All stays host-only
        if (Utils.isHost && (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle))
        {
            CheatToggles.spamOpenAllDoors = GUILayout.Toggle(CheatToggles.spamOpenAllDoors, "Spam Open All", GUIStylePreset.NormalToggle);
        }
        else
        {
            CheatToggles.spamOpenAllDoors = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        }
        catch { }

        GUI.DragWindow();
    }

    public void Update()
    {
        if (!Utils.isShip)
        {
            // Don't carry spam selections across games
            if (_doorsToSpamClose.Count != 0) _doorsToSpamClose.Clear();
            if (_doorsToSpamOpen.Count != 0) _doorsToSpamOpen.Clear();
            return;
        }

        // Re-close selected doors, but only once a door has actually reopened (and at most a few
        // times per second), so a non-host doesn't flood the host with door RPCs.
        foreach (var doorRoom in _doorsToSpamClose)
        {
            if (ShouldReclose(doorRoom))
            {
                DoorsHandler.CloseDoorsInRoom(doorRoom);
            }
        }

        // Spam open selected doors
        var map = (MapNames)Utils.GetCurrentMapID();

        if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
        {
            foreach (var doorRoom in _doorsToSpamOpen)
            {
                DoorsHandler.OpenDoorsInRoom(doorRoom);
            }
        }
    }

    // True only when a door in the room is open and we haven't just tried to close it
    private bool ShouldReclose(SystemTypes room)
    {
        var anyOpen = false;
        foreach (var door in DoorsHandler.GetDoorsInRoom(room))
        {
            if (door.IsOpen)
            {
                anyOpen = true;
                break;
            }
        }

        if (!anyOpen) return false;

        // Throttle so one reopen doesn't cause a burst of RPCs while the close is still in flight
        if (_lastCloseTime.TryGetValue((int)room, out var last) && Time.time - last < 0.35f) return false;

        _lastCloseTime[(int)room] = Time.time;
        return true;
    }
}
