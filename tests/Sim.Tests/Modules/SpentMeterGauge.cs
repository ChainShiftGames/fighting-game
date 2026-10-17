using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Modules;

/// <summary>Test-only meter gauge: every fighter starts with <see cref="StartMeter"/> and keeps what isn't spent.</summary>
internal sealed class SpentMeterGauge(int id, int channel = IMeterGauge.MainChannel) : IMechanicModule, IMeterGauge
{
    public const int StartMeter = 100;

    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"gauge_{id}";

    public int ChannelOf(ref readonly ModuleConfig config) => channel;

    public int MeterOf(ModuleContext context, int fighter)
    {
        CountingState spent = context.GetState<CountingState>();
        return StartMeter - (fighter == 0 ? spent.OnHit : spent.OnBlock);
    }

    public bool TrySpend(ModuleContext context, int fighter, int amount)
    {
        if (MeterOf(context, fighter) < amount)
        {
            return false;
        }

        // The counting slice's fields stand in for meter spent: OnHit for P1, OnBlock for P2.
        ref CountingState spent = ref context.GetState<CountingState>();
        if (fighter == 0)
        {
            spent.OnHit += amount;
        }
        else
        {
            spent.OnBlock += amount;
        }

        return true;
    }
}
