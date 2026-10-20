using System;
using System.Collections.Generic;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.World.Sparks;
using Sim.Characters;
using Sim.Events;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation.World.Sparks;

public sealed class HitSparkLooksTests
{
    private static readonly CharacterData _testman = TestGameData.Testman;
    private static readonly IntVector2 _contact = new(10_000, 80_000);

    [Fact]
    public void ALightNormalBurstsForwardAndAHeavierHitWeighsMore()
    {
        HitSparkLooks looks = Looks();

        HitSparkLook jab = looks.Of(Spark(FrameEventKind.Hit, "5a"));
        HitSparkLook heavy = looks.Of(Spark(FrameEventKind.Hit, "5c"));

        Assert.Equal((HitSparkStyle.Burst, HitSparkTone.Hit, 0f, 0f), (jab.Style, jab.Tone, jab.Weight, jab.Angle));
        Assert.True(heavy.Weight > jab.Weight);
    }

    [Fact]
    public void SpecialsTearAndBlocksBurst()
    {
        HitSparkLooks looks = Looks();

        Assert.Equal(HitSparkStyle.Tear, looks.Of(Spark(FrameEventKind.Hit, "623c")).Style);
        HitSparkLook blocked = looks.Of(Spark(FrameEventKind.Block, "623c"));
        Assert.Equal((HitSparkStyle.Burst, HitSparkTone.Block), (blocked.Style, blocked.Tone));
        Assert.True(blocked.Weight < looks.Of(Spark(FrameEventKind.Hit, "623c")).Weight);
    }

    [Fact]
    public void ACounterHitIsHeavierAndInTheCounterTone()
    {
        HitSparkLooks looks = Looks();

        HitSparkLook plain = looks.Of(Spark(FrameEventKind.Hit, "5a"));
        HitSparkLook counter = looks.Of(Spark(FrameEventKind.Hit, "5a", counter: FrameEventKind.CounterHit));

        Assert.Equal(HitSparkTone.Counter, counter.Tone);
        Assert.True(counter.Weight > plain.Weight);
    }

    [Fact]
    public void TheArtFileSetsTheHitsAngleStyleOffsetAndScale()
    {
        var settings = new HitSparkSettings(Angle: 90f, OffsetX: 5f, OffsetY: -2f, HitSparkStyle.Tear, Scale: 1.5f);
        HitSparkLooks looks = Looks(("5a", settings));

        HitSparkLook look = looks.Of(Spark(FrameEventKind.Hit, "5a"));

        Assert.Equal(HitSparkStyle.Tear, look.Style);
        Assert.Equal(MathF.PI / 2f, look.Angle, 5);
        Assert.Equal(_contact + new IntVector2(5_000, -2_000), look.Position);
        Assert.Equal(1.5f, look.Scale);
    }

    [Fact]
    public void FacingLeftMirrorsTheAngleAndOffset()
    {
        var settings = new HitSparkSettings(Angle: 30f, OffsetX: 5f);
        HitSparkLooks looks = Looks(("5a", settings));

        HitSparkLook look = looks.Of(Spark(FrameEventKind.Hit, "5a", fighter: 1, facing: -1));

        Assert.Equal(MathF.PI - (MathF.PI / 6f), look.Angle, 5);
        Assert.Equal(_contact + new IntVector2(-5_000, 0), look.Position);
    }

    [Fact]
    public void TheSameEventAlwaysGetsTheSameSeedAndOthersDiffer()
    {
        HitSparkLooks looks = Looks();

        float first = looks.Of(Spark(FrameEventKind.Hit, "5a", frame: 100)).Seed;

        Assert.Equal(first, looks.Of(Spark(FrameEventKind.Hit, "5a", frame: 100)).Seed);
        Assert.NotEqual(first, looks.Of(Spark(FrameEventKind.Hit, "5a", frame: 101)).Seed);
    }

    [Fact]
    public void AThrowTechHasNoHitAndBurstsInTheClashTone()
    {
        HitSparkLook look = Looks().Of(Spark(FrameEventKind.ThrowTech, detail: 0));

        Assert.Equal((HitSparkStyle.Burst, HitSparkTone.Clash), (look.Style, look.Tone));
    }

    private static HitSparkLooks Looks(params (string Move, HitSparkSettings Settings)[] sparks)
    {
        var byMove = new Dictionary<string, IReadOnlyList<HitSparkSettings>>();
        foreach ((string move, HitSparkSettings settings) in sparks)
        {
            byMove[move] = [settings];
        }

        return new HitSparkLooks([_testman, _testman], [byMove, byMove], TestEffectSettings.Standard.Sparks);
    }

    private static LiveSpark Spark(
        FrameEventKind kind,
        string move,
        int fighter = 0,
        int facing = 1,
        FrameEventKind counter = FrameEventKind.None,
        int frame = 10) =>
        Spark(kind, HitEventDetail.Of(MoveIndex(move), 0), fighter, facing, counter, frame);

    private static LiveSpark Spark(
        FrameEventKind kind,
        int detail,
        int fighter = 0,
        int facing = 1,
        FrameEventKind counter = FrameEventKind.None,
        int frame = 10)
    {
        var sparks = new HitSparks();
        var fighters = default(FighterStates);
        fighters[fighter].Facing = facing;
        sparks.Play(new FrameEvent(frame, kind, fighter, occurrence: 0, _contact, detail));
        if (counter != FrameEventKind.None)
        {
            sparks.Play(new FrameEvent(frame, counter, fighter, occurrence: 0, _contact, detail: 0));
        }

        return sparks.Live(frame, in fighters)[0];
    }

    private static int MoveIndex(string move) => Array.IndexOf(_testman.MoveIds.ToArray(), move);
}
