using System;
using System.Collections.Generic;
using System.Linq;
using Sim.Characters;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Input;

namespace Scenarios.Tests.Runner;

/// <summary>The data scenarios run with: the real data/ folder plus the scenario fixture characters, loaded once.</summary>
public sealed class ScenarioData
{
    private static readonly Lazy<ScenarioData> _shared = new(LoadShared);

    private readonly GameData _data;

    public ScenarioData(GameData data)
    {
        _data = data;
    }

    public static ScenarioData Shared => _shared.Value;

    /// <summary>The button set from data/input/buttons.json; input notation uses its ids.</summary>
    public ButtonSet Buttons => _data.Buttons;

    public IEnumerable<string> CharacterIds => _data.Characters.Select(character => character.Id);

    public bool TryGetCharacter(string id, out CharacterData? character)
    {
        character = _data.Characters.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        return character is not null;
    }

    private static ScenarioData LoadShared()
    {
        LayeredDataSource source = new(
            new DirectoryDataSource(ScenarioPaths.RepositoryDataRoot),
            new DirectoryDataSource(ScenarioPaths.FixtureDataRoot));
        return new ScenarioData(new GameDataLoader(source).Load());
    }
}
