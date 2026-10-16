using System;
using Sim.Modules;
using Sim.Rules;
using Xunit;

namespace Sim.Tests.Modules;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void FindsModulesByIdAndByDataName()
    {
        var counting = new CountingModule(4);
        var catalog = new ModuleCatalog(new CountingModule(3), counting);

        Assert.True(catalog.TryFind(new ModuleId(4), out IMechanicModule? found));
        Assert.Same(counting, found);
        Assert.True(catalog.TryFindId("counting_4", out ModuleId id));
        Assert.Equal(new ModuleId(4), id);
        Assert.False(catalog.TryFind(new ModuleId(5), out _));
        Assert.False(catalog.TryFindId("missing", out _));
    }

    [Fact]
    public void RejectsTheReservedIdZero()
    {
        Assert.Throws<ArgumentException>(() => new ModuleCatalog(new CountingModule(0)));
    }

    [Fact]
    public void RejectsDuplicateIds()
    {
        Assert.Throws<ArgumentException>(() => new ModuleCatalog(new CountingModule(2), new RequestingModule(2)));
    }

    [Fact]
    public void RejectsDuplicateNames()
    {
        Assert.Throws<ArgumentException>(() => new ModuleCatalog(new CountingModule(2), new CountingModule(2)));
    }
}
