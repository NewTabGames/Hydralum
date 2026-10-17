using UnityEngine;

namespace MalumMenu;

public class LevelTab : ITab
{
    public string name => "Level";

    public void Draw()
    {
        GUILayout.Label("Shield Method", GUIStylePreset.TabSubtitle);

        GUILayout.Space(6);

        if (!Utils.isHost)
            GUILayout.Label("<color=#FFCC55>You must be the host for this to do anything.</color>", GUIStylePreset.Hint);

        CheatToggles.levelStrobe = GUILayout.Toggle(CheatToggles.levelStrobe, " Enable");

        GUILayout.Space(10);

        float rate = Mathf.Clamp(CheatToggles.levelKillsPerSecond, 1f, MalumLevelStrobe.MaxKillsPerSecond);
        GUILayout.Label($"Kills per second: {rate:F0}  (max {MalumLevelStrobe.MaxKillsPerSecond:F0})", GUIStylePreset.Hint);
        CheatToggles.levelKillsPerSecond = GUILayout.HorizontalSlider(rate, 1f, MalumLevelStrobe.MaxKillsPerSecond, GUILayout.Width(250f));

        GUILayout.Space(6);
        GUILayout.Label("The more fps you have, the quicker you can kill.", GUIStylePreset.Hint);

        GUILayout.Space(14);
        GUILayout.Label("Auto Loop", GUIStylePreset.TabSubtitle);
        GUILayout.Label("In order to start, you must be in a private lobby as the host.", GUIStylePreset.Hint);

        GUILayout.Space(6);
        CheatToggles.levelLoop = GUILayout.Toggle(CheatToggles.levelLoop, " Enable Auto Loop");

        GUILayout.Space(8);
        float dur = Mathf.Clamp(CheatToggles.levelLoopDuration, MalumLevelLoop.MinDuration, MalumLevelLoop.MaxDuration);
        GUILayout.Label($"Shield duration per cycle: {dur:F0}s  ({MalumLevelLoop.MinDuration:F0}-{MalumLevelLoop.MaxDuration:F0}s)", GUIStylePreset.Hint);
        CheatToggles.levelLoopDuration = GUILayout.HorizontalSlider(dur, MalumLevelLoop.MinDuration, MalumLevelLoop.MaxDuration, GUILayout.Width(250f));
    }
}
