using System;
using System.Collections.Generic;
using System.Linq;
using Sim.Characters;
using Sim.Data.Modules;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Modules;
using Sim.Rules;
using Xunit;

namespace Sim.Data.Tests.Modules;

/// <summary>ADR-0023: characters name moves for module roles, and meter gauges serve one channel each.</summary>
public sealed class MoveRoleCheckTests
{
    private static readonly RoleTestModule _dodger = new(31, "dodger", "dodge");
    private static readonly ChannelTestGauge _mainGauge = new(32, "main_gauge");
    private static readonly ChannelTestGauge _otherGauge = new(33, "other_gauge");

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    public MoveRoleCheckTests()
    {
        SetModules((_dodger.Name, false, ""), (_mainGauge.Name, false, ", \"channel\": 0"),
            (_otherGauge.Name, false, ", \"channel\": 0"));
    }

    [Fact]
    public void ARoleBecomesTheIndexOfItsMove()
    {
        SetRoles("\"dodge\": \"5a\"");

        CharacterData character = Load().Character(Fixture.CharacterId);

        Assert.Equal(1, character.MoveForRole("dodge"));
        Assert.Equal(CharacterData.NoMove, character.MoveForRole("burst"));
    }

    [Fact]
    public void ARoleNoModuleAsksForIsReported()
    {
        SetRoles("\"dodge\": \"5a\", \"dogde\": \"5a\"");

        DataError error = SingleError();
        Assert.Equal((Fixture.CharacterFile, "moveRoles.dogde"), (error.File, error.Field));
    }

    [Fact]
    public void ARoleNamingAMoveOutsideTheMoveListIsReported()
    {
        SetRoles("\"dodge\": \"5z\"");

        DataError error = SingleError();
        Assert.Equal((Fixture.CharacterFile, "moveRoles.dodge"), (error.File, error.Field));
    }

    [Fact]
    public void ACharacterWithoutARoleAnEnabledModuleNeedsIsReported()
    {
        SetModules((_dodger.Name, true, ""));

        DataError error = SingleError();
        Assert.Equal((Fixture.CharacterFile, "moveRoles"), (error.File, error.Field));
        Assert.Contains(_dodger.Name, error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ADisabledModuleNeedsNoRole()
    {
        Assert.Equal(CharacterData.NoMove, Load().Character(Fixture.CharacterId).MoveForRole("dodge"));
    }

    [Fact]
    public void TwoEnabledGaugesOnOneChannelAreReported()
    {
        SetModules((_mainGauge.Name, true, ", \"channel\": 2"), (_otherGauge.Name, true, ", \"channel\": 2"));

        DataError error = SingleError();
        Assert.Equal(ModuleLineupSchema.ModuleFile(_otherGauge.Name), error.File);
    }

    [Fact]
    public void GaugesOnDifferentChannelsLoad()
    {
        SetModules((_mainGauge.Name, true, ", \"channel\": 0"), (_otherGauge.Name, true, ", \"channel\": 1"));

        MatchRules rules = Load().CreateMatchRules();

        Assert.Equal((_mainGauge.Id, _otherGauge.Id), (rules.Modules[0].Module, rules.Modules[1].Module));
    }

    private void SetRoles(string roles) =>
        _data.Replace(Fixture.CharacterFile, "\"moveRoles\": {}", $"\"moveRoles\": {{ {roles} }}");

    private void SetModules(params (string Name, bool Enabled, string Config)[] modules)
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

    private GameData Load() => new GameDataLoader(_data, new ModuleCatalog(_dodger, _mainGauge, _otherGauge)).Load();

    private DataError SingleError() => Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);

    private sealed class RoleTestModule(int id, string name, string role) : IMechanicModule
    {
        public ModuleId Id { get; } = new(id);

        public string Name { get; } = name;

        public IReadOnlyList<string> MoveRoles { get; } = [role];
    }

    private sealed class ChannelTestGauge(int id, string name) : IMechanicModule, IMeterGauge
    {
        public ModuleId Id { get; } = new(id);

        public string Name { get; } = name;

        public ModuleConfigLayout ConfigLayout { get; } =
            ModuleConfigLayout.For<ChannelTestConfig>(new ModuleConfigField("channel", 0, IMeterGauge.ChannelCount - 1));

        public int ChannelOf(ref readonly ModuleConfig config) => ModuleConfigView.As<ChannelTestConfig>(in config).Channel;

        public int MeterOf(ModuleContext context, int fighter) => 0;

        public bool TrySpend(ModuleContext context, int fighter, int amount) => false;
    }
}
