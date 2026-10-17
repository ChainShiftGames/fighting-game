using Sim.Modules;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Whether one fighter may take a cancel its move's data window offers, this frame.</summary>
/// <remarks>
/// In a match the modules decide (<see cref="ICancelWindowHook"/>), so with no module allowing it, data windows
/// never cancel. <see cref="EveryWindow"/> skips the modules, for driving a state machine on its own (unit tests).
/// A ref struct so it can carry the match state the modules read; made fresh for each fighter's step.
/// </remarks>
public readonly ref struct CancelPermission
{
    private readonly ref MatchState _state;
    private readonly ModuleLineup? _modules;

    /// <param name="state">The match as the state machine step started, for the modules' read-only view.</param>
    /// <param name="modules">The match's modules.</param>
    /// <param name="fighter">The fighter being advanced: 0 or 1.</param>
    public CancelPermission(ref MatchState state, ModuleLineup modules, int fighter)
    {
        _state = ref state;
        _modules = modules;
        Fighter = fighter;
    }

    /// <summary>Every data window can be taken; no modules are asked.</summary>
    public static CancelPermission EveryWindow => default;

    public int Fighter { get; }

    public bool Allows(in CancelCheck check) => _modules is null || _modules.AllowsCancel(ref _state, in check);
}
