using System.Collections.Generic;
using System.Linq;
using FightingGame.VfxLab;
using FightingGame.VfxLab.Files;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class LabSessionTests
{
    private const string Json = """
        {
          "labVersion": 1,
          "style": { "character": "testman", "palette": { "accent_color": "#ffffff", "glow_color": "#000000" } },
          "rounds": [
            { "id": "r1", "name": "Round 1", "variants": [
              { "id": "a", "flight": { "shader": "a.gdshader" }, "values": { "accent_color": "#ff0000", "aura": 1 } },
              { "id": "b", "flight": { "shader": "b.gdshader" } }
            ] },
            { "id": "r2", "name": "Round 2", "variants": [ { "id": "a2", "extends": "a", "values": { "aura": 2 } } ] }
          ]
        }
        """;

    [Fact]
    public void DrawsWithThePaletteThenTheVariantThenTheDevsChanges()
    {
        LabSession session = Open();
        session.Feedback.For("a").Values["aura"] = LabValue.Number(5f);

        Dictionary<string, LabValue> values = session.ValuesFor(session.Variants["a"], lockPalette: false);
        Dictionary<string, LabValue> locked = session.ValuesFor(session.Variants["a"], lockPalette: true);

        Assert.Equal([1f, 0f, 0f, 1f], values["accent_color"].Numbers);
        Assert.Equal([0f, 0f, 0f, 1f], values["glow_color"].Numbers);
        Assert.Equal(5f, values["aura"].First);
        Assert.Equal([1f, 1f, 1f, 1f], locked["accent_color"].Numbers);
    }

    [Fact]
    public void ShowsARoundPicksAndHidesRejects()
    {
        LabSession session = Open();
        session.Feedback.For("b").Hidden = true;
        session.Feedback.For("a2").Pick = true;

        Assert.Equal(["a", "a2"], Ids(session, new LabViewOptions()));
        Assert.Equal(["a", "b", "a2"], Ids(session, new LabViewOptions { ShowHidden = true }));
        Assert.Equal(["a2"], Ids(session, new LabViewOptions { Round = "r2" }));
        Assert.Equal(["a2"], Ids(session, new LabViewOptions { PicksOnly = true }));
        Assert.Equal(["b"], Ids(session, new LabViewOptions { Solo = "b" }));
    }

    [Fact]
    public void AForkKeepsTheChangesAndExtendsItsVariant()
    {
        LabSession session = Open();
        session.Feedback.For("a2").Values["aura"] = LabValue.Number(3f);

        LabVariant fork = session.Fork("a2");
        LabVariant again = session.Fork("a2");

        Assert.Equal("a2_fork1", fork.Id);
        Assert.Equal("a2_fork2", again.Id);
        Assert.Equal(3f, fork.Values["aura"].First);
        Assert.Equal([1f, 0f, 0f, 1f], fork.Values["accent_color"].Numbers);
        Assert.Equal(["a2_fork1", "a2_fork2"], Ids(session, new LabViewOptions { Round = LabSession.ForkRound }));
        Assert.Equal("Fork", session.RoundNameOf(fork.Id));
    }

    [Fact]
    public void LeavesOutAForkOfAVariantThatIsGone()
    {
        LabFeedback feedback = new();
        feedback.Forks.Add(new LabFork("gone_fork1", "gone", "Gone", new Dictionary<string, LabValue>()));

        LabSession session = new(LabSheetFile.Read(Json, "res://x", out _)!, feedback);

        Assert.False(session.Variants.ContainsKey("gone_fork1"));
        Assert.Contains("gone_fork1", session.Problem);
    }

    [Fact]
    public void AppliesTheDevsTimingChanges()
    {
        LabSession session = Open();
        session.Feedback.Timing["speed"] = 9f;
        session.Feedback.Timing["flightFrames"] = 40f;

        Assert.Equal(9f, session.Timing.Speed);
        Assert.Equal(40, session.Timing.Flight);
    }

    private static LabSession Open() => new(LabSheetFile.Read(Json, "res://x", out _)!, new LabFeedback());

    private static string[] Ids(LabSession session, LabViewOptions options) => session.Shown(options).Select(v => v.Id).ToArray();
}
