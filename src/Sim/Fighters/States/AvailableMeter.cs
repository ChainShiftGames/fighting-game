using System.Runtime.CompilerServices;
using Sim.Modules;

namespace Sim.Fighters.States;

/// <summary>One fighter's meter on each channel as its state machine step starts, to tell which move costs it can pay.</summary>
/// <remarks>
/// Read from the gauges through the core before each step (ADR-0023), so the state machine needs no access to the
/// modules; <see cref="Unlimited"/> pays every cost, for driving a state machine without modules (unit tests).
/// </remarks>
[InlineArray(IMeterGauge.ChannelCount)]
public struct AvailableMeter
{
    private int _element;

    /// <summary>Every channel full beyond any cost.</summary>
    public static AvailableMeter Unlimited
    {
        get
        {
            AvailableMeter meter = default;
            for (int channel = 0; channel < IMeterGauge.ChannelCount; channel++)
            {
                meter[channel] = int.MaxValue;
            }

            return meter;
        }
    }
}
