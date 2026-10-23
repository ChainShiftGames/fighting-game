using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>Which player on this PC each menu device belongs to, for screens both players use at once.</summary>
public interface IMenuPlayers
{
    /// <summary>The player (0 or 1) <paramref name="device"/> belongs to, or -1 when it is nobody's.</summary>
    int PlayerOf(MenuDevice device);

    /// <summary>Whether <paramref name="player"/> has a connected device to press with.</summary>
    bool CanPress(int player);
}
