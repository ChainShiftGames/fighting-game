using FightingGame.Online;
using FightingGame.Training;
using Netcode.Drivers;
using Sim.Diagnostics;
using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Training;

public sealed class FrameMeterTests
{
    private static readonly FighterFrameState _free = new(FrameKind.Free, false);
    private static readonly FighterFrameState _startup = new(FrameKind.Startup, false);
    private static readonly FighterFrameState _active = new(FrameKind.Active, false);
    private static readonly FighterFrameState _recovery = new(FrameKind.Recovery, false);
    private static readonly FighterFrameState _hitstun = new(FrameKind.Hitstun, false);

    [Fact]
    public void StaysEmptyWhileBothFightersAreFree()
    {
        var meter = new FrameMeter();
        meter.Record(_free, _free);

        Assert.Equal(0, meter.Count);
        Assert.Null(meter.Startup);
    }

    [Fact]
    public void MeasuresStartupAndAdvantage()
    {
        var meter = new FrameMeter();
        Record(meter, _startup, _free, 4);
        Record(meter, _active, _hitstun, 1);
        Record(meter, _recovery, _hitstun, 5);
        Record(meter, _free, _hitstun, 2);
        Assert.Null(meter.Advantage);

        meter.Record(_free, _free);

        Assert.Equal(5, meter.Startup);
        Assert.Equal(2, meter.Advantage);
        Assert.Equal(13, meter.Count);
        Assert.Equal(_active, meter.At(0, 4));
        Assert.Equal(_hitstun, meter.At(1, 4));
    }

    [Fact]
    public void AdvantageIsNegativeWhenTheOpponentActsFirst()
    {
        var meter = new FrameMeter();
        Record(meter, _active, _hitstun, 2);
        Record(meter, _recovery, _free, 3);
        meter.Record(_free, _free);

        Assert.Equal(-3, meter.Advantage);
    }

    [Fact]
    public void AWhiffHasNoAdvantage()
    {
        var meter = new FrameMeter();
        Record(meter, _startup, _free, 2);
        Record(meter, _active, _free, 2);
        meter.Record(_free, _free);

        Assert.Equal(3, meter.Startup);
        Assert.Null(meter.Advantage);
    }

    [Fact]
    public void TheNextActionStartsAFreshStretch()
    {
        var meter = new FrameMeter();
        Record(meter, _active, _hitstun, 3);
        meter.Record(_free, _free);
        meter.Record(_free, _free);

        meter.Record(_startup, _free);

        Assert.Equal(1, meter.Count);
        Assert.Null(meter.Startup);
        Assert.Null(meter.Advantage);
    }

    [Fact]
    public void KeepsOnlyTheNewestColumns()
    {
        var meter = new FrameMeter();
        Record(meter, _startup, _free, FrameMeter.Columns);
        meter.Record(_active, _free);

        Assert.Equal(FrameMeter.Columns, meter.Count);
        Assert.Equal(_active, meter.At(0, FrameMeter.Columns - 1));
        Assert.Equal(FrameMeter.Columns + 1, meter.Startup);
    }

    [Fact]
    public void ClearEmptiesIt()
    {
        var meter = new FrameMeter();
        Record(meter, _active, _hitstun, 3);

        meter.Clear();

        Assert.Equal(0, meter.Count);
        Assert.Null(meter.Startup);
    }

    [Fact]
    public void DescribesAnEmptyMeterWithDashes() =>
        Assert.Equal("Startup -   Advantage -", FrameMeterText.Describe(new FrameMeter()));

    [Fact]
    public void DescribesAdvantageWithItsSign()
    {
        var plus = new FrameMeter();
        Record(plus, _startup, _free, 4);
        Record(plus, _active, _hitstun, 1);
        Record(plus, _free, _hitstun, 2);
        plus.Record(_free, _free);
        var minus = new FrameMeter();
        Record(minus, _active, _hitstun, 1);
        Record(minus, _recovery, _free, 4);
        minus.Record(_free, _free);

        Assert.Equal("Startup 5   Advantage +2", FrameMeterText.Describe(plus));
        Assert.Equal("Startup 1   Advantage -4", FrameMeterText.Describe(minus));
    }

    [Fact]
    public void TestmansPointBlank5AIsStartup5AndPlus3OnHitWithHitstopLeftOut()
    {
        // Testman's 5a (data): startup 5, active 3, recovery 8, hitstun 13, hitstop 8. On hit on its first active
        // frame: 10 more frames of 5a against 13 of hitstun, so +3.
        var meter = new FrameMeter();
        MatchLineup lineup = MatchLineup.From(TestGameData.Data);
        var simulation = new FrameMeterSimulation(
            new ComposedMatchSimulation(lineup.Simulation), lineup.Characters, meter);
        MatchState state = simulation.CreateInitialState();
        state.Fighters[0].Position = new IntVector2(-30_000, 0);
        state.Fighters[1].Position = new IntVector2(30_000, 0);

        var buttonA = new InputWord(1 << InputWord.FirstButtonBit);
        simulation.Tick(ref state, buttonA, InputWord.Neutral);
        for (int frame = 0; frame < 60; frame++)
        {
            simulation.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        }

        Assert.Equal(5, meter.Startup);
        Assert.Equal(3, meter.Advantage);
    }

    private static void Record(FrameMeter meter, FighterFrameState player, FighterFrameState opponent, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            meter.Record(player, opponent);
        }
    }
}
