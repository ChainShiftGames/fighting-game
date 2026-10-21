using System;
using Sim.Modules;
using Sim.Rules;
using Sim.State;

namespace FightingGame.Presentation.Hud;

/// <summary>Reads the meter and burst gauge from their modules' state slices, when those modules are enabled.</summary>
/// <remarks>
/// Modules are found by data name, never by type, so removing a module's folder still compiles (ARCHITECTURE.md §5);
/// the HUD then hides its gauge. The slice indices follow each module's state struct (MeterState, BurstState);
/// HudGaugesTests pin them.
/// </remarks>
public sealed class HudGauges
{
    /// <summary>A full meter, in hundredths of a percent.</summary>
    public const int FullMeter = IMeterGauge.FullMeter;

    private const string MeterName = "meter";
    private const string BurstName = "burst";
    private const string MaxGaugeKey = "maxGauge";

    // MeterState: P1's meter, then P2's.
    private const int MeterStride = 1;

    // BurstState: per fighter its gauge, then bursts used.
    private const int BurstStride = 2;

    private readonly int _meterSlot;
    private readonly int _burstSlot;
    private readonly int _burstMax;

    public HudGauges(ModuleCatalog catalog, in MatchRules rules)
    {
        _meterSlot = SlotOf(catalog, in rules, MeterName);
        _burstSlot = SlotOf(catalog, in rules, BurstName);
        _burstMax = _burstSlot < 0 ? 0 : ConfigValue(catalog, in rules, _burstSlot, MaxGaugeKey);
    }

    public bool HasMeter => _meterSlot >= 0;

    public bool HasBurst => _burstSlot >= 0 && _burstMax > 0;

    /// <summary>The fighter's meter in hundredths of a percent, 0 to <see cref="FullMeter"/>; 0 without the meter module.</summary>
    public int Meter(in MatchState state, int player) =>
        HasMeter ? Math.Clamp(state.Modules[_meterSlot][player * MeterStride], 0, FullMeter) : 0;

    /// <summary>Share of the burst gauge filled, 0 to 1; 0 without the burst module.</summary>
    public float BurstFraction(in MatchState state, int player) =>
        HasBurst ? Math.Clamp((float)state.Modules[_burstSlot][player * BurstStride] / _burstMax, 0f, 1f) : 0f;

    private static int SlotOf(ModuleCatalog catalog, in MatchRules rules, string name)
    {
        if (!catalog.TryFindId(name, out ModuleId id))
        {
            return -1;
        }

        for (int slot = 0; slot < ModuleStates.LineupSlots; slot++)
        {
            if (rules.Modules[slot].Module == id)
            {
                return slot;
            }
        }

        return -1;
    }

    private static int ConfigValue(ModuleCatalog catalog, in MatchRules rules, int slot, string key)
    {
        catalog.TryFind(rules.Modules[slot].Module, out IMechanicModule? module);
        ModuleConfigLayout layout = module!.ConfigLayout;
        for (int i = 0; i < layout.Count; i++)
        {
            if (string.Equals(layout[i].Key, key, StringComparison.Ordinal))
            {
                return rules.Modules[slot].Config[i];
            }
        }

        return 0;
    }
}
