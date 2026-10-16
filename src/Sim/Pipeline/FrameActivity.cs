using Sim.State;

namespace Sim.Pipeline;

/// <summary>Which fighters advance this frame after freeze, hitstop and time scale.</summary>
/// <remarks>
/// Stages that move a fighter forward in time (state machines, movement) only do so for fighters that advance.
/// A fighter that doesn't advance still keeps its boxes, can still be hit and still records input.
/// </remarks>
public readonly struct FrameActivity
{
    private readonly int _advancingFighters;

    /// <param name="advancingFighters">Bit i set: fighter i advances this frame.</param>
    /// <param name="isFrozen">A global freeze is running this frame.</param>
    public FrameActivity(int advancingFighters, bool isFrozen)
    {
        _advancingFighters = advancingFighters;
        IsFrozen = isFrozen;
    }

    /// <summary>Every fighter advances and nothing is frozen: a normal frame.</summary>
    public static FrameActivity Everyone => new((1 << MatchState.PlayerCount) - 1, isFrozen: false);

    /// <summary>A global freeze is running; match-wide clocks such as the round timer stop.</summary>
    public bool IsFrozen { get; }

    public bool Advances(int fighter) => (_advancingFighters & (1 << fighter)) != 0;
}
