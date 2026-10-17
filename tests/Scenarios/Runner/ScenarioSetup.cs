using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Motions;
using Sim.Input;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>What a scenario's <c>setup:</c> lines chose: characters, match rules and start-state values.</summary>
public sealed class ScenarioSetup
{
    private readonly CharacterData?[] _characters = new CharacterData?[MatchState.PlayerCount];
    private readonly List<EnabledModule> _modules = [];
    private readonly List<SetupValue> _values = [];
    private readonly MatchRules _dataRules;
    private TradeRule? _tradeRule;
    private int? _entitiesPerPlayer;
    private ThrowPriority? _throwVsStrike;
    private int? _shortcutDamagePercent;
    private int? _shortcutCooldownFrames;
    private bool? _airRecovery;

    public ScenarioSetup(ButtonSet buttons, MotionFile motions, MatchRules dataRules)
    {
        Buttons = buttons;
        Motions = motions;
        _dataRules = dataRules;
    }

    /// <summary>The button set the match runs with (from data).</summary>
    public ButtonSet Buttons { get; }

    /// <summary>The motions and input timing the match runs with (from data).</summary>
    public MotionFile Motions { get; }

    /// <summary>In lineup order: the module at index i runs in slot i.</summary>
    public IReadOnlyList<EnabledModule> Modules => _modules;

    /// <summary>In file order; a later line for the same value wins.</summary>
    public IReadOnlyList<SetupValue> Values => _values;

    /// <summary>The player's character; every parsed scenario has both.</summary>
    public CharacterData Character(int player) => _characters[player]!;

    /// <summary>The lineup slot of an enabled module, or -1.</summary>
    public int ModuleSlot(string name) => _modules.FindIndex(module => module.Name == name);

    /// <summary>
    /// The match rules: the values from data, the trade rule, entity pool size, throw against strike rule and special
    /// button shortcut trade-off if set,
    /// then the modules in lineup order.
    /// </summary>
    /// <remarks>
    /// Only the modules the scenario enables run, never the lineup in data: switching a module there for development
    /// must not change what a scenario proves.
    /// </remarks>
    public MatchRules BuildRules()
    {
        MatchRules rules = _dataRules;
        rules.TradeRule = _tradeRule ?? rules.TradeRule;
        rules.EntitiesPerPlayer = _entitiesPerPlayer ?? rules.EntitiesPerPlayer;
        rules.ThrowVsStrike = _throwVsStrike ?? rules.ThrowVsStrike;
        rules.ShortcutDamagePercent = _shortcutDamagePercent ?? rules.ShortcutDamagePercent;
        rules.ShortcutCooldownFrames = _shortcutCooldownFrames ?? rules.ShortcutCooldownFrames;
        rules.AirRecovery = _airRecovery is bool airRecovery ? (airRecovery ? 1 : 0) : rules.AirRecovery;
        rules.Modules = default;
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

        if (_modules.Count == ModuleStates.LineupSlots)
        {
            throw new ScenarioSyntaxException(location, $"at most {ModuleStates.LineupSlots} modules fit the lineup");
        }

        _modules.Add(module);
    }

    internal void AddValue(SetupValue value) => _values.Add(value);

    /// <summary>Overrides the trade rule from data; a later line wins.</summary>
    internal void SetTradeRule(TradeRule rule) => _tradeRule = rule;

    /// <summary>Overrides <c>entitiesPerPlayer</c> from data; a later line wins.</summary>
    internal void SetEntitiesPerPlayer(int slots) => _entitiesPerPlayer = slots;

    /// <summary>Overrides the throw against strike rule from data; a later line wins.</summary>
    internal void SetThrowVsStrike(ThrowPriority priority) => _throwVsStrike = priority;

    /// <summary>Overrides <c>shortcutDamagePercent</c> from data; a later line wins.</summary>
    internal void SetShortcutDamagePercent(int percent) => _shortcutDamagePercent = percent;

    /// <summary>Overrides <c>shortcutCooldownFrames</c> from data; a later line wins.</summary>
    internal void SetShortcutCooldownFrames(int frames) => _shortcutCooldownFrames = frames;

    internal void SetAirRecovery(bool on) => _airRecovery = on;

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
