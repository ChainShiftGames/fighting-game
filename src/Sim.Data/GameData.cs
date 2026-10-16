using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Hashing;
using Sim.Data.Motions;
using Sim.Data.Rules;
using Sim.Input;
using Sim.Rules;

namespace Sim.Data;

/// <summary>All loaded game data in compact form, plus its hash.</summary>
public sealed class GameData
{
    public GameData(
        ButtonSet buttons,
        MotionFile motions,
        MatchRulesFile rules,
        IReadOnlyList<CharacterData> characters,
        DataHash hash)
    {
        Buttons = buttons;
        Motions = motions;
        Rules = rules;
        Characters = characters;
        Hash = hash;
    }

    /// <summary>The buttons of the input word (ADR-0010).</summary>
    public ButtonSet Buttons { get; }

    /// <summary>Motions, their priority, input timing and movement inputs (ADR-0002).</summary>
    public MotionFile Motions { get; }

    /// <summary>Match-wide gameplay values from rules/match.json.</summary>
    public MatchRulesFile Rules { get; }

    /// <summary>Sorted by id (ordinal), the same on every platform.</summary>
    public IReadOnlyList<CharacterData> Characters { get; }

    public DataHash Hash { get; }

    /// <summary>Match rules with every value the data sets; enabling modules is up to the caller.</summary>
    public MatchRules CreateMatchRules()
    {
        MatchRules rules = default;
        Motions.ApplyTo(ref rules);
        Rules.ApplyTo(ref rules);
        return rules;
    }

    public CharacterData Character(string id)
    {
        foreach (CharacterData character in Characters)
        {
            if (string.Equals(character.Id, id, StringComparison.Ordinal))
            {
                return character;
            }
        }

        throw new KeyNotFoundException($"No character with id '{id}' was loaded.");
    }
}
