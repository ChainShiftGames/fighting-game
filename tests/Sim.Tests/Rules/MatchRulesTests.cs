using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sim.Rules;
using Xunit;

namespace Sim.Tests.Rules;

/// <summary>ADR-0011 §7: the match-rules hash covers every field, so no gameplay setting escapes the online check.</summary>
public sealed class MatchRulesTests
{
    [Fact]
    public void MatchRulesIsIntsOnlyWithoutPadding()
    {
        Assert.NotEmpty(IntFieldLayout.Walk(typeof(MatchRules)));
    }

    [Fact]
    public void EqualRulesHaveEqualHashes()
    {
        MatchRules first = default;
        first.Modules[3].Module = new ModuleId(7);
        first.Modules[3].Config[2] = 40;
        MatchRules copy = first;

        Assert.Equal(first.ComputeHash(), copy.ComputeHash());
    }

    // Walks the fields by reflection, so every field a later task adds is covered without touching this test.
    [Fact]
    public void ChangingAnyFieldChangesTheHash()
    {
        List<IntFieldLayout.IntField> fields = IntFieldLayout.Walk(typeof(MatchRules));
        ulong baseline = default(MatchRules).ComputeHash();

        foreach (IntFieldLayout.IntField field in fields)
        {
            MatchRules changed = default;
            Span<byte> bytes = MemoryMarshal.AsBytes(new Span<MatchRules>(ref changed));
            MemoryMarshal.Write(bytes[field.Offset..], 1);

            Assert.True(baseline != changed.ComputeHash(), $"Changing {field.Path} doesn't change the hash.");
        }
    }

    [Fact]
    public void FieldWalkReachesTheFieldsItWrites()
    {
        // Guards the byte offsets the hash test writes at: a known field must land where the struct really has it.
        List<IntFieldLayout.IntField> fields = IntFieldLayout.Walk(typeof(MatchRules));
        int moduleIdIndex = fields.FindIndex(field => field.Path == "MatchRules.Modules[2].Module.Value");
        MatchRules rules = default;
        Span<byte> bytes = MemoryMarshal.AsBytes(new Span<MatchRules>(ref rules));

        MemoryMarshal.Write(bytes[fields[moduleIdIndex].Offset..], 5);

        Assert.Equal(new ModuleId(5), rules.Modules[2].Module);
    }
}
