namespace FightingGame.UI.SideSelect;

/// <summary>Which sides a mode needs a device on: versus both, training only player 1 (player 2 is the dummy).</summary>
public sealed class SideRules
{
    private readonly SideRole[] _roles;

    private SideRules(SideRole player1, SideRole player2)
    {
        _roles = [player1, player2];
        PlayerCount = player2 == SideRole.Player ? 2 : 1;
    }

    public static SideRules Versus { get; } = new(SideRole.Player, SideRole.Player);

    public static SideRules Training { get; } = new(SideRole.Player, SideRole.Dummy);

    /// <summary>How many players the match reads devices for: the sides a device plays.</summary>
    public int PlayerCount { get; }

    /// <param name="side">0 for player 1, 1 for player 2.</param>
    public SideRole RoleOf(int side) => _roles[side];
}
