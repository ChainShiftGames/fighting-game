using System;
using Sim.Modules;
using Xunit;

namespace Sim.Tests.Modules;

/// <summary>A config layout names exactly its struct's int fields, so data can't fill a field the struct lacks.</summary>
public sealed class ModuleConfigLayoutTests
{
    [Fact]
    public void ALayoutKeepsItsFieldsInOrder()
    {
        ModuleConfigLayout layout = ModuleConfigLayout.For<TwoValueConfig>(
            new ModuleConfigField("first", 0, 1), new ModuleConfigField("second", 2, 3));

        Assert.Equal((2, "first", "second", 3), (layout.Count, layout[0].Key, layout[1].Key, layout[1].Max));
    }

    [Fact]
    public void FewerFieldsThanTheStructAreRefused()
    {
        Assert.Throws<ArgumentException>(() => ModuleConfigLayout.For<TwoValueConfig>(new ModuleConfigField("first", 0, 1)));
    }

    [Fact]
    public void ARepeatedKeyIsRefused()
    {
        Assert.Throws<ArgumentException>(() => ModuleConfigLayout.For<TwoValueConfig>(
            new ModuleConfigField("same", 0, 1), new ModuleConfigField("same", 0, 1)));
    }

    [Fact]
    public void AnEmptyRangeIsRefused()
    {
        Assert.Throws<ArgumentException>(() => ModuleConfigLayout.For<TwoValueConfig>(
            new ModuleConfigField("first", 2, 1), new ModuleConfigField("second", 0, 1)));
    }

    [Fact]
    public void AModuleWithoutConfigHasAnEmptyLayout()
    {
        Assert.Equal(0, ((IMechanicModule)new CountingModule(1)).ConfigLayout.Count);
    }
}
