namespace FightingGame.UI.SideSelect;

/// <summary>The three columns of side select, left to right; left and right move a device one column.</summary>
public enum SideColumn
{
    Player1,

    /// <summary>The middle: the device sits this match out.</summary>
    NotPlaying,

    Player2,
}
