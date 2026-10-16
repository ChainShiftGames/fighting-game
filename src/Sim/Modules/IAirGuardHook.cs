namespace Sim.Modules;

/// <summary>Optional module question: may an airborne fighter block this attack (air block, P1-24)?</summary>
/// <remarks>
/// The core asks during hit resolution, only for an attack touching an airborne defender that is free to guard and
/// holds back; it blocks when any enabled module says yes, so with none airborne fighters can't block. A question,
/// not a hook point: the answer must depend only on the check, the read-only state and the config, and the module
/// must not change its state slice or make requests here.
/// </remarks>
public interface IAirGuardHook
{
    bool CanAirGuard(ModuleContext context, in AirGuardCheck check);
}
