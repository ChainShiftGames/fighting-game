using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Hashing;
using Sim.Data.Modules;
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
        ModuleLineupFile modules,
        IReadOnlyList<CharacterData> characters,
        DataHash hash)
    {
        Buttons = buttons;
        Motions = motions;
        Rules = rules;
        Modules = modules;
        Characters = characters;
        Hash = hash;
    }

    /// <summary>The buttons of the input word (ADR-0010).</summary>
    public ButtonSet Buttons { get; }

    /// <summary>Motions, their priority, input timing and movement inputs (ADR-0002).</summary>
    public MotionFile Motions { get; }

    /// <summary>Match-wide gameplay values from rules/match.json.</summary>
    public MatchRulesFile Rules { get; }

    /// <summary>Every module data knows, in hook order, switched on or off (rules/modules.json and rules/modules/).</summary>
    public ModuleLineupFile Modules { get; }

    /// <summary>Sorted by id (ordinal), the same on every platform.</summary>
    public IReadOnlyList<CharacterData> Characters { get; }

    public DataHash Hash { get; }

    /// <summary>Match rules with every value the data sets, including the enabled modules and their config.</summary>
    /// <remarks>
    /// Data is read again for every match, so a module switched in its data file applies when the round restarts, with
    /// a new rules hash. Exported builds read the embedded data, so their rules can't change (ADR-0005 §8).
    /// </remarks>
    public MatchRules CreateMatchRules()
    {
        MatchRules rules = default;
        Motions.ApplyTo(ref rules);
        Rules.ApplyTo(ref rules);
        Modules.ApplyTo(ref rules);
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
