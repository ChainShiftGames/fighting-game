using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netcode.Desync;

/// <summary>Lists every integer in a plain-data game state by name, so a desync dump can say which field differs.</summary>
/// <remarks>
/// Names follow the state's own fields: <c>Fighters[1].Position.X</c>, with one entry per inline-array element.
/// It relies on the state rule of ARCHITECTURE.md §4 (integers only, no padding): offsets are then the running
/// sum of field sizes. A layout that breaks the rule fails loudly instead of naming the wrong bytes. Reflection is
/// fine here: it runs only when a desync is dumped, never per frame.
/// </remarks>
public static class StateFields
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>Every field's name and value, in layout order.</summary>
    public static Dictionary<string, long> Read<TState>(ref readonly TState state)
        where TState : unmanaged
    {
        var layout = new List<Field>();
        int size = Walk(typeof(TState), string.Empty, 0, layout);
        if (size != Unsafe.SizeOf<TState>())
        {
            throw new InvalidOperationException($"{typeof(TState).Name}'s fields don't cover its bytes: it has padding.");
        }

        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<TState>(in state));
        var values = new Dictionary<string, long>(layout.Count, StringComparer.Ordinal);
        foreach (Field field in layout)
        {
            values.Add(field.Name, ReadInteger(bytes.Slice(field.Offset), field.Type));
        }

        return values;
    }

    private static int Walk(Type type, string path, int offset, List<Field> layout)
    {
        Type integer = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
        if (integer.IsPrimitive)
        {
            layout.Add(new Field(path, offset, integer));
            return SizeOf(integer);
        }

        FieldInfo[] declared = type.GetFields(InstanceFields);
        InlineArrayAttribute? inlineArray = type.GetCustomAttribute<InlineArrayAttribute>();
        int size = 0;
        if (inlineArray is not null)
        {
            for (int i = 0; i < inlineArray.Length; i++)
            {
                size += Walk(declared[0].FieldType, $"{path}[{i}]", offset + size, layout);
            }
        }
        else
        {
            foreach (FieldInfo field in declared)
            {
                string name = path.Length == 0 ? FieldName(field) : $"{path}.{FieldName(field)}";
                size += Walk(field.FieldType, name, offset + size, layout);
            }
        }

        if (size != SizeOf(type))
        {
            throw new InvalidOperationException($"{path} ({type.Name}) has padding, so its fields can't be named.");
        }

        return size;
    }

    private static long ReadInteger(ReadOnlySpan<byte> bytes, Type type) => type switch
    {
        _ when type == typeof(int) => MemoryMarshal.Read<int>(bytes),
        _ when type == typeof(uint) => MemoryMarshal.Read<uint>(bytes),
        _ when type == typeof(short) => MemoryMarshal.Read<short>(bytes),
        _ when type == typeof(ushort) => MemoryMarshal.Read<ushort>(bytes),
        _ when type == typeof(sbyte) => MemoryMarshal.Read<sbyte>(bytes),
        _ when type == typeof(byte) => bytes[0],
        _ when type == typeof(long) => MemoryMarshal.Read<long>(bytes),
        _ => throw new NotSupportedException($"{type.Name} fields can't be dumped: the game state holds integers only."),
    };

    // A record struct's property (InputWord.Bits) is stored in a compiler-named backing field.
    private static string FieldName(FieldInfo field)
    {
        string name = field.Name;
        int end = name.IndexOf(">k__BackingField", StringComparison.Ordinal);
        return name.StartsWith('<') && end > 0 ? name[1..end] : name;
    }

    private static int SizeOf(Type type) =>
        (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;

    private readonly record struct Field(string Name, int Offset, Type Type);
}
