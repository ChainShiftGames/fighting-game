namespace Sim.State;

/// <summary>A cancel the core accepted for one fighter; the move state machine honours it from any state.</summary>
public struct PendingCancel
{
    /// <summary>1 while a cancel waits for the state machine, else 0.</summary>
    public int IsPending;

    /// <summary>Index of the move in the fighter's move list.</summary>
    public int TargetMove;

    public int Priority;
}
