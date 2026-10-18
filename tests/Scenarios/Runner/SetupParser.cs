using System;
using System.Globalization;
using System.Linq;
using Scenarios.Tests.Expectations;
using Sim.Characters;
using Sim.Data.Rules;
using Sim.Modules;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>Reads the body of a <c>setup:</c> line: a character, a module, a match rule or a start-state value.</summary>
/// <remarks>
/// Forms: <c>p1 character &lt;id&gt;</c>, <c>module &lt;name&gt; [config integers…]</c>, <c>trade &lt;rule&gt;</c>,
/// <c>entities_per_player &lt;n&gt;</c>, <c>throw_vs_strike &lt;rule&gt;</c>, <c>shortcut_damage &lt;percent&gt;</c>,
/// <c>shortcut_cooldown &lt;frames&gt;</c>, <c>air_recovery &lt;0|1&gt;</c>, <c>max_player_distance &lt;subpixels&gt;</c> and
/// <c>[p1|p2] &lt;keyword&gt; = &lt;value&gt;</c> for any expectation kind that is also an <see cref="ISettableKind"/>.
/// </remarks>
public sealed class SetupParser
{
    private readonly ScenarioData _data;
    private readonly ModuleCatalog _modules = RegisteredModules.Create();

    public SetupParser(ScenarioData data)
    {
        _data = data;
    }

    public void Parse(string body, ScenarioSetup setup, ScenarioLocation location)
    {
        string[] words = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool hasPlayer = ScenarioPlayers.TryParse(words[0], out int player);
        string[] rest = hasPlayer ? words[1..] : words;
        if (rest.Length == 0)
        {
            throw new ScenarioSyntaxException(location, $"'{body}' says nothing to set up");
        }

        switch (rest[0])
        {
            case "character":
                SetCharacter(rest, player, setup, location);
                break;
            case "module":
                RequireNoPlayer(hasPlayer, "modules are enabled for the whole match", location);
                EnableModule(rest, setup, location);
                break;
            case "trade":
                RequireNoPlayer(hasPlayer, "the trade rule is a match rule", location);
                SetTradeRule(rest, setup, location);
                break;
            case "throw_vs_strike":
                RequireNoPlayer(hasPlayer, "the throw against strike rule is a match rule", location);
                SetThrowVsStrike(rest, setup, location);
                break;
            case "entities_per_player":
                RequireNoPlayer(hasPlayer, "the entity pool size is a match rule", location);
                SetEntitiesPerPlayer(rest, setup, location);
                break;
            case "shortcut_damage":
                RequireNoPlayer(hasPlayer, "the shortcut trade-off is a match rule", location);
                setup.SetShortcutDamagePercent(ReadNumber(rest, 1, MatchRulesSchema.MaxShortcutDamagePercent, location));
                break;
            case "shortcut_cooldown":
                RequireNoPlayer(hasPlayer, "the shortcut trade-off is a match rule", location);
                setup.SetShortcutCooldownFrames(ReadNumber(rest, 0, MatchRulesSchema.MaxShortcutCooldownFrames, location));
                break;
            case "air_recovery":
                RequireNoPlayer(hasPlayer, "air recovery is a match rule", location);
                setup.SetAirRecovery(ReadNumber(rest, 0, 1, location) == 1);
                break;
            case "max_player_distance":
                RequireNoPlayer(hasPlayer, "the screen width is a match rule", location);
                setup.SetMaxPlayerDistance(ReadNumber(rest, 1, 2 * MatchRulesSchema.MaxDistance, location));
                break;
            default:
                AddValue(rest, player, setup, location);
                break;
        }
    }

    private void SetCharacter(string[] words, int player, ScenarioSetup setup, ScenarioLocation location)
    {
        if (player == ScenarioPlayers.None || words.Length != 2)
        {
            throw new ScenarioSyntaxException(location, "write 'setup: p1 character <id>'");
        }

        if (!_data.TryGetCharacter(words[1], out CharacterData? character))
        {
            throw new ScenarioSyntaxException(
                location, $"no character '{words[1]}'; known: {string.Join(", ", _data.CharacterIds)}");
        }

        setup.SetCharacter(player, character!, location);
    }

    private void EnableModule(string[] words, ScenarioSetup setup, ScenarioLocation location)
    {
        if (words.Length < 2 || !_modules.TryFindId(words[1], out ModuleId id))
        {
            string known = string.Join(", ", Enumerable.Range(0, _modules.Count).Select(i => _modules[i].Name));
            throw new ScenarioSyntaxException(location, $"write 'setup: module <name> [config…]'; modules: {known}");
        }

        string[] config = words[2..];
        if (config.Length > ModuleConfig.Length)
        {
            throw new ScenarioSyntaxException(location, $"a module has at most {ModuleConfig.Length} config values");
        }

        int[] values = new int[config.Length];
        for (int i = 0; i < config.Length; i++)
        {
            if (!int.TryParse(config[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out values[i]))
            {
                throw new ScenarioSyntaxException(location, $"module config value '{config[i]}' isn't an integer");
            }
        }

        setup.EnableModule(new EnabledModule(words[1], id, values), location);
    }

    private static void SetTradeRule(string[] words, ScenarioSetup setup, ScenarioLocation location)
    {
        if (words.Length != 2)
        {
            throw new ScenarioSyntaxException(location, "write 'setup: trade <rule>', e.g. 'setup: trade clash'");
        }

        try
        {
            setup.SetTradeRule(SnakeCaseNames.Parse<TradeRule>(words[1], "trade rule"));
        }
        catch (FormatException exception)
        {
            throw new ScenarioSyntaxException(location, exception.Message);
        }
    }

    private static void SetThrowVsStrike(string[] words, ScenarioSetup setup, ScenarioLocation location)
    {
        if (words.Length != 2)
        {
            throw new ScenarioSyntaxException(
                location, "write 'setup: throw_vs_strike <rule>', e.g. 'setup: throw_vs_strike strike'");
        }

        try
        {
            setup.SetThrowVsStrike(SnakeCaseNames.Parse<ThrowPriority>(words[1], "throw against strike rule"));
        }
        catch (FormatException exception)
        {
            throw new ScenarioSyntaxException(location, exception.Message);
        }
    }

    private static void SetEntitiesPerPlayer(string[] words, ScenarioSetup setup, ScenarioLocation location)
    {
        int slots = -1;
        bool isValid = words.Length == 2 &&
            int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out slots) &&
            slots <= EntityPool.MaxPerPlayer;
        if (!isValid)
        {
            throw new ScenarioSyntaxException(
                location, $"write 'setup: entities_per_player <n>' with n from 0 to {EntityPool.MaxPerPlayer}");
        }

        setup.SetEntitiesPerPlayer(slots);
    }

    // "<keyword> <n>" with n in range, for match rules that are one number.
    private static int ReadNumber(string[] words, int min, int max, ScenarioLocation location)
    {
        int value = -1;
        bool isValid = words.Length == 2 &&
            int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out value) &&
            value >= min && value <= max;
        if (!isValid)
        {
            throw new ScenarioSyntaxException(location, $"write 'setup: {words[0]} <n>' with n from {min} to {max}");
        }

        return value;
    }

    private static void AddValue(string[] words, int player, ScenarioSetup setup, ScenarioLocation location)
    {
        bool isSettable = ExpectationKinds.TryFind(words[0], out IExpectationKind? kind) && kind is ISettableKind;
        if (!isSettable || kind is not ValueKind valueKind)
        {
            string settableKinds = string.Join(
                ", ", ExpectationKinds.Values.Where(k => k is ISettableKind).Select(k => k.Keyword));
            throw new ScenarioSyntaxException(
                location,
                $"can't set '{words[0]}' in setup; settable: character, module, trade, throw_vs_strike, " +
                "entities_per_player, shortcut_damage, shortcut_cooldown, air_recovery, max_player_distance, " +
                settableKinds);
        }

        ExpectationParser.RequireSubject(valueKind, player, location);
        if (words.Length != 3 || words[1] != "=")
        {
            throw new ScenarioSyntaxException(location, $"write 'setup: {Subject(player)}{words[0]} = <value>'");
        }

        int value = ExpectationParser.ParseValue(valueKind, words[2], setup, player, location);
        setup.AddValue(new SetupValue((ISettableKind)valueKind, player, value));
    }

    private static void RequireNoPlayer(bool hasPlayer, string reason, ScenarioLocation location)
    {
        if (hasPlayer)
        {
            throw new ScenarioSyntaxException(location, $"no player here: {reason}");
        }
    }

    private static string Subject(int player) =>
        player == ScenarioPlayers.None ? "" : ScenarioPlayers.Label(player) + " ";
}
