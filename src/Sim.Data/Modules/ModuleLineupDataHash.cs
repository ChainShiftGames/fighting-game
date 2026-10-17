using Sim.Data.Hashing;

namespace Sim.Data.Modules;

/// <summary>Adds the module lineup to the data hash: order, on/off and every module's config, enabled or not.</summary>
public static class ModuleLineupDataHash
{
    public static void Append(DataHashBuilder hash, ModuleLineupFile lineup)
    {
        hash.Add(lineup.Modules.Count);
        foreach (ModuleEntry module in lineup.Modules)
        {
            hash.Add(module.Name);
            hash.Add(module.Enabled ? 1 : 0);
            hash.Add(module.Config.Count);
            foreach (int value in module.Config)
            {
                hash.Add(value);
            }
        }
    }
}
