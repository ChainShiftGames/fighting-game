using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Validation;
using Sim.Modules;

namespace Sim.Data.Modules;

/// <summary>
/// Checks the characters' move roles against the modules (ADR-0023): every role is one a registered module asks for
/// (catches typos), and every character names each role an enabled module or its own mechanic needs.
/// </summary>
public static class MoveRoleCheck
{
    private const string Field = "moveRoles";

    public static void Check(
        IReadOnlyList<CharacterData> characters, ModuleCatalog catalog, ModuleLineupFile lineup, DataErrors errors)
    {
        foreach (CharacterData character in characters)
        {
            string file = CharacterSchema.CharacterFile(character.Id);
            foreach (MoveRole role in character.MoveRoles)
            {
                if (!IsAskedFor(catalog, role.Name))
                {
                    errors.Add(file, $"{Field}.{role.Name}", "no registered module asks for this role");
                }
            }

            foreach (ModuleEntry entry in lineup.Modules)
            {
                if (entry.Enabled && catalog.TryFind(entry.Id, out IMechanicModule? module))
                {
                    ReportMissingRoles(character, module!, file, errors);
                }
            }

            if (!character.Mechanic.Module.IsNone && catalog.TryFind(character.Mechanic.Module, out IMechanicModule? mechanic))
            {
                ReportMissingRoles(character, mechanic!, file, errors);
            }
        }
    }

    private static void ReportMissingRoles(
        CharacterData character, IMechanicModule module, string file, DataErrors errors)
    {
        foreach (string role in module.MoveRoles)
        {
            if (character.MoveForRole(role) == CharacterData.NoMove)
            {
                errors.Add(file, Field, $"names no '{role}' move, which the module '{module.Name}' needs");
            }
        }
    }

    private static bool IsAskedFor(ModuleCatalog catalog, string role)
    {
        for (int i = 0; i < catalog.Count; i++)
        {
            foreach (string asked in catalog[i].MoveRoles)
            {
                if (string.Equals(asked, role, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
