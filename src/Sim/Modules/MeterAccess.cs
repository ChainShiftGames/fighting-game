using System;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>Routes meter questions and spends from any module to the gauge serving that channel (<see cref="IMeterGauge"/>).</summary>
/// <remarks>Built once by <see cref="ModuleLineup"/>; a channel without a gauge has 0 meter for every fighter.</remarks>
internal sealed class MeterAccess
{
    private readonly IMeterGauge?[] _gauges = new IMeterGauge?[IMeterGauge.ChannelCount];
    private readonly int[] _slots = new int[IMeterGauge.ChannelCount];
    private readonly ModuleConfig[] _configs = new ModuleConfig[IMeterGauge.ChannelCount];
    private readonly CoreRequests _requests;
    private readonly ModuleCharacters? _characters;

    /// <exception cref="ArgumentException">A gauge names a channel out of range, or two serve one channel.</exception>
    public MeterAccess(
        IMechanicModule?[] modulesBySlot, in ModuleSlots slots, CoreRequests requests, ModuleCharacters? characters)
    {
        _requests = requests;
        _characters = characters;
        var gauges = new HookList<IMeterGauge>(modulesBySlot);
        for (int i = 0; i < gauges.Count; i++)
        {
            int slot = gauges.SlotAt(i);
            int channel = gauges.HookAt(i).ChannelOf(in slots[slot].Config);
            if (channel < 0 || channel >= IMeterGauge.ChannelCount)
            {
                throw new ArgumentException($"The gauge in slot {slot} names meter channel {channel}.", nameof(slots));
            }

            if (_gauges[channel] is not null)
            {
                throw new ArgumentException(
                    $"The gauges in slots {_slots[channel]} and {slot} both serve meter channel {channel}.",
                    nameof(slots));
            }

            _gauges[channel] = gauges.HookAt(i);
            _slots[channel] = slot;
            _configs[channel] = slots[slot].Config;
        }
    }

    public int MeterOf(ref MatchState state, int fighter, int channel)
    {
        CheckChannel(channel);
        return _gauges[channel] is { } gauge ? gauge.MeterOf(GaugeContext(ref state, channel), fighter) : 0;
    }

    public bool TrySpend(ref MatchState state, int fighter, int amount, int channel)
    {
        CheckChannel(channel);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Meter can't be spent negatively.");
        }

        return _gauges[channel] is { } gauge && gauge.TrySpend(GaugeContext(ref state, channel), fighter, amount);
    }

    private static void CheckChannel(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, IMeterGauge.ChannelCount);
    }

    private ModuleContext GaugeContext(ref MatchState state, int channel)
    {
        int slot = _slots[channel];
        return new(ref state, ref state.Modules[slot], in _configs[channel], _requests, slot, this, _characters);
    }
}
