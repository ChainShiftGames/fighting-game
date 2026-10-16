using System.Collections.Generic;
using Sim.Characters;
using Sim.Input;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>What a scenario's <c>setup:</c> lines chose: characters, enabled modules and start-state values.</summary>
public sealed class ScenarioSetup
{
    private readonly CharacterData?[] _characters = new CharacterData?[MatchState.PlayerCount];
    private readonly List<EnabledModule> _modules = [];
    private readonly List<SetupValue> _values = [];

    public ScenarioSetup(ButtonSet buttons)
    {
        Buttons = buttons;
    }

    /// <summary>The button set the match runs with (from data).</summary>
    public ButtonSet Buttons { get; }

    /// <summary>In lineup order: the module at index i runs in slot i.</summary>
    public IReadOnlyList<EnabledModule> Modules => _modules;

    /// <summary>In file order; a later line for the same value wins.</summary>
    public IReadOnlyList<SetupValue> Values => _values;

    /// <summary>The player's character; every parsed scenario has both.</summary>
    public CharacterData Character(int player) => _characters[player]!;

    /// <summary>The lineup slot of an enabled module, or -1.</summary>
    public int ModuleSlot(string name) => _modules.FindIndex(module => module.Name == name);

    /// <summary>The match rules: the enabled modules in lineup order, with their config.</summary>
    public MatchRules BuildRules()
    {
        MatchRules rules = default;
        for (int slot = 0; slot < _modules.Count; slot++)
        {
            rules.Modules[slot].Module = _modules[slot].Id;
            for (int i = 0; i < _modules[slot].Config.Length; i++)
            {
                rules.Modules[slot].Config[i] = _modules[slot].Config[i];
            }
        }

        return rules;
    }

    internal void SetCharacter(int player, CharacterData character, ScenarioLocation location)
    {
        if (_characters[player] is not null)
        {
            throw new ScenarioSyntaxException(location, $"{ScenarioPlayers.Label(player)} already has a character");
        }

        _characters[player] = character;
    }

    internal void EnableModule(EnabledModule module, ScenarioLocation location)
    {
        if (ModuleSlot(module.Name) >= 0)
        {
            throw new ScenarioSyntaxException(location, $"module {module.Name} is already enabled");
        }

        if (_modules.Count == ModuleStates.MaxModules)
        {
            throw new ScenarioSyntaxException(location, $"at most {ModuleStates.MaxModules} modules fit the lineup");
        }

        _modules.Add(module);
    }

    internal void AddValue(SetupValue value) => _values.Add(value);

    internal void RequireCharacters(string file)
    {
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            if (_characters[player] is null)
            {
                string label = ScenarioPlayers.Label(player);
                throw new ScenarioSyntaxException(
                    new ScenarioLocation(file, 0), $"no character for {label}; add 'setup: {label} character <id>'");
            }
        }
    }
}
