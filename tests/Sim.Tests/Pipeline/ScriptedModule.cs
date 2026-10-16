using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Pipeline;

/// <summary>Test-only module whose hooks run the given actions, to drive core requests from a test.</summary>
internal sealed class ScriptedModule(int id)
    : IMechanicModule, IAfterInputHook, IAfterStateMachinesHook, IEndOfFrameHook
{
    public delegate void HookAction(ModuleContext context);

    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"scripted_{id}";

    public HookAction? OnAfterInput { get; init; }

    public HookAction? OnAfterStateMachines { get; init; }

    public HookAction? OnEndOfFrame { get; init; }

    public void AfterInput(ModuleContext context) => OnAfterInput?.Invoke(context);

    public void AfterStateMachines(ModuleContext context) => OnAfterStateMachines?.Invoke(context);

    public void EndOfFrame(ModuleContext context) => OnEndOfFrame?.Invoke(context);
}
