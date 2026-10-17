using System;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>Routes meter questions and spends from any module to the lineup's meter gauge (<see cref="IMeterGauge"/>).</summary>
/// <remarks>Built once by <see cref="ModuleLineup"/>; without a gauge in the lineup every fighter has 0 meter.</remarks>
internal sealed class MeterAccess
{
    private readonly IMeterGauge? _gauge;
    private readonly int _slot;
    private readonly CoreRequests _requests;
    private readonly ModuleConfig _config;

    public MeterAccess(IMeterGauge? gauge, int slot, in ModuleConfig config, CoreRequests requests)
    {
        _gauge = gauge;
        _slot = slot;
        _config = config;
        _requests = requests;
    }

    public int MeterOf(ref MatchState state, int fighter) =>
        _gauge is null ? 0 : _gauge.MeterOf(GaugeContext(ref state), fighter);

    public bool TrySpend(ref MatchState state, int fighter, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Meter can't be spent negatively.");
        }

        return _gauge is not null && _gauge.TrySpend(GaugeContext(ref state), fighter, amount);
    }

    private ModuleContext GaugeContext(ref MatchState state) =>
        new(ref state, ref state.Modules[_slot], in _config, _requests, _slot, this);
}
