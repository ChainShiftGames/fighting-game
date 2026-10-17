namespace Sim.Modules;

/// <summary>Optional module role: keeps each fighter's meter, which other modules read and spend through the core.</summary>
/// <remarks>
/// Other modules never see the gauge's slice; they call <see cref="ModuleContext.MeterOf"/> and
/// <see cref="ModuleContext.TrySpendMeter"/>, which the core routes here (the first enabled gauge in hook order).
/// Meter is in hundredths of a percent: 0 to <see cref="FullMeter"/>. Spending is immediate, so a later hook in the
/// same frame sees the new amount.
/// </remarks>
public interface IMeterGauge
{
    /// <summary>A full gauge (100 %), in hundredths of a percent.</summary>
    const int FullMeter = 10_000;

    /// <summary>The fighter's meter now; a question, so it must not change state or make requests.</summary>
    int MeterOf(ModuleContext context, int fighter);

    /// <summary>Takes <paramref name="amount"/> (≥ 0) from the fighter's meter if it has that much; false leaves it.</summary>
    bool TrySpend(ModuleContext context, int fighter, int amount);
}
