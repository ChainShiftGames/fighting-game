using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sim.Tests.Rules;

/// <summary>Finds every int field of a plain-data struct, including each inline-array element, with its offset.</summary>
/// <remarks>
/// Asserts the struct is ints only (enums backed by int count), which rules out padding: equal values then always have
/// equal bytes and hash equally. Offsets follow from that, since sequential structs of 4-byte fields have no gaps.
/// </remarks>
internal static class IntFieldLayout
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public readonly record struct IntField(string Path, int Offset);

    public static List<IntField> Walk(Type type)
    {
        var fields = new List<IntField>();
        int size = Walk(type, type.Name, 0, fields);
        Assert.Equal(SizeOf(type), size);
        return fields;
    }

    private static int Walk(Type type, string path, int offset, List<IntField> fields)
    {
        if (type == typeof(int) || (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(int)))
        {
            fields.Add(new IntField(path, offset));
            return sizeof(int);
        }

        Assert.True(
            type.IsValueType && !type.IsPrimitive && !type.IsEnum,
            $"{path} is a {type.Name}; only int fields (or structs of them) are allowed.");

        FieldInfo[] declared = type.GetFields(InstanceFields);
        InlineArrayAttribute? inlineArray = type.GetCustomAttribute<InlineArrayAttribute>();
        int size = 0;
        if (inlineArray is not null)
        {
            for (int i = 0; i < inlineArray.Length; i++)
            {
                size += Walk(declared[0].FieldType, $"{path}[{i}]", offset + size, fields);
            }
        }
        else
        {
            foreach (FieldInfo field in declared)
            {
                size += Walk(field.FieldType, $"{path}.{field.Name}", offset + size, fields);
            }
        }

        Assert.True(size == SizeOf(type), $"{path} ({type.Name}) has padding.");
        return size;
    }

    private static int SizeOf(Type type) =>
        (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;
}
