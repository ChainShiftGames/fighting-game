using Sim.Input;
using Sim.Modules;
using Sim.State;

namespace Sim.Pipeline;

/// <summary>Runs one logic frame: the stages in the fixed order, with module hooks and core requests between.</summary>
/// <remarks>
/// Order (ARCHITECTURE.md §4, ADR-0011):
/// input → after-input hooks → before-state-machine hooks → apply requests → time control → state machines →
/// after-state-machine hooks → movement → collision (pushboxes, walls, camera) → hit detection → hit resolution
/// (hit, block and throw hooks) → end-of-frame hooks (the mechanics step) → apply requests → round and timer.
/// Requests made before the state machines take effect this frame; later ones from the next frame. The pipeline only
/// sequences; every rule lives in a stage, <see cref="TimeControl"/> or <see cref="CoreRequestApplier"/>.
/// Built once at match start; a tick allocates nothing.
/// </remarks>
public sealed class TickPipeline
{
    private readonly IInputStage _input;
    private readonly ITickStage _stateMachines;
    private readonly ITickStage _movement;
    private readonly ITickStage _collision;
    private readonly ITickStage _hitDetection;
    private readonly ITickStage _hitResolution;
    private readonly ITickStage _roundTimer;
    private readonly ModuleLineup _modules;
    private readonly CoreRequestApplier _requests;
    private readonly TimeControl _timeControl = new();

    /// <param name="stages">The systems for each step.</param>
    /// <param name="modules">The enabled modules; built with the same <paramref name="requests"/>.</param>
    /// <param name="requests">The request tables the modules write to.</param>
    public TickPipeline(TickStages stages, ModuleLineup modules, CoreRequests requests)
    {
        _input = stages.Input;
        _stateMachines = stages.StateMachines;
        _movement = stages.Movement;
        _collision = stages.Collision;
        _hitDetection = stages.HitDetection;
        _hitResolution = stages.HitResolution;
        _roundTimer = stages.RoundTimer;
        _modules = modules;
        _requests = new CoreRequestApplier(requests);
    }

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        _input.Run(ref state, player1, player2);
        _modules.RunAfterInput(ref state);
        _modules.RunBeforeStateMachines(ref state);
        _requests.Apply(ref state);

        FrameActivity activity = _timeControl.BeginFrame(ref state);
        _stateMachines.Run(ref state, in activity);
        _modules.RunAfterStateMachines(ref state);
        _movement.Run(ref state, in activity);
        _collision.Run(ref state, in activity);
        _hitDetection.Run(ref state, in activity);
        _hitResolution.Run(ref state, in activity);
        _modules.RunEndOfFrame(ref state);
        _requests.Apply(ref state);
        _roundTimer.Run(ref state, in activity);

        state.Frame++;
    }
}
