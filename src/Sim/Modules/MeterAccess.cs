using System;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>Routes meter questions and spends from any module to the gauge serving that channel (<see cref="IMeterGauge"/>).</summary>
/// <remarks>
/// Built once by <see cref="ModuleLineup"/>. A lineup gauge serves its channel for both fighters; a character
/// mechanic's gauge only for its owner (ADR-0023), so mirror matches don't collide. A channel without a gauge has 0
/// meter.
/// </remarks>
internal sealed class MeterAccess
{
    private const int Routes = IMeterGauge.ChannelCount * MatchState.PlayerCount;

    // Indexed by Route(channel, fighter).
    private readonly IMeterGauge?[] _gauges = new IMeterGauge?[Routes];
    private readonly int[] _slots = new int[Routes];
    private readonly ModuleConfig[] _configs;
    private readonly CoreRequests _requests;
    private readonly ModuleCharacters? _characters;

    /// <param name="configs">Every slot's config, indexed by slot.</param>
    /// <exception cref="ArgumentException">A gauge names a channel out of range, or two serve one channel for a fighter.</exception>
    public MeterAccess(
        IMechanicModule?[] modulesBySlot, ModuleConfig[] configs, CoreRequests requests, ModuleCharacters? characters)
    {
        _configs = configs;
        _requests = requests;
        _characters = characters;
        var gauges = new HookList<IMeterGauge>(modulesBySlot);
        for (int i = 0; i < gauges.Count; i++)
        {
            int slot = gauges.SlotAt(i);
            int channel = gauges.HookAt(i).ChannelOf(in configs[slot]);
            if (channel < 0 || channel >= IMeterGauge.ChannelCount)
            {
                throw new ArgumentException($"The gauge in slot {slot} names meter channel {channel}.", nameof(modulesBySlot));
            }

            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                if (Serves(slot, fighter))
                {
                    AddRoute(gauges.HookAt(i), slot, channel, fighter);
                }
            }
        }
    }

    public int MeterOf(ref MatchState state, int fighter, int channel)
    {
        int route = Route(channel, fighter);
        return _gauges[route] is { } gauge ? gauge.MeterOf(GaugeContext(ref state, route), fighter) : 0;
    }

    public bool TrySpend(ref MatchState state, int fighter, int amount, int channel)
    {
        int route = Route(channel, fighter);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Meter can't be spent negatively.");
        }

        return _gauges[route] is { } gauge && gauge.TrySpend(GaugeContext(ref state, route), fighter, amount);
    }

    private static bool Serves(int slot, int fighter) =>
        slot < ModuleStates.LineupSlots || slot == ModuleStates.MechanicSlot(fighter);

    private static int Route(int channel, int fighter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, IMeterGauge.ChannelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(fighter);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fighter, MatchState.PlayerCount);
        return (channel * MatchState.PlayerCount) + fighter;
    }

    private void AddRoute(IMeterGauge gauge, int slot, int channel, int fighter)
    {
        int route = Route(channel, fighter);
        if (_gauges[route] is not null)
        {
            throw new ArgumentException(
                $"The gauges in slots {_slots[route]} and {slot} both serve meter channel {channel} for fighter {fighter}.");
        }

        _gauges[route] = gauge;
        _slots[route] = slot;
    }

    private ModuleContext GaugeContext(ref MatchState state, int route)
    {
        int slot = _slots[route];
        return new(ref state, ref state.Modules[slot], in _configs[slot], _requests, slot, this, _characters);
    }
}
