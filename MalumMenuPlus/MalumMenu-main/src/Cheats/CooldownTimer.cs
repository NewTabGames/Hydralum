namespace MalumMenu;

// Shared gate for the cooldown-ESP estimators (Kill, Guardian Angel, Advanced Role). Kill/ability cooldowns
// only tick while a player can move - they pause during meetings, ejections, vents, task minigames and the
// map screen. CanMove is the game's own role-neutral flag for exactly that state, so the estimators freeze a
// player's countdown whenever it's false, instead of counting down in real time regardless.
public static class CooldownTimer
{
    public static bool CanAct(PlayerControl pc)
    {
        try { return pc != null && pc.CanMove; }
        catch { return true; }
    }
}
