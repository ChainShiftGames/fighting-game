using System;
using System.Linq;
using Scenarios.Tests.Expectations;

namespace Scenarios.Tests.Runner;

/// <summary>Reads what follows <c>expect</c>: an optional player, a keyword, then whatever that kind takes.</summary>
/// <remarks>The keyword picks the <see cref="IExpectationKind"/>; the kind parses the rest, so kinds plug in here.</remarks>
public static class ExpectationParser
{
    public static IExpectation Parse(string text, ScenarioSetup setup, ScenarioLocation location)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int player = ScenarioPlayers.None;
        bool hasPlayer = words.Length > 0 && ScenarioPlayers.TryParse(words[0], out player);
        string[] rest = hasPlayer ? words[1..] : words;
        if (rest.Length == 0 || !ExpectationKinds.TryFind(rest[0], out IExpectationKind? kind))
        {
            string what = rest.Length == 0 ? "nothing" : $"'{rest[0]}'";
            string known = string.Join(", ", ExpectationKinds.All.Select(k => k.Keyword));
            throw new ScenarioSyntaxException(location, $"can't expect {what}; expectation kinds: {known}");
        }

        RequireSubject(kind!, player, location);
        string description = string.Join(' ', words);
        return kind!.Parse(new ExpectationSyntax(description, player, rest[0], rest[1..], setup, location));
    }

    /// <summary>Fighter kinds need <c>p1</c> or <c>p2</c>; match kinds must not have one.</summary>
    public static void RequireSubject(IExpectationKind kind, int player, ScenarioLocation location)
    {
        if (kind.IsPerFighter && player == ScenarioPlayers.None)
        {
            throw new ScenarioSyntaxException(location, $"'{kind.Keyword}' belongs to a fighter; write 'p1 {kind.Keyword}'");
        }

        if (!kind.IsPerFighter && player != ScenarioPlayers.None)
        {
            throw new ScenarioSyntaxException(location, $"'{kind.Keyword}' belongs to the match; drop the player");
        }
    }

    public static int ParseValue(
        ValueKind kind, string text, ScenarioSetup setup, int player, ScenarioLocation location)
    {
        try
        {
            return kind.ParseValue(text, setup, player);
        }
        catch (FormatException exception)
        {
            throw new ScenarioSyntaxException(location, exception.Message);
        }
    }
}
