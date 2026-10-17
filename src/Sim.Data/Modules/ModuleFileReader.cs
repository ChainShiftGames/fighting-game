using System;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Modules;

namespace Sim.Data.Modules;

/// <summary>Reads and validates one <c>rules/modules/&lt;name&gt;.json</c> against the module's config layout.</summary>
public static class ModuleFileReader
{
    /// <summary>The module's entry, or null when anything was reported to <paramref name="errors"/>.</summary>
    /// <exception cref="InvalidOperationException">The module's config layout uses the reserved enabled key.</exception>
    public static ModuleEntry? Read(IDataSource source, IMechanicModule module, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, ModuleLineupSchema.ModuleFile(module.Name), errors);
        if (file is null || !SchemaVersion.Check(file, ModuleLineupSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        bool enabled = file.RequiredBool(ModuleLineupSchema.EnabledKey);

        // Every key is required, also while the module is off: switching it on must never pick up a hidden default.
        ModuleConfigLayout layout = module.ConfigLayout;
        int[] config = new int[layout.Count];
        for (int i = 0; i < layout.Count; i++)
        {
            if (string.Equals(layout[i].Key, ModuleLineupSchema.EnabledKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Module '{module.Name}' uses the reserved key '{ModuleLineupSchema.EnabledKey}' for its config.");
            }

            config[i] = file.RequiredInt(layout[i].Key, layout[i].Min, layout[i].Max);
        }

        file.Finish();
        return errors.Count > errorsBefore ? null : new ModuleEntry(module.Name, module.Id, enabled, config);
    }
}
