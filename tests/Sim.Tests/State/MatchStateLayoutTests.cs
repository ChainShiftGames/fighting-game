using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sim.State;
using Xunit;

namespace Sim.Tests.State;

/// <summary>The match state must hash the same on both PCs: no references and no padding bytes anywhere.</summary>
public sealed class MatchStateLayoutTests
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void MatchStateContainsNoReferences()
    {
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<MatchState>());
    }

    // Padding bytes aren't guaranteed to be equal in two equal states, so they would make the checksum differ.
    // Every nested struct's size must equal the sum of its fields' sizes. This walks every field a later task adds.
    [Fact]
    public void MatchStateHasNoPaddingAtAnyLevel()
    {
        AssertNoPadding(typeof(MatchState), nameof(MatchState));
    }

    private static int AssertNoPadding(Type type, string path)
    {
        int size = SizeOf(type);
        if (type.IsPrimitive || type.IsEnum)
        {
            return size;
        }

        FieldInfo[] fields = type.GetFields(InstanceFields);
        int payload = 0;
        InlineArrayAttribute? inlineArray = type.GetCustomAttribute<InlineArrayAttribute>();
        if (inlineArray is not null)
        {
            payload = inlineArray.Length * AssertNoPadding(fields[0].FieldType, $"{path}[]");
        }
        else
        {
            foreach (FieldInfo field in fields)
            {
                payload += AssertNoPadding(field.FieldType, $"{path}.{field.Name}");
            }
        }

        Assert.True(
            payload == size,
            $"{path} ({type.Name}) is {size} bytes but its fields are {payload}: reorder or widen fields.");
        return size;
    }

    private static int SizeOf(Type type) =>
        (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;
}
