using System;
using System.Collections.Generic;
using Sim.Input;

namespace Scenarios.Tests.Runner;

/// <summary>Reads a scenario file line by line; the format is in tests/Scenarios/README.md.</summary>
/// <remarks>
/// Every line is <c>setup: …</c>, <c>frame N: …</c> or <c>frames A-B: …</c>; <c>#</c> starts a comment and case is
/// ignored. Setup lines come first. This class only splits lines and reads inputs: setup statements go to
/// <see cref="SetupParser"/> and expectations to <see cref="ExpectationParser"/>, so new expectation kinds never
/// change it. Anything it can't read throws <see cref="ScenarioSyntaxException"/> naming the line.
/// </remarks>
public sealed class ScenarioParser
{
    private const string ExpectWord = "expect";

    private readonly ScenarioData _data;
    private readonly SetupParser _setupParser;

    public ScenarioParser(ScenarioData data)
    {
        _data = data;
        _setupParser = new SetupParser(data);
    }

    public Scenario Parse(string file, string text)
    {
        ScenarioSetup setup = new(_data.Buttons, _data.Motions, _data.CreateMatchRules());
        ScenarioInputs inputs = new();
        List<FrameExpectation> expectations = [];
        bool setupDone = false;

        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = WithoutComment(lines[i]).Trim().ToLowerInvariant();
            if (line.Length == 0)
            {
                continue;
            }

            ScenarioLocation location = new(file, i + 1);
            (string head, string body) = SplitStatement(line, location);
            if (head == "setup")
            {
                if (setupDone)
                {
                    throw new ScenarioSyntaxException(location, "setup lines must come before the frame lines");
                }

                _setupParser.Parse(body, setup, location);
                continue;
            }

            if (!setupDone)
            {
                setup.RequireCharacters(file);
                setupDone = true;
            }

            FrameRange frames = FrameRange.Parse(head, location);
            if (IsExpectation(body))
            {
                string expectation = body[ExpectWord.Length..].Trim();
                expectations.Add(new(frames, ExpectationParser.Parse(expectation, setup, location), location));
            }
            else
            {
                ParseInputs(body, frames, inputs, location);
            }
        }

        if (expectations.Count == 0)
        {
            throw new ScenarioSyntaxException(
                new ScenarioLocation(file, 0), "the scenario checks nothing; add at least one 'frame N: expect …' line");
        }

        return new Scenario(file, setup, inputs, expectations);
    }

    private static string WithoutComment(string line)
    {
        int comment = line.IndexOf('#', StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }

    private static (string Head, string Body) SplitStatement(string line, ScenarioLocation location)
    {
        int colon = line.IndexOf(':', StringComparison.Ordinal);
        string body = colon < 0 ? "" : line[(colon + 1)..].Trim();
        if (colon < 0 || body.Length == 0)
        {
            throw new ScenarioSyntaxException(
                location, $"'{line}' isn't a statement; write 'setup: …', 'frame N: …' or 'frames A-B: …'");
        }

        return (line[..colon].Trim(), body);
    }

    private static bool IsExpectation(string body) =>
        body == ExpectWord || body.StartsWith(ExpectWord + " ", StringComparison.Ordinal);

    // "p1 6a" holds 6a on every frame of the range; "p1 2, 3, 6c" is one input per frame from the first frame.
    private static void ParseInputs(string body, FrameRange frames, ScenarioInputs inputs, ScenarioLocation location)
    {
        int space = body.IndexOf(' ', StringComparison.Ordinal);
        if (space < 0 || !ScenarioPlayers.TryParse(body[..space], out int player))
        {
            throw new ScenarioSyntaxException(
                location, $"'{body}' is neither 'expect …' nor an input like 'p1 6a'");
        }

        string[] sequence = body[(space + 1)..].Split(',', StringSplitOptions.TrimEntries);
        if (sequence.Length > 1 && frames.First != frames.Last)
        {
            throw new ScenarioSyntaxException(
                location, "a sequence ('2, 3, 6c') starts on one frame; write 'frame N:', not a range");
        }

        for (int frame = frames.First; frame <= frames.Last; frame++)
        {
            inputs.Set(player, frame, ParseInput(sequence[0], location), location);
        }

        for (int i = 1; i < sequence.Length; i++)
        {
            inputs.Set(player, frames.First + i, ParseInput(sequence[i], location), location);
        }
    }

    private static InputWord ParseInput(string text, ScenarioLocation location)
    {
        try
        {
            return InputNotation.Parse(text.Replace(" ", "", StringComparison.Ordinal));
        }
        catch (FormatException exception)
        {
            throw new ScenarioSyntaxException(location, exception.Message);
        }
    }
}
