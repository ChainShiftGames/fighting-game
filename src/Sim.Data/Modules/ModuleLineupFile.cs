using System.Collections.Generic;
using Sim.Rules;

namespace Sim.Data.Modules;

/// <summary>The loaded module lineup: every module data knows, in hook order, switched on or off.</summary>
public sealed class ModuleLineupFile
{
    public ModuleLineupFile(IReadOnlyList<ModuleEntry> modules)
    {
        Modules = modules;
    }

    /// <summary>In the hook order of <c>rules/modules.json</c>, enabled or not.</summary>
    public IReadOnlyList<ModuleEntry> Modules { get; }

    /// <summary>Puts the enabled modules into the lineup slots in hook order, each with its config; the rest are empty.</summary>
    /// <remarks>Disabled modules take no slot, so switching one changes only the enabled modules' slots and the hash.</remarks>
    public void ApplyTo(ref MatchRules rules)
    {
        rules.Modules = default;
        int slot = 0;
        foreach (ModuleEntry module in Modules)
        {
            if (!module.Enabled)
            {
                continue;
            }

            rules.Modules[slot].Module = module.Id;
            for (int i = 0; i < module.Config.Count; i++)
            {
                rules.Modules[slot].Config[i] = module.Config[i];
            }

            slot++;
        }
    }
}
