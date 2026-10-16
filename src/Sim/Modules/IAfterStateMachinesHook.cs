namespace Sim.Modules;

/// <summary>Optional module hook: runs right after the move state machines.</summary>
public interface IAfterStateMachinesHook
{
    void AfterStateMachines(ModuleContext context);
}
