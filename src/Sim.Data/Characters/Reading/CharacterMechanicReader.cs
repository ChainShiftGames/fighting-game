using System;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;

namespace Sim.Data.Characters.Reading;

/// <summary>
/// Reads a character's own mechanic (ADR-0023): <c>"mechanic": { "name": "&lt;module&gt;", &lt;config keys&gt; }</c>, the
/// module's config read through its <see cref="ModuleConfigLayout"/> like a module file. Part of the character data
/// (and its hash), not of the match rules.
/// </summary>
public static class CharacterMechanicReader
{
    /// <summary>The key naming the module; no config key may use it.</summary>
    public const string NameKey = "name";

    /// <summary>The module and its config; an empty slot without a mechanic or when anything was reported.</summary>
    /// <exception cref="InvalidOperationException">The module's config layout uses the reserved name key.</exception>
    public static ModuleSlot Read(FieldReader? mechanic, ModuleCatalog catalog)
    {
        if (mechanic is null || mechanic.RequiredId(NameKey) is not { } name)
        {
            return default;
        }

        if (!catalog.TryFindId(name, out ModuleId id) || !catalog.TryFind(id, out IMechanicModule? module) ||
            !module!.IsCharacterMechanic)
        {
            // Its config keys are unknown without the module, so the rest of the object isn't checked.
            mechanic.Error(NameKey, $"no registered character mechanic is called '{name}'");
            return default;
        }

        // Every key is required: the character's data must never pick up a hidden default.
        ModuleConfigLayout layout = module.ConfigLayout;
        ModuleSlot slot = new() { Module = id };
        for (int i = 0; i < layout.Count; i++)
        {
            if (string.Equals(layout[i].Key, NameKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Module '{module.Name}' uses the reserved key '{NameKey}' for its config.");
            }

            slot.Config[i] = mechanic.RequiredInt(layout[i].Key, layout[i].Min, layout[i].Max);
        }

        mechanic.Finish();
        return slot;
    }
}
