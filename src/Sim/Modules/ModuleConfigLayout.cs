using System;
using System.Runtime.CompilerServices;
using Sim.Rules;

namespace Sim.Modules;

/// <summary>A module's config struct as data names it: one field per int, in the struct's field order.</summary>
/// <remarks>
/// Sim.Data reads each key from the module's data file into that position of the module's <see cref="MatchRules"/>
/// slot, and the module reads it back through its own config struct. <see cref="For{TConfig}"/> checks the field
/// count against the struct, so a field added to one but not the other fails as soon as the module is created.
/// </remarks>
public sealed class ModuleConfigLayout
{
    private readonly ModuleConfigField[] _fields;

    private ModuleConfigLayout(ModuleConfigField[] fields)
    {
        _fields = fields;
    }

    /// <summary>A module without config.</summary>
    public static ModuleConfigLayout None => new([]);

    public int Count => _fields.Length;

    public ModuleConfigField this[int index] => _fields[index];

    /// <param name="fields">One per int field of <typeparamref name="TConfig"/>, in declaration order.</param>
    /// <exception cref="ArgumentException">The fields don't match the struct, repeat a key or have an empty range.</exception>
    public static ModuleConfigLayout For<TConfig>(params ModuleConfigField[] fields)
        where TConfig : unmanaged, IModuleConfig
    {
        int structFields = Unsafe.SizeOf<TConfig>() / sizeof(int);
        if (structFields != fields.Length || fields.Length > ModuleConfig.Length)
        {
            throw new ArgumentException(
                $"{typeof(TConfig).Name} has {structFields} int field(s) but its layout names {fields.Length}.",
                nameof(fields));
        }

        for (int i = 0; i < fields.Length; i++)
        {
            if (string.IsNullOrEmpty(fields[i].Key) || fields[i].Min > fields[i].Max)
            {
                throw new ArgumentException($"Config field {i} needs a key and min <= max.", nameof(fields));
            }

            for (int j = 0; j < i; j++)
            {
                if (string.Equals(fields[j].Key, fields[i].Key, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Config key '{fields[i].Key}' appears twice.", nameof(fields));
                }
            }
        }

        return new ModuleConfigLayout((ModuleConfigField[])fields.Clone());
    }
}
