namespace Sim.Modules;

/// <summary>Optional module question: may a fighter take a cancel its move's data window offers (gatlings)?</summary>
/// <remarks>
/// The core asks in the move state machines, only for a cancel the fighter input while a window from character data
/// is open; it cancels when any enabled module says yes, so with none, moves never cancel through their data windows.
/// Cancels modules request (<see cref="CancelRequest"/>) are not asked about. A question, not a hook point: the
/// answer must depend only on the check, the read-only state and the config, and the module must not change its
/// state slice or make requests here.
/// </remarks>
public interface ICancelWindowHook
{
    bool CanCancel(ModuleContext context, in CancelCheck check);
}
