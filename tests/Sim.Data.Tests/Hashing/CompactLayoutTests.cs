using System;
using System.Reflection;
using Sim.Characters;
using Xunit;

namespace Sim.Data.Tests.Hashing;

/// <summary>
/// DataHashBuilder hashes compact structs byte for byte. That is only deterministic without padding bytes, which
/// holds while every field is 4 bytes wide (ints, int enums, or structs made of them).
/// </summary>
public sealed class CompactLayoutTests
{
    public static TheoryData<Type> HashedStructs =>
        [typeof(CharacterStats), typeof(MoveData), typeof(BoxData), typeof(CancelData), typeof(HitData),
            typeof(VelocityData), typeof(ProjectileData)];

    [Theory]
    [MemberData(nameof(HashedStructs))]
    public void HashedStructsHaveOnlyFourByteFields(Type type)
    {
        Assert.True(type.IsLayoutSequential, $"{type.Name} must keep sequential layout");
        AssertFourByteFields(type, type.Name);
    }

    private static void AssertFourByteFields(Type type, string path)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Type fieldType = field.FieldType.IsEnum ? Enum.GetUnderlyingType(field.FieldType) : field.FieldType;
            string fieldPath = $"{path}.{field.Name}";
            if (fieldType == typeof(int))
            {
                continue;
            }

            Assert.True(fieldType.IsValueType && !fieldType.IsPrimitive,
                $"{fieldPath} is {field.FieldType.Name}; hashed structs may only hold int-sized fields");
            AssertFourByteFields(fieldType, fieldPath);
        }
    }
}
