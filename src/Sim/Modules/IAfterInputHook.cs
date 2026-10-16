namespace Sim.Modules;

/// <summary>Optional module hook: runs after the input stage, before the move state machines.</summary>
public interface IAfterInputHook
{
    void AfterInput(ModuleContext context);
}
