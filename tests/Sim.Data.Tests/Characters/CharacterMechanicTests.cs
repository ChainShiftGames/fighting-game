using System;
using System.Collections.Generic;
using System.Linq;
using Sim.Characters;
using Sim.Data.Modules;
using Sim.Data.Tests.Modules;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;
using Xunit;

namespace Sim.Data.Tests.Characters;

/// <summary>ADR-0023: a character names its own mechanic module in character.json, with that module's config.</summary>
public sealed class CharacterMechanicTests
{
    private static readonly MechanicTestModule _mechanic = new(41, "test_mechanic");
    private static readonly GaugeTestModule _gaugeMechanic = new(42, "gauge_mechanic", isMechanic: true);
    private static readonly LineupTestModule _lineupModule = new(43, "lineup_test");
    private static readonly GaugeTestModule _lineupGauge = new(44, "lineup_gauge", isMechanic: false);

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    public CharacterMechanicTests()
    {
        SetLineup();
    }

    [Fact]
    public void AMechanicLoadsWithItsConfig()
    {
        SetMechanic("\"name\": \"test_mechanic\", \"channel\": 2, \"power\": 9");

        ModuleSlot mechanic = Load().Character(Fixture.CharacterId).Mechanic;

        Assert.Equal((_mechanic.Id, 2, 9), (mechanic.Module, mechanic.Config[0], mechanic.Config[1]));
    }

    [Fact]
    public void ACharacterWithoutAMechanicHasAnEmptySlot()
    {
        Assert.True(Load().Character(Fixture.CharacterId).Mechanic.Module.IsNone);
    }

    [Theory]
    [InlineData("\"name\": \"no_such_mechanic\"", "mechanic.name")]
    [InlineData("\"name\": \"lineup_test\"", "mechanic.name")]
    [InlineData("\"name\": \"test_mechanic\", \"channel\": 2", "mechanic.power")]
    [InlineData("\"name\": \"test_mechanic\", \"channel\": 2, \"power\": 11", "mechanic.power")]
    [InlineData("\"name\": \"test_mechanic\", \"channel\": 2, \"power\": 1, \"powr\": 1", "mechanic.powr")]
    [InlineData("\"channel\": 2, \"power\": 1", "mechanic.name")]
    public void AWrongMechanicIsReported(string mechanic, string field)
    {
        SetMechanic(mechanic);

        DataError error = SingleError();
        Assert.Equal((Fixture.CharacterFile, field), (error.File, error.Field));
    }

    [Fact]
    public void TheLineupCantNameACharacterMechanic()
    {
        _data.SetText(ModuleLineupSchema.OrderFile, """{ "schemaVersion": 1, "order": ["test_mechanic"] }""");

        DataError error = SingleError();
        Assert.Equal((ModuleLineupSchema.OrderFile, "order[0]"), (error.File, error.Field));
    }

    [Fact]
    public void AMechanicGaugeOnAnEnabledLineupGaugesChannelIsReported()
    {
        SetLineup((_lineupGauge.Name, true, ", \"channel\": 3, \"power\": 0"));
        SetMechanic("\"name\": \"gauge_mechanic\", \"channel\": 3, \"power\": 0");

        DataError error = SingleError();
        Assert.Equal((Fixture.CharacterFile, "mechanic.channel"), (error.File, error.Field));
    }

    [Fact]
    public void AMechanicGaugeBesideALineupGaugeOnAnotherChannelLoads()
    {
        SetLineup((_lineupGauge.Name, true, ", \"channel\": 0, \"power\": 0"));
        SetMechanic("\"name\": \"gauge_mechanic\", \"channel\": 3, \"power\": 0");

        Assert.Equal(_gaugeMechanic.Id, Load().Character(Fixture.CharacterId).Mechanic.Module);
    }

    [Fact]
    public void ACharacterMissingARoleOfItsMechanicIsReported()
    {
        SetMechanic("\"name\": \"test_mechanic\", \"channel\": 0, \"power\": 0");
        var needsRole = new MechanicTestModule(41, "test_mechanic", role: "install");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(
            () => new GameDataLoader(_data, Catalog(needsRole)).Load()).Errors);
        Assert.Equal((Fixture.CharacterFile, "moveRoles"), (error.File, error.Field));
    }

    [Fact]
    public void TheMechanicsConfigIsInTheDataHash()
    {
        SetMechanic("\"name\": \"test_mechanic\", \"channel\": 0, \"power\": 1");
        GameData before = Load();

        SetMechanic("\"name\": \"test_mechanic\", \"channel\": 0, \"power\": 2");

        Assert.NotEqual(before.Hash, Load().Hash);
    }

    private void SetMechanic(string fields)
    {
        string character = Fixture.InMemory().GetText(Fixture.CharacterFile);
        _data.SetText(Fixture.CharacterFile, character.Replace(
            "\"moveRoles\": {}", $"\"moveRoles\": {{}}, \"mechanic\": {{ {fields} }}", StringComparison.Ordinal));
    }

    private void SetLineup(params (string Name, bool Enabled, string Config)[] modules)
    {
        string order = string.Join(", ", modules.Select(module => $"\"{module.Name}\""));
        _data.SetText(ModuleLineupSchema.OrderFile, $$"""{ "schemaVersion": 1, "order": [{{order}}] }""");
        string modulesFolder = ModuleLineupSchema.ModulesDirectory + "/";
        foreach (string path in _data.Paths.Where(path => path.StartsWith(modulesFolder, StringComparison.Ordinal)).ToList())
        {
            _data.Remove(path);
        }

        foreach ((string name, bool enabled, string config) in modules)
        {
            _data.SetText(
                ModuleLineupSchema.ModuleFile(name),
                $$"""{ "schemaVersion": 1, "enabled": {{(enabled ? "true" : "false")}}{{config}} }""");
        }
    }

    private static ModuleCatalog Catalog(MechanicTestModule? mechanic = null) =>
        new(mechanic ?? _mechanic, _gaugeMechanic, _lineupModule, _lineupGauge);

    private GameData Load() => new GameDataLoader(_data, Catalog()).Load();

    private DataError SingleError() => Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);

    private static ModuleConfigLayout Layout() => ModuleConfigLayout.For<LineupTestConfig>(
        new ModuleConfigField("channel", 0, IMeterGauge.ChannelCount - 1), new ModuleConfigField("power", 0, 10));

    private sealed class MechanicTestModule(int id, string name, string? role = null) : IMechanicModule
    {
        public ModuleId Id { get; } = new(id);

        public string Name { get; } = name;

        public bool IsCharacterMechanic => true;

        public IReadOnlyList<string> MoveRoles { get; } = role is null ? [] : [role];

        public ModuleConfigLayout ConfigLayout { get; } = Layout();
    }

    private sealed class GaugeTestModule(int id, string name, bool isMechanic) : IMechanicModule, IMeterGauge
    {
        public ModuleId Id { get; } = new(id);

        public string Name { get; } = name;

        public bool IsCharacterMechanic { get; } = isMechanic;

        public ModuleConfigLayout ConfigLayout { get; } = Layout();

        public int ChannelOf(ref readonly ModuleConfig config) => config[0];

        public int MeterOf(ModuleContext context, int fighter) => 0;

        public bool TrySpend(ModuleContext context, int fighter, int amount) => false;
    }
}
