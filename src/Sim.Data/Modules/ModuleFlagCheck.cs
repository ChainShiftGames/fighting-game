using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Rules;
using Sim.Data.Validation;
using Sim.Fighters;
using Sim.Modules;
using Sim.Rules;

namespace Sim.Data.Modules;

/// <summary>
/// Checks that every fighter flag an enabled module or a character's mechanic needs (<see cref="IMechanicModule.Flags"/>)
/// is in rules/flags.json (ADR-0023), so a match never fails to start over it.
/// </summary>
public static class ModuleFlagCheck
{
    public static void Check(
        IReadOnlyList<CharacterData> characters,
        ModuleCatalog catalog,
        ModuleLineupFile lineup,
        FighterFlagNames flags,
        DataErrors errors)
    {
        foreach (ModuleEntry entry in lineup.Modules)
        {
            if (entry.Enabled && catalog.TryFind(entry.Id, out IMechanicModule? module))
            {
                ReportMissing(module!, flags, ModuleLineupSchema.ModuleFile(entry.Name), errors);
            }
        }

        foreach (CharacterData character in characters)
        {
            if (!character.Mechanic.Module.IsNone && catalog.TryFind(character.Mechanic.Module, out IMechanicModule? mechanic))
            {
                ReportMissing(mechanic!, flags, CharacterSchema.CharacterFile(character.Id), errors);
            }
        }
    }

    private static void ReportMissing(IMechanicModule module, FighterFlagNames flags, string file, DataErrors errors)
    {
        foreach (string flag in module.Flags)
        {
            if (flags.MaskOf(flag) == 0)
            {
                errors.Add(file, "", $"module '{module.Name}' needs fighter flag '{flag}', which {FighterFlagsSchema.File} doesn't name");
            }
        }
    }
}
