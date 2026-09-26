using UnityEngine;

namespace MalumMenu;

// Chat Sender: sends a typed message on demand, or repeatedly ("Spam") on a fixed interval. Uses the same
// low-level send as the mod's URL-bypass path (PlayerControl.RpcSendChat). Spam is driven from
// HudManager.Update (ChatSender.Tick), so it keeps firing while the menu is closed - but only in a lobby/
// game where LocalPlayer exists.
public static class ChatSender
{
    public const int MaxLength = 100; // vanilla free-chat cap; longer strings risk being rejected/truncated
    public const float MinInterval = 0.0001f;
    public const float MaxInterval = 60f;

    public static string Message = "";
    public static bool Spam = false;
    public static float Interval = MinInterval; // seconds, clamped to [MinInterval, MaxInterval]

    // Timestamp of the last spam send. Negative = "not spamming yet", so enabling Spam fires immediately.
    private static float _lastSendTime = -999f;

    public static void SendOnce()
    {
        if (string.IsNullOrWhiteSpace(Message)) return;
        try
        {
            var lp = PlayerControl.LocalPlayer;
            if (lp == null) return;
            lp.RpcSendChat(Message);
        }
        catch { }
    }

    // Called every frame from HudManager.Update. Sends immediately when Spam is switched on, then repeats
    // every Interval seconds. The gap is measured from the last send each frame, so dragging the interval
    // slider while spamming takes effect right away instead of only after the next (old-interval) send.
    public static void Tick()
    {
        if (!Spam)
        {
            _lastSendTime = -999f; // reset so re-enabling fires at once
            return;
        }

        if (string.IsNullOrWhiteSpace(Message)) return;

        float interval = Mathf.Clamp(Interval, MinInterval, MaxInterval);
        if (_lastSendTime < 0f || Time.time - _lastSendTime >= interval)
        {
            SendOnce();
            _lastSendTime = Time.time;
        }
    }
}
