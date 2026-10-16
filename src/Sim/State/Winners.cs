namespace Sim.State;

/// <summary>Which players won a round or the match: none (not decided), one of them, or both (a draw).</summary>
/// <remarks>A set of players rather than one index, so a draw needs no special value and neither port is favoured.</remarks>
public struct Winners
{
    /// <summary>Bit i set: player i (0 = P1, 1 = P2) won.</summary>
    public int Players;

    public static Winners None => default;

    public static Winners Both => new() { Players = (1 << MatchState.PlayerCount) - 1 };

    public readonly bool IsDecided => Players != 0;

    /// <summary>Both players won: a draw.</summary>
    public readonly bool IsDraw => Players == Both.Players;

    public static Winners Only(int player) => new() { Players = 1 << player };

    public readonly bool Includes(int player) => (Players & (1 << player)) != 0;
}
