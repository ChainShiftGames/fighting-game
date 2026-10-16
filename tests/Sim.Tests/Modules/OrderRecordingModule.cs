using System.Collections.Generic;
using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Modules;

/// <summary>Writes its id to a shared log on two hooks, to observe the order modules run in.</summary>
internal sealed class OrderRecordingModule(int id, List<string> log) : IMechanicModule, IAfterInputHook,
    IEndOfFrameHook
{
    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"order_{id}";

    public void AfterInput(ModuleContext context) => log.Add($"{nameof(AfterInput)} {id}");

    public void EndOfFrame(ModuleContext context) => log.Add($"{nameof(EndOfFrame)} {id}");
}
