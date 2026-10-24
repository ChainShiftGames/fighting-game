namespace FightingGame.UI.SideSelect;

/// <summary>What a side is for in the mode being set up.</summary>
public enum SideRole
{
    /// <summary>A device plays it, and it must be ready before the screen moves on.</summary>
    Player,

    /// <summary>Training's dummy: no device can take it (training reads one player's devices).</summary>
    Dummy,
}
