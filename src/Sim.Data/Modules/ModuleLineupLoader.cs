using System.Collections.Generic;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;
using Sim.State;

namespace Sim.Data.Modules;

/// <summary>Reads <c>rules/modules.json</c> and every module file it lists, checked against the registered modules.</summary>
/// <remarks>
/// Fails loudly on a name no module registers, a module listed twice, a module file the order doesn't list and more
/// enabled modules than the lineup has slots. A registered module data doesn't list is simply never enabled (e.g. the
/// test-only contract probe).
/// </remarks>
public static class ModuleLineupLoader
{
    private const string OrderField = "order";

    /// <summary>The lineup, or null when anything was reported to <paramref name="errors"/>.</summary>
    public static ModuleLineupFile? Load(IDataSource source, ModuleCatalog catalog, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, ModuleLineupSchema.OrderFile, errors);
        if (file is null || !SchemaVersion.Check(file, ModuleLineupSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        IReadOnlyList<string?> order = file.IdArray(OrderField, required: true);
        file.Finish();

        List<ModuleEntry> modules = [];
        List<string> listed = [];
        for (int i = 0; i < order.Count; i++)
        {
            if (order[i] is { } name && IsNewRegisteredName(file, catalog, listed, name, i, out IMechanicModule? module))
            {
                listed.Add(name);
                if (ModuleFileReader.Read(source, module!, errors) is { } entry)
                {
                    modules.Add(entry);
                }
            }
        }

        CheckEveryFileIsListed(source, listed, errors);
        CheckEnabledModulesFit(file, modules);
        return errors.Count > errorsBefore ? null : new ModuleLineupFile(modules);
    }

    private static bool IsNewRegisteredName(
        FieldReader file, ModuleCatalog catalog, List<string> listed, string name, int index, out IMechanicModule? module)
    {
        module = null;
        string field = $"{OrderField}[{index}]";
        if (listed.Contains(name))
        {
            file.Error(field, $"'{name}' is listed more than once");
            return false;
        }

        if (!catalog.TryFindId(name, out ModuleId id) || !catalog.TryFind(id, out module))
        {
            file.Error(field, $"no registered module is called '{name}' (registered: {RegisteredNames(catalog)})");
            return false;
        }

        return true;
    }

    // A forgotten file would otherwise look like a module that is off.
    private static void CheckEveryFileIsListed(IDataSource source, List<string> listed, DataErrors errors)
    {
        foreach (string fileName in source.ListFiles(ModuleLineupSchema.ModulesDirectory))
        {
            string? name = ModuleLineupSchema.ModuleName(fileName);
            if (name is not null && !listed.Contains(name))
            {
                errors.Add(
                    ModuleLineupSchema.ModuleFile(name),
                    "",
                    $"the module isn't in the {OrderField} of {ModuleLineupSchema.OrderFile} (or isn't registered)");
            }
        }
    }

    private static void CheckEnabledModulesFit(FieldReader file, List<ModuleEntry> modules)
    {
        int enabled = 0;
        foreach (ModuleEntry module in modules)
        {
            enabled += module.Enabled ? 1 : 0;
        }

        if (enabled > ModuleStates.MaxModules)
        {
            file.Error(OrderField, $"{enabled} modules are enabled; the lineup has {ModuleStates.MaxModules} slots");
        }
    }

    private static string RegisteredNames(ModuleCatalog catalog)
    {
        string[] names = new string[catalog.Count];
        for (int i = 0; i < catalog.Count; i++)
        {
            names[i] = catalog[i].Name;
        }

        return string.Join(", ", names);
    }
}
