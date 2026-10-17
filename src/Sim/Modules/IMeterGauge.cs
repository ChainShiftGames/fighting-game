using Sim.Rules;

namespace Sim.Modules;

/// <summary>Optional module role: keeps each fighter's meter on one channel; other modules read and spend it through the core.</summary>
/// <remarks>
/// Other modules never see the gauge's slice; they call <see cref="ModuleContext.MeterOf"/> and
/// <see cref="ModuleContext.TrySpendMeter"/> with a channel, which the core routes here when this gauge serves that
/// channel (ADR-0023). Channel <see cref="MainChannel"/> is the main meter; other channels are further resources (a
/// character's own gauge, ammo…). At most one enabled gauge serves a channel. Meter is in hundredths of a percent: 0
/// to <see cref="FullMeter"/>. Spending is immediate, so a later hook in the same frame sees the new amount.
/// </remarks>
public interface IMeterGauge
{
    /// <summary>A full gauge (100 %), in hundredths of a percent.</summary>
    const int FullMeter = 10_000;

    /// <summary>The main meter's channel; what modules use unless they name another.</summary>
    const int MainChannel = 0;

    /// <summary>Channels 0 to 7.</summary>
    const int ChannelCount = 8;

    /// <summary>The channel this gauge serves with <paramref name="config"/> (its slot's config); 0 to 7.</summary>
    /// <remarks>Asked once at match start, so the channel comes from config, never from state.</remarks>
    int ChannelOf(ref readonly ModuleConfig config);

    /// <summary>The fighter's meter now; a question, so it must not change state or make requests.</summary>
    int MeterOf(ModuleContext context, int fighter);

    /// <summary>Takes <paramref name="amount"/> (≥ 0) from the fighter's meter if it has that much; false leaves it.</summary>
    bool TrySpend(ModuleContext context, int fighter, int amount);
}
