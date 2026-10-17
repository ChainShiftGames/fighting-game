using System;
using System.Collections.Generic;
using Sim.Fighters;
using Sim.Rules;

namespace Sim.Modules;

/// <summary>Each module's flag names (<see cref="IMechanicModule.Flags"/>) resolved to bit masks once at match start.</summary>
internal sealed class ModuleFlags
{
    private readonly int[][] _masks;

    /// <exception cref="ArgumentException">A module names a flag that <paramref name="names"/> doesn't have.</exception>
    public ModuleFlags(IMechanicModule?[] modulesBySlot, FighterFlagNames names)
    {
        _masks = new int[modulesBySlot.Length][];
        for (int slot = 0; slot < modulesBySlot.Length; slot++)
        {
            _masks[slot] = modulesBySlot[slot] is { } module ? Resolve(module, names) : [];
        }
    }

    /// <exception cref="ArgumentOutOfRangeException">The module in the slot has no flag at that index.</exception>
    public int MaskOf(int slot, int flag) => _masks[slot][flag];

    private static int[] Resolve(IMechanicModule module, FighterFlagNames names)
    {
        IReadOnlyList<string> flags = module.Flags;
        int[] masks = new int[flags.Count];
        for (int i = 0; i < flags.Count; i++)
        {
            masks[i] = names.MaskOf(flags[i]);
            if (masks[i] == 0)
            {
                throw new ArgumentException(
                    $"Module '{module.Name}' needs fighter flag '{flags[i]}', which data/rules/flags.json doesn't name.");
            }
        }

        return masks;
    }
}
