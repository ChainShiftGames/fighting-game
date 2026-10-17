using System;
using System.Linq;
using Sim.Data.Modules;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Xunit;

namespace Sim.Data.Tests.Modules;

/// <summary>
/// P3-01: modules are switched on and off, and configured, in <c>rules/modules.json</c> and
/// <c>rules/modules/&lt;name&gt;.json</c>; reloading the data gives the next round the new rules and rules hash.
/// </summary>
public sealed class ModuleLineupLoaderTests
{
    private const string OrderFile = ModuleLineupSchema.OrderFile;

    private static readonly LineupTestModule _first = new(11, "first_test");
    private static readonly LineupTestModule _second = new(12, "second_test");

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void WithNoModulesInDataTheLineupIsEmpty()
    {
        MatchRules rules = Load().CreateMatchRules();

        Assert.True(rules.Modules[0].Module.IsNone);
    }

    [Fact]
    public void SwitchingAModuleOnPutsItInTheRulesWithItsConfigAndChangesTheRulesHash()
    {
        SetLineup((_first, false));
        MatchRules off = Load().CreateMatchRules();

        _data.SetText(ModuleLineupSchema.ModuleFile(_first.Name), LineupTestModule.File(enabled: true, 40, 7));
        MatchRules on = Load().CreateMatchRules();

        Assert.True(off.Modules[0].Module.IsNone);
        Assert.Equal((_first.Id, 40, 7), (on.Modules[0].Module, on.Modules[0].Config[0], on.Modules[0].Config[1]));
        Assert.NotEqual(off.ComputeHash(), on.ComputeHash());
    }

    [Fact]
    public void SwitchingAModuleBackOffGivesTheOriginalRulesHash()
    {
        SetLineup((_first, false));
        ulong before = Load().CreateMatchRules().ComputeHash();
        _data.SetText(ModuleLineupSchema.ModuleFile(_first.Name), LineupTestModule.File(enabled: true));
        Load();

        _data.SetText(ModuleLineupSchema.ModuleFile(_first.Name), LineupTestModule.File(enabled: false));

        Assert.Equal(before, Load().CreateMatchRules().ComputeHash());
    }

    [Fact]
    public void ChangingAnEnabledModulesConfigChangesTheRulesHash()
    {
        SetLineup((_first, true));
        ulong before = Load().CreateMatchRules().ComputeHash();

        _data.SetText(ModuleLineupSchema.ModuleFile(_first.Name), LineupTestModule.File(enabled: true, strength: 6));

        Assert.NotEqual(before, Load().CreateMatchRules().ComputeHash());
    }

    [Fact]
    public void ADisabledModulesConfigDoesNotReachTheRulesButIsInTheDataHash()
    {
        SetLineup((_first, false));
        GameData before = Load();

        _data.SetText(ModuleLineupSchema.ModuleFile(_first.Name), LineupTestModule.File(enabled: false, strength: 6));
        GameData after = Load();

        Assert.Equal(before.CreateMatchRules().ComputeHash(), after.CreateMatchRules().ComputeHash());
        Assert.NotEqual(before.Hash, after.Hash);
    }

    [Fact]
    public void EnabledModulesTakeSlotsInHookOrderAndDisabledOnesTakeNone()
    {
        SetLineup((_second, true), (_first, false));
        MatchRules secondOnly = Load().CreateMatchRules();
        SetLineup((_second, true), (_first, true));
        MatchRules both = Load().CreateMatchRules();

        Assert.Equal((_second.Id, ModuleId.None), (secondOnly.Modules[0].Module, secondOnly.Modules[1].Module));
        Assert.Equal((_second.Id, _first.Id), (both.Modules[0].Module, both.Modules[1].Module));
    }

    [Fact]
    public void AnUnregisteredModuleNameIsReported()
    {
        _data.SetText(OrderFile, Order("no_such_module"));

        DataError error = SingleError();
        Assert.Equal((OrderFile, "order[0]"), (error.File, error.Field));
        Assert.Contains(_first.Name, error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AModuleListedTwiceIsReported()
    {
        SetLineup((_first, false));
        _data.SetText(OrderFile, Order(_first.Name, _first.Name));

        DataError error = SingleError();
        Assert.Equal((OrderFile, "order[1]"), (error.File, error.Field));
    }

    [Fact]
    public void AListedModuleWithoutItsFileIsReported()
    {
        _data.SetText(OrderFile, Order(_first.Name));

        DataError error = SingleError();
        Assert.Equal(ModuleLineupSchema.ModuleFile(_first.Name), error.File);
    }

    [Fact]
    public void AModuleFileTheOrderDoesNotListIsReported()
    {
        SetLineup((_first, false));
        _data.SetText(ModuleLineupSchema.ModuleFile(_second.Name), LineupTestModule.File(enabled: true));

        DataError error = SingleError();
        Assert.Equal(ModuleLineupSchema.ModuleFile(_second.Name), error.File);
    }

    [Theory]
    [InlineData("\"enabled\": false", "\"enabled\": 0", "enabled")]
    [InlineData("\"enabled\": false, ", "", "enabled")]
    [InlineData("\"strength\": 5", "\"strength\": 101", "strength")]
    [InlineData("\"window\": 3", "\"window\": 0", "window")]
    [InlineData(", \"window\": 3", "", "window")]
    [InlineData("\"window\": 3", "\"window\": 3, \"extra\": 1", "extra")]
    public void AWrongModuleFileValueNamesTheField(string from, string to, string field)
    {
        SetLineup((_first, false));
        _data.Replace(ModuleLineupSchema.ModuleFile(_first.Name), from, to);

        DataError error = SingleError();
        Assert.Equal((ModuleLineupSchema.ModuleFile(_first.Name), field), (error.File, error.Field));
    }

    [Fact]
    public void MoreEnabledModulesThanLineupSlotsIsReported()
    {
        LineupTestModule[] modules = Enumerable.Range(1, ModuleStates.MaxModules + 1)
            .Select(id => new LineupTestModule(id, $"module_{id}"))
            .ToArray();
        _data.SetText(OrderFile, Order(modules.Select(module => module.Name).ToArray()));
        foreach (LineupTestModule module in modules)
        {
            _data.SetText(ModuleLineupSchema.ModuleFile(module.Name), LineupTestModule.File(enabled: true));
        }

        var exception = Assert.Throws<DataValidationException>(
            () => new GameDataLoader(_data, new ModuleCatalog(modules)).Load());

        DataError error = Assert.Single(exception.Errors);
        Assert.Equal((OrderFile, "order"), (error.File, error.Field));
    }

    [Fact]
    public void AModuleWhoseConfigUsesTheEnabledKeyIsRefused()
    {
        _data.SetText(OrderFile, Order(ReservedKeyModule.ModuleName));
        _data.SetText(ModuleLineupSchema.ModuleFile(ReservedKeyModule.ModuleName), """{ "schemaVersion": 1, "enabled": true }""");

        Assert.Throws<InvalidOperationException>(
            () => new GameDataLoader(_data, new ModuleCatalog(new ReservedKeyModule())).Load());
    }

    // Each registered module, switched on with every config value at its minimum, loads: its layout fits a slot
    // and uses no reserved key. Names no module, so it covers every module a later task registers.
    [Fact]
    public void EveryRegisteredModuleLoadsFromItsDataFile()
    {
        ModuleCatalog registered = RegisteredModules.Create();
        string[] names = new string[registered.Count];
        for (int i = 0; i < registered.Count; i++)
        {
            IMechanicModule module = registered[i];
            names[i] = module.Name;
            string config = string.Concat(Enumerable.Range(0, module.ConfigLayout.Count)
                .Select(field => $", \"{module.ConfigLayout[field].Key}\": {module.ConfigLayout[field].Min}"));
            _data.SetText(ModuleLineupSchema.ModuleFile(module.Name), $$"""{ "schemaVersion": 1, "enabled": true{{config}} }""");
        }

        _data.SetText(OrderFile, Order(names));

        MatchRules rules = new GameDataLoader(_data, registered).Load().CreateMatchRules();

        Assert.Equal(registered[registered.Count - 1].Id, rules.Modules[registered.Count - 1].Module);
    }

    // ADR-0005 §8: exported builds read the embedded data, so they play exactly the rules in the repository's data.
    [Fact]
    public void EmbeddedDataHasTheRepositorysMatchRules()
    {
        MatchRules folder = Fixture.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot)).CreateMatchRules();
        MatchRules embedded = Fixture.Load(new EmbeddedDataSource()).CreateMatchRules();

        Assert.Equal(folder.ComputeHash(), embedded.ComputeHash());
    }

    private static string Order(params string[] names) =>
        $$"""{ "schemaVersion": 1, "order": [{{string.Join(", ", names.Select(name => $"\"{name}\""))}}] }""";

    private void SetLineup(params (LineupTestModule Module, bool Enabled)[] modules)
    {
        _data.SetText(OrderFile, Order(modules.Select(entry => entry.Module.Name).ToArray()));
        foreach ((LineupTestModule module, bool enabled) in modules)
        {
            _data.SetText(ModuleLineupSchema.ModuleFile(module.Name), LineupTestModule.File(enabled));
        }
    }

    private GameData Load() => new GameDataLoader(_data, new ModuleCatalog(_first, _second)).Load();

    private DataError SingleError() => Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);

    private sealed class ReservedKeyModule : IMechanicModule
    {
        public const string ModuleName = "reserved_key";

        public ModuleId Id => new(20);

        public string Name => ModuleName;

        public ModuleConfigLayout ConfigLayout { get; } =
            ModuleConfigLayout.For<LineupTestConfig>(
                new ModuleConfigField(ModuleLineupSchema.EnabledKey, 0, 1), new ModuleConfigField("window", 0, 1));
    }
}
