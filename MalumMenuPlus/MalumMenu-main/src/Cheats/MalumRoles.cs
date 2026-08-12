using AmongUs.GameOptions;

namespace MalumMenu;

// Role features adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
public static class MalumRoles
{
    // Change your own role. Client-side by default (only you see it); as host we also send the
    // SetRole RPC so the change syncs to everyone. Mirrors Hydra's UpdateRole.
    public static void ChangeRole(RoleTypes role)
    {
        if (!Utils.isPlayer)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("You must be in a game to change your role");
            return;
        }

        bool isGhost = RoleManager.IsGhostRole(role);

        // CoSetRole (which SetRole calls) toggles the report button when swapping to/from a ghost
        // role, but calling SetRole directly skips that, so we fix the report button up ourselves.
        HudManager.Instance.ReportButton.gameObject.SetActive(!isGhost);

        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, role);

        if (Utils.isHost)
        {
            // As host we can sync the new role to everyone else.
            PlayerControl.LocalPlayer.RpcSetRole(role, true);
        }

        HudManager.Instance.Notifier.AddDisconnectMessage($"Your role is now {role}{(Utils.isHost ? "" : " (local only)")}");
    }
}
