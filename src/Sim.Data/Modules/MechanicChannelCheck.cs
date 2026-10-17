using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;

namespace Sim.Data.Modules;

/// <summary>
/// Checks that a character's mechanic, when it is a meter gauge, doesn't take a channel an enabled lineup gauge already
/// serves (ADR-0023): the core routes each channel of a fighter to one gauge.
/// </summary>
public static class MechanicChannelCheck
{
    public static void Check(
        IReadOnlyList<CharacterData> characters, ModuleCatalog catalog, ModuleLineupFile lineup, DataErrors errors)
    {
        foreach (CharacterData character in characters)
        {
            ModuleSlot mechanic = character.Mechanic;
            if (mechanic.Module.IsNone || !catalog.TryFind(mechanic.Module, out IMechanicModule? module) ||
                module is not IMeterGauge gauge)
            {
                continue;
            }

            int channel = gauge.ChannelOf(in mechanic.Config);
            if (LineupGaugeOn(channel, catalog, lineup) is { } other)
            {
                errors.Add(
                    CharacterSchema.CharacterFile(character.Id),
                    "mechanic.channel",
                    $"the enabled module '{other}' already keeps meter channel {channel}");
            }
        }
    }

    private static string? LineupGaugeOn(int channel, ModuleCatalog catalog, ModuleLineupFile lineup)
    {
        foreach (ModuleEntry entry in lineup.Modules)
        {
            if (!entry.Enabled || !catalog.TryFind(entry.Id, out IMechanicModule? module) ||
                module is not IMeterGauge gauge)
            {
                continue;
            }

            ModuleConfig config = default;
            for (int i = 0; i < entry.Config.Count; i++)
            {
                config[i] = entry.Config[i];
            }

            if (gauge.ChannelOf(in config) == channel)
            {
                return entry.Name;
            }
        }

        return null;
    }
}
