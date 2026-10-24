namespace FightingGame.UI.SideSelect;

/// <summary>Between a side (0 for player 1, 1 for player 2) and its column.</summary>
public static class SideSelectColumns
{
    public static SideColumn Of(int side) => side == 0 ? SideColumn.Player1 : SideColumn.Player2;

    /// <summary>0 or 1 for a side's column; -1 for the middle.</summary>
    public static int SideOf(SideColumn column) => column switch
    {
        SideColumn.Player1 => 0,
        SideColumn.Player2 => 1,
        _ => -1,
    };
}
