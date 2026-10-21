using System;
using FightingGame.Online;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class HitReactionClockTests
{
    [Fact]
    public void HoldsTheFirstFrameWhileHitstopCountsDown()
    {
        // A hit with 13 hitstun and 8 hitstop on match frame 100: the fighter is held through frame 108.
        var clock = new HitReactionClock();
        for (int frame = 100; frame <= 108; frame++)
        {
            Assert.Equal(1, Show(clock, Reaction("hitstun", frame, stun: 13, hitstop: 108 - frame)));
        }

        Assert.Equal(2, Show(clock, Reaction("hitstun", 109, stun: 12)));
        Assert.Equal(5, Show(clock, Reaction("hitstun", 112, stun: 9)));
    }

    [Fact]
    public void StartsOverOnEveryHitOfAChainThoughTheMoveStaysTheSame()
    {
        var clock = new HitReactionClock();
        Show(clock, Reaction("hitstun", 100, stun: 13, hitstop: 8));
        Assert.Equal(8, Show(clock, Reaction("hitstun", 115, stun: 6)));

        Assert.Equal(1, Show(clock, Reaction("hitstun", 116, stun: 18, hitstop: 10)));
        Assert.Equal(1, Show(clock, Reaction("hitstun", 126, stun: 18)));
        Assert.Equal(2, Show(clock, Reaction("hitstun", 127, stun: 17)));
    }

    [Fact]
    public void StartsOverOnAHitThatLeavesLessStunThanBefore()
    {
        // A heavy hit's long stun, then a quick hit with less: its hitstop shows it.
        var clock = new HitReactionClock();
        Show(clock, Reaction("hitstun", 100, stun: 24));
        Show(clock, Reaction("hitstun", 103, stun: 21));

        Assert.Equal(1, Show(clock, Reaction("hitstun", 104, stun: 10, hitstop: 8)));
    }

    [Fact]
    public void StartsOverOnABlockAfterABlock()
    {
        var clock = new HitReactionClock();
        Show(clock, Reaction("blockstun", 100, stun: 11, hitstop: 8, FighterAction.Blockstun));
        Show(clock, Reaction("blockstun", 113, stun: 7, action: FighterAction.Blockstun));

        Assert.Equal(1, Show(clock, Reaction("blockstun", 114, stun: 14, hitstop: 10, FighterAction.Blockstun)));
    }

    [Fact]
    public void HoldsWhileAFreezeHoldsTheFighter()
    {
        // In a freeze the fighter's stun doesn't count down.
        var clock = new HitReactionClock();
        Show(clock, Reaction("hitstun", 100, stun: 13));
        Assert.Equal(5, Show(clock, Reaction("hitstun", 104, stun: 9)));

        Assert.Equal(5, Show(clock, Reaction("hitstun", 110, stun: 9)));
    }

    [Fact]
    public void CountsAnAirHitstunByTheMatchClockFromTheEndOfItsHitstop()
    {
        // Without air recovery, air hitstun's stun doesn't count down until landing.
        var clock = new HitReactionClock();
        for (int frame = 100; frame <= 108; frame++)
        {
            Assert.Equal(1, Show(clock, Air(frame, hitstop: 108 - frame)));
        }

        Assert.Equal(2, Show(clock, Air(109)));
        Assert.Equal(13, Show(clock, Air(120)));
    }

    [Fact]
    public void StartsAJuggleOverOnTheNextHit()
    {
        var clock = new HitReactionClock();
        Show(clock, Air(100, hitstop: 8));
        Show(clock, Air(120));

        Assert.Equal(1, Show(clock, Air(121, hitstop: 8)));
        Assert.Equal(2, Show(clock, Air(130)));
    }

    [Fact]
    public void StartsOverWhenTheMatchGoesBack()
    {
        var clock = new HitReactionClock();
        Show(clock, Reaction("hitstun", 100, stun: 13));
        Show(clock, Reaction("hitstun", 106, stun: 7));

        Assert.Equal(1, Show(clock, Reaction("hitstun", 103, stun: 10)));
    }

    [Fact]
    public void TellsAReHitFromAFirstHit()
    {
        var clock = new HitReactionClock();
        clock.Observe(Idle(99));
        clock.Observe(Reaction("hitstun", 100, stun: 13, hitstop: 8));
        Assert.False(clock.IsReHit, "the first hit snaps in");

        clock.Observe(Reaction("hitstun", 112, stun: 9));
        Assert.False(clock.IsReHit);

        clock.Observe(Reaction("hitstun", 113, stun: 18, hitstop: 10));
        Assert.True(clock.IsReHit, "a gatling's next hit");

        clock.Observe(Reaction("hitstun", 114, stun: 18, hitstop: 9));
        Assert.False(clock.IsReHit, "only on the frame it lands");

        clock.Observe(Air(130, hitstop: 8));
        Assert.True(clock.IsReHit, "a launcher mid-combo");
    }

    [Fact]
    public void IsNoReHitWhenTheMatchGoesBack()
    {
        var clock = new HitReactionClock();
        clock.Observe(Reaction("hitstun", 100, stun: 13));
        clock.Observe(Reaction("hitstun", 106, stun: 7));
        clock.Observe(Reaction("hitstun", 103, stun: 10));

        Assert.False(clock.IsReHit);
    }

    [Fact]
    public void TestmansGatlingStartsTheReactionOverOnEachHitAndHoldsItThroughHitstop()
    {
        // 5A cancelled into 5B point blank. The defender stays in testman's hitstun move, one frame long and looping,
        // so neither its move nor its move frame tells the second hit from the first.
        var match = new PointBlankMatch();
        match.Tick(PointBlankMatch.ButtonA);
        match.TickUntil(() => match.Defender.Action == FighterAction.Hitstun);

        Assert.Equal(1, match.ReactionFrame);
        match.TickThroughHitstop();
        match.Tick(PointBlankMatch.ButtonB);
        Assert.Equal(2, match.ReactionFrame);

        match.TickUntil(() => match.Defender.Timing.HitstopFramesLeft > 0);

        Assert.True(match.PreviousReactionFrame > 2, "the reaction counted on until 5B hit");
        Assert.Equal("hitstun", match.Pose.MoveId);
        Assert.Equal(1, match.ReactionFrame);
        Assert.True(match.IsReHit, "5B lands mid-reaction, so the model fades into it");
        match.TickThroughHitstop();
        match.Tick(InputWord.Neutral);
        Assert.Equal(2, match.ReactionFrame);
    }

    private static int Show(HitReactionClock clock, FighterPose pose)
    {
        clock.Observe(in pose);
        return clock.FrameOf(in pose);
    }

    private static FighterPose Reaction(
        string moveId, int matchFrame, int stun, int hitstop = 0, FighterAction action = FighterAction.Hitstun) =>
        new(action, moveId, 1, 1, MovePhase.None)
        {
            MatchFrame = matchFrame,
            StunFramesLeft = stun,
            HitstopFramesLeft = hitstop,
        };

    private static FighterPose Idle(int matchFrame) =>
        new FighterPose(FighterAction.Idle, "idle", 1, 60, MovePhase.None) { MatchFrame = matchFrame };

    private static FighterPose Air(int matchFrame, int hitstop = 0) =>
        Reaction("air_hitstun", matchFrame, stun: 20, hitstop, FighterAction.AirHitstun);

    // Testman against testman under the game's own rules and modules, P2 standing still; the clock watches P2.
    private sealed class PointBlankMatch
    {
        // Buttons a and b (data/input/buttons.json).
        public static readonly InputWord ButtonA = new(1 << InputWord.FirstButtonBit);
        public static readonly InputWord ButtonB = new(1 << (InputWord.FirstButtonBit + 1));

        private readonly ComposedMatchSimulation _simulation;
        private readonly CharacterData _defender;
        private readonly HitReactionClock _clock = new();
        private MatchState _state;

        public PointBlankMatch()
        {
            MatchLineup lineup = MatchLineup.From(TestGameData.Data);
            _simulation = new ComposedMatchSimulation(lineup.Simulation);
            _defender = lineup.Characters[1];
            _state = _simulation.CreateInitialState();
            _state.Fighters[0].Position = new IntVector2(-30_000, 0);
            _state.Fighters[1].Position = new IntVector2(30_000, 0);
        }

        public FighterState Defender => _state.Fighters[1];

        public FighterPose Pose { get; private set; }

        public int ReactionFrame { get; private set; }

        public bool IsReHit => _clock.IsReHit;

        /// <summary>The reaction frame shown on the frame before.</summary>
        public int PreviousReactionFrame { get; private set; }

        public void Tick(InputWord attacker)
        {
            PreviousReactionFrame = ReactionFrame;
            _simulation.Tick(ref _state, attacker, InputWord.Neutral);
            Pose = FighterPose.Of(_defender, in _state.Fighters[1]) with { MatchFrame = _state.Frame };
            _clock.Observe(Pose);
            ReactionFrame = _clock.FrameOf(Pose);
        }

        public void TickUntil(Func<bool> condition)
        {
            for (int frame = 0; frame < 60 && !condition(); frame++)
            {
                Tick(InputWord.Neutral);
            }

            Assert.True(condition());
        }

        // Every frame hitstop holds the defender shows the reaction's first frame.
        public void TickThroughHitstop()
        {
            Assert.True(Defender.Timing.HitstopFramesLeft > 0, "a hit just landed");
            while (Defender.Timing.HitstopFramesLeft > 0)
            {
                Tick(InputWord.Neutral);
                Assert.Equal(1, ReactionFrame);
            }
        }
    }
}
