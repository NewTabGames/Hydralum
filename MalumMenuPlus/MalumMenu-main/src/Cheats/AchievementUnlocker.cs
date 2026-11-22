using System;
using AmongUs.GameOptions;

namespace MalumMenu;

// Ported from SickoMenu (g0aty, GPL-3.0). AchievementManager keeps a static map of achievement-key ->
// the game modes that achievement counts in. We walk every entry, make sure it counts for both Normal and
// Hide-n-Seek (mirrors SickoMenu, so mode-gated achievements still unlock), then call UnlockAchievement,
// which writes the unlock up to the EOS stats backend. Requires being logged in (AchievementManager exists).
public static class AchievementUnlocker
{
    public static void UnlockAll()
    {
        try
        {
            var manager = AchievementManager.Instance;
            if (manager == null)
            {
                ConsoleUI.Log("Unlock Achievements: unavailable (you must be logged in).");
                return;
            }

            var map = AchievementManager.AchievementGameModeKey;
            if (map == null)
            {
                ConsoleUI.Log("Unlock Achievements: achievement list unavailable.");
                return;
            }

            int count = 0;
            foreach (var key in map.Keys)
            {
                try
                {
                    var modes = map[key];
                    if (modes != null)
                    {
                        if (!modes.Contains(GameModes.Normal)) modes.Add(GameModes.Normal);
                        if (!modes.Contains(GameModes.HideNSeek)) modes.Add(GameModes.HideNSeek);
                    }
                    manager.UnlockAchievement(key);
                    count++;
                }
                catch { }
            }

            ConsoleUI.Log($"Unlocked {count} achievements.");
        }
        catch (Exception e)
        {
            ConsoleUI.Log($"Unlock Achievements failed: {e.Message}");
        }
    }
}
