using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace MalumMenu;

// Game-options plumbing adapted from Hydra (https://github.com/MrDiamond64/Hydra), GPL-3.0.
// Lets the host push a modified copy of the game options to a single client (used for the per-player
// grief buttons and the shapeshift-ratelimit bypass) without touching the real lobby settings.
public static class MalumGameOptions
{
    // A fresh, independent copy of the current game options (serialize out and back in to detach it).
    public static IGameOptions CloneCurrent()
    {
        var factory = GameManager.Instance.LogicOptions.gameOptionsFactory;
        var current = GameOptionsManager.Instance.CurrentGameOptions;
        var bytes = factory.ToBytes(current, AprilFoolsMode.IsAprilFoolsModeToggledOn);
        return factory.FromBytes(bytes);
    }

    // Send a game-options update to one client only. In Freeplay there is no network layer, so apply
    // locally. Otherwise hand-build a GameDataTo -> DataFlag carrying the LogicOptions component update
    // (StartRpcImmediately only makes RPC messages, so we construct the data message by hand).
    public static void SendToClient(IGameOptions options, int targetClientId)
    {
        if (options == null) return;

        // Keep the shapeshift-ratelimit bypass alive whenever we push any options update.
        if (CheatToggles.shapeshiftBypass)
        {
            options.SetFloat(FloatOptionNames.ShapeshifterCooldown, 0f);
        }

        if (Utils.isFreePlay && targetClientId == PlayerControl.LocalPlayer.OwnerId)
        {
            GameManager.Instance.LogicOptions.SetGameOptions(options);
            return;
        }

        int logicIndex = FindLogicOptionsIndex();
        if (logicIndex < 0) return;

        var inner = MessageWriter.Get(SendOption.Reliable);
        inner.StartMessage((byte)logicIndex);
        inner.WriteBytesAndSize(GameManager.Instance.LogicOptions.gameOptionsFactory.ToBytes(options, AprilFoolsMode.IsAprilFoolsModeToggledOn));
        inner.EndMessage();

        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage((byte)Tags.GameDataTo);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(targetClientId);

        // 1 = DataFlag (a component data update) within a GameData/GameDataTo message.
        writer.StartMessage(1);
        writer.WritePacked(GameManager.Instance.NetId);
        writer.Write(inner, false);
        writer.EndMessage();

        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);

        writer.Recycle();
        inner.Recycle();
    }

    // GameManager stores its logic pieces in LogicComponents; the LogicOptions one owns the settings,
    // and its list index is the sub-message tag we serialize the options under.
    private static int FindLogicOptionsIndex()
    {
        var components = GameManager.Instance.LogicComponents;
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i] != null && components[i].TryCast<LogicOptions>() != null) return i;
        }

        return -1;
    }
}
