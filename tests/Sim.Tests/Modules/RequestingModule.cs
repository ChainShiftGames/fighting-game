using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Modules;

/// <summary>Makes one core request of each kind after input, valued from its config, for player 2.</summary>
internal sealed class RequestingModule(int id) : IMechanicModule, IAfterInputHook
{
    public const int Fighter = 1;

    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"requesting_{id}";

    public void AfterInput(ModuleContext context)
    {
        int value = context.GetConfig<TestConfig>().Value;
        context.RequestCancel(Fighter, new CancelRequest { TargetMove = value, Priority = value });
        context.RequestTimeScale(Fighter, new TimeScaleRequest { Numerator = 1, Denominator = value, Frames = value });
        context.RequestHitstopOverride(Fighter, new HitstopOverrideRequest { Frames = value });
        context.RequestFreeze(new FreezeRequest { Frames = value, ExemptFighters = 1 << Fighter });
    }
}
