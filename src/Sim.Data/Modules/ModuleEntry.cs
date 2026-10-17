using System.Collections.Generic;
using Sim.Rules;

namespace Sim.Data.Modules;

/// <summary>One module's data file: whether it is on, and its config in its config struct's field order.</summary>
/// <remarks>Both are developer settings (ARCHITECTURE.md §6): part of the match rules when enabled.</remarks>
public sealed class ModuleEntry
{
    public ModuleEntry(string name, ModuleId id, bool enabled, IReadOnlyList<int> config)
    {
        Name = name;
        Id = id;
        Enabled = enabled;
        Config = config;
    }

    public string Name { get; }

    public ModuleId Id { get; }

    public bool Enabled { get; }

    /// <summary>At most <see cref="ModuleConfig.Length"/> values, in the order of the module's config layout.</summary>
    public IReadOnlyList<int> Config { get; }
}
