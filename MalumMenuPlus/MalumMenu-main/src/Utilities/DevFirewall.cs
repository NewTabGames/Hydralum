using System;
using UnityEngine;

namespace MalumMenu;

public static class DevFirewall
{
    [ThreadStatic]
    private static bool _isProcessingRemoteRpc = false;

    public static bool IsProcessingRemoteRpc
    {
        get => _isProcessingRemoteRpc;
        set => _isProcessingRemoteRpc = value;
    }

    public static void ResetProcessingState()
    {
        _isProcessingRemoteRpc = false;
    }

    public static bool IsLocalPlayerDev()
    {
        try
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
            return PresenceTracker.IsDevUser(PlayerControl.LocalPlayer.Data);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsTargetDev(PlayerControl target)
    {
        if (target == null) return false;
        if (target.Data != null) return PresenceTracker.IsDevUser(target.Data);
        return PresenceTracker.IsDevUser(target);
    }

    public static bool IsTargetDev(NetworkedPlayerInfo targetData)
    {
        if (targetData == null) return false;
        return PresenceTracker.IsDevUser(targetData);
    }

    public static bool IsAuthorizedSender(PlayerControl sender)
    {
        try
        {
            // If this is not a remote RPC (e.g. local cheat action or local menu button), always authorized
            if (!_isProcessingRemoteRpc)
            {
                return true;
            }

            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return true;

            // Sender is local player (self-initiated action)
            if (sender != null && (sender == PlayerControl.LocalPlayer || sender.AmOwner))
            {
                return true;
            }

            // If LocalPlayer is Host, any remote sender attempting an action on LocalPlayer is unauthorized
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            // If LocalPlayer is not Host, check if sender is the authorized Host
            if (AmongUsClient.Instance != null && sender != null)
            {
                if (sender.OwnerId == AmongUsClient.Instance.HostId)
                {
                    return true;
                }

                var hostClient = AmongUsClient.Instance.GetHost();
                var senderClient = AmongUsClient.Instance.GetClientFromCharacter(sender);
                if (hostClient != null && senderClient != null && hostClient.Id == senderClient.Id)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool ShouldBlockOutboundAction(NetworkedPlayerInfo target)
    {
        try
        {
            if (target != null && PresenceTracker.IsDevUser(target))
            {
                if (target.Object == null || !target.Object.AmOwner)
                {
                    NotifyDevTargetBlocked();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public static bool ShouldBlockOutboundAction(PlayerControl target)
    {
        try
        {
            if (target != null && PresenceTracker.IsDevUser(target))
            {
                if (!target.AmOwner)
                {
                    NotifyDevTargetBlocked();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public static void NotifyDevTargetBlocked()
    {
        try
        {
            HudManager.Instance?.Notifier?.AddDisconnectMessage("Cannot target Developer");
        }
        catch { }
    }
}
