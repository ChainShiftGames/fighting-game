namespace Sim.Modules;

/// <summary>Core request: cancel a fighter's current state into a move or into neutral, from any state (ADR-0011).</summary>
public struct CancelRequest
{
    /// <summary>
    /// <see cref="TargetMove"/> for a cancel into neutral: the fighter is free again at once, standing, crouching,
    /// walking or airborne as its held direction and position say, and a buffered press can start a move (Roman Cancel).
    /// </summary>
    public const int Neutral = -1;

    /// <summary>Index of the move in the fighter's move list, or <see cref="Neutral"/>.</summary>
    public int TargetMove;

    /// <summary>Higher wins when requests for the same fighter conflict; ties go to the earlier module slot.</summary>
    public int Priority;
}
