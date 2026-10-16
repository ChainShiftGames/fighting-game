namespace Sim.Modules;

/// <summary>Optional module hook: runs right before the move state machines.</summary>
public interface IBeforeStateMachinesHook
{
    void BeforeStateMachines(ModuleContext context);
}
