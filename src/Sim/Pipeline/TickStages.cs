namespace Sim.Pipeline;

/// <summary>The systems the pipeline runs, one per step of the fixed tick order (ARCHITECTURE.md §4).</summary>
/// <remarks>
/// Gameplay tasks plug their system into their step here; the pipeline itself never changes for that. Steps that
/// aren't built yet are <see cref="IdleStage"/>. Module hooks and core requests are not stages: the pipeline runs
/// them at their fixed points between these.
/// </remarks>
public sealed class TickStages
{
    public IInputStage Input { get; init; } = new SocdInputStage();

    /// <summary>Move state machines (P1-08); honours pending cancels from any state.</summary>
    public ITickStage StateMachines { get; init; } = new IdleStage();

    public ITickStage Movement { get; init; } = new IdleStage();

    /// <summary>Pushboxes, walls and camera (P1-10, P1-11).</summary>
    public ITickStage Collision { get; init; } = new IdleStage();

    public ITickStage HitDetection { get; init; } = new IdleStage();

    /// <summary>Hit resolution (P1-12); it runs the modules' hit, block and throw hooks.</summary>
    public ITickStage HitResolution { get; init; } = new IdleStage();

    /// <summary>Round and timer (P1-13); the round timer stops while <see cref="FrameActivity.IsFrozen"/>.</summary>
    public ITickStage RoundTimer { get; init; } = new IdleStage();
}
