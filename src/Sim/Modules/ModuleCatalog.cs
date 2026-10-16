using System;
using Sim.Rules;

namespace Sim.Modules;

/// <summary>The registered modules, looked up by id or data name; ids and names are checked to be unique.</summary>
public sealed class ModuleCatalog
{
    private readonly IMechanicModule[] _modules;

    public ModuleCatalog(params IMechanicModule[] modules)
    {
        for (int i = 0; i < modules.Length; i++)
        {
            if (modules[i].Id.IsNone)
            {
                throw new ArgumentException($"Module '{modules[i].Name}' uses the reserved id 0.", nameof(modules));
            }

            for (int j = 0; j < i; j++)
            {
                if (modules[j].Id == modules[i].Id ||
                    string.Equals(modules[j].Name, modules[i].Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Modules '{modules[j].Name}' and '{modules[i].Name}' share an id or a name.", nameof(modules));
                }
            }
        }

        _modules = (IMechanicModule[])modules.Clone();
    }

    public int Count => _modules.Length;

    public IMechanicModule this[int index] => _modules[index];

    public bool TryFind(ModuleId id, out IMechanicModule? module)
    {
        foreach (IMechanicModule candidate in _modules)
        {
            if (candidate.Id == id)
            {
                module = candidate;
                return true;
            }
        }

        module = null;
        return false;
    }

    /// <summary>Resolves a name from the module order list in data.</summary>
    public bool TryFindId(string name, out ModuleId id)
    {
        foreach (IMechanicModule candidate in _modules)
        {
            if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                id = candidate.Id;
                return true;
            }
        }

        id = ModuleId.None;
        return false;
    }
}
