using System;

namespace Sim.Rules;

/// <summary>Stable identifier of a mechanics module; each module picks its own non-zero value.</summary>
/// <remarks>An integer rather than the module's name so that <see cref="MatchRules"/> stays blittable.</remarks>
public readonly struct ModuleId : IEquatable<ModuleId>
{
    public readonly int Value;

    public ModuleId(int value)
    {
        Value = value;
    }

    /// <summary>An empty lineup slot.</summary>
    public static ModuleId None => default;

    public bool IsNone => Value == 0;

    public bool Equals(ModuleId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ModuleId other && Equals(other);

    public override int GetHashCode() => Value;

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static bool operator ==(ModuleId left, ModuleId right) => left.Equals(right);

    public static bool operator !=(ModuleId left, ModuleId right) => !left.Equals(right);
}
