using HarmonyLib;

namespace MalumMenu;

// When you create a NEW / guest account, the username you choose becomes your friend code. We hook
// EditAccountUsername.SaveUsername with a Harmony prefix and overwrite the username text with a custom value
// right before it is saved.
//
// Limitations (shown to the user in the UI): this only fires on the new-account username screen, so it does
// nothing on an established Steam/Epic account, and other players can't see a guest account's friend code in
// game. It is genuinely "new/guest accounts only".
[HarmonyPatch(typeof(EditAccountUsername), nameof(EditAccountUsername.SaveUsername))]
public static class EditAccountUsername_SaveUsername
{
    public static void Prefix(EditAccountUsername __instance)
    {
        try
        {
            if (!CheatToggles.useCustomFriendCode || __instance == null || __instance.UsernameText == null) return;

            // SAFETY: only ever touch a brand-new / guest account that is choosing its code for the first
            // time. An account that already has a friend code is established (real Steam/Epic account), and
            // overwriting its username there would rename the player's own account - never do that. This
            // guard: bail when EOSManager already reports a friend code (i.e. an established account).
            if (EOSManager.Instance == null || !string.IsNullOrWhiteSpace(EOSManager.Instance.FriendCode)) return;

            // Friend codes are lower-case, <= 10 chars, no spaces. Ignore an invalid entry rather than
            // write a bad username over the player's choice.
            string code = (CheatToggles.customFriendCode ?? "").Trim().ToLowerInvariant();
            if (code.Length == 0 || code.Length > 10 || code.Contains(" ")) return;

            __instance.UsernameText.SetText(code);
        }
        catch { }
    }
}
