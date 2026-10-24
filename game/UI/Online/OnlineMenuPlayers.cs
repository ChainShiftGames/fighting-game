using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Online;

/// <summary>
/// Online, this PC's one player owns every device the online assignment gives them (ADR-0038), on whichever side the
/// lobby put them; the other side is the other PC's.
/// </summary>
public sealed class OnlineMenuPlayers : IMenuPlayers
{
    private const int OnlyPlayer = 0;

    private readonly IMenuPlayers _onThisPc;
    private readonly int _localSide;

    /// <param name="onThisPc">The devices of the one player on this PC, as player 0.</param>
    /// <param name="localSide">The side this PC plays (0: P1, the host).</param>
    public OnlineMenuPlayers(IMenuPlayers onThisPc, int localSide)
    {
        _onThisPc = onThisPc;
        _localSide = localSide;
    }

    public int PlayerOf(MenuDevice device) => _onThisPc.PlayerOf(device) == OnlyPlayer ? _localSide : -1;

    public bool CanPress(int player) => player == _localSide && _onThisPc.CanPress(OnlyPlayer);
}
