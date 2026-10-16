namespace Sim.Modules;

/// <summary>Core request: cancel a fighter's current state into a move, from any state (ADR-0011).</summary>
public struct CancelRequest
{
    /// <summary>Index of the move in the fighter's move list.</summary>
    public int TargetMove;

    /// <summary>Higher wins when requests for the same fighter conflict; ties go to the earlier module slot.</summary>
    public int Priority;
}
