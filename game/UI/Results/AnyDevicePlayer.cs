using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Results;

/// <summary>One chooser who presses with any device: after a replay, where nobody on this PC played.</summary>
public sealed class AnyDevicePlayer : IMenuPlayers
{
    public int PlayerOf(MenuDevice device) => 0;

    public bool CanPress(int player) => player == 0;
}
