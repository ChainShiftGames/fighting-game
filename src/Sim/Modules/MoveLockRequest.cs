using Sim.Characters;

namespace Sim.Modules;

/// <summary>Core request: the fighter can't start moves of these categories in its next state machine step.</summary>
/// <remarks>
/// E.g. supers without enough meter (P3-13). A locked move is skipped in input priority, so the input falls through
/// to the next move it matches. Locks from several modules combine. A pending cancel request isn't affected.
/// </remarks>
public struct MoveLockRequest
{
    /// <summary>Normal, special, super and throw bits only: basic moves are the state machine's own.</summary>
    public MoveCategory Categories;
}
