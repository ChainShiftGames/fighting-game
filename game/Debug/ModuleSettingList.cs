using System.Collections.Generic;
using System.IO;
using Sim.Data.Modules;
using Sim.Modules;

namespace FightingGame.Debug;

/// <summary>
/// The mechanics modules' developer settings for the debug panel (P3-34): each module's on/off switch and every
/// config value, read from its <c>data/rules/modules/&lt;name&gt;.json</c> with the bounds its config layout declares.
/// </summary>
/// <remarks>
/// Built from the registered modules, so a new module shows up without a change here. Only modules with a data file
/// are listed (test-only modules have none). Without a data folder (an exported build reads the embedded data, whose
/// rules are fixed: ADR-0005 §8) the list is empty, so release builds can't change the rules from the panel.
/// </remarks>
public static class ModuleSettingList
{
    /// <param name="dataRoot">The data folder the game reads; null for the embedded data.</param>
    public static (DataFileFlag[] Switches, DataFileSetting[] Values) For(string? dataRoot)
    {
        if (dataRoot is null)
        {
            return ([], []);
        }

        var switches = new List<DataFileFlag>();
        var values = new List<DataFileSetting>();

        ModuleCatalog modules = RegisteredModules.Create();
        for (int i = 0; i < modules.Count; i++)
        {
            IMechanicModule module = modules[i];
            string file = ModuleLineupSchema.ModuleFile(module.Name);
            if (!File.Exists(DataFilePath.In(dataRoot, file)))
            {
                continue;
            }

            switches.Add(new DataFileFlag($"Module {module.Name}", file, ModuleLineupSchema.EnabledKey));
            for (int field = 0; field < module.ConfigLayout.Count; field++)
            {
                ModuleConfigField config = module.ConfigLayout[field];
                values.Add(new DataFileSetting($"{module.Name}: {config.Key}", file, config.Key, config.Min, config.Max, 1));
            }
        }

        return (switches.ToArray(), values.ToArray());
    }
}
