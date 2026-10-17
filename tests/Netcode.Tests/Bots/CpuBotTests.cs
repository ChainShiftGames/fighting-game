using System;
using System.Collections.Generic;
using Netcode.Bots;
using Netcode.Drivers;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Bots;

/// <summary>CPU bots in real Testman matches: they fight, stay deterministic and react as their settings say.</summary>
public sealed class CpuBotTests
{
    private const int Frames = 10_000;
    // A pair whose 10,000 frames show every action and intent the fight test checks; re-picked when core pushback
    // (hits push the defender away) changed how matches go.
    private const uint Player1Seed = 51;
    private const uint Player2Seed = 3050;

    private static readonly BotSettings _defaults = new();

    [Fact]
    public void TheSameSeedPlaysTheSameMatch()
    {
        using var first = new BotMatch(_defaults, Player1Seed, player2Seed: Player2Seed);
        using var second = new BotMatch(_defaults, Player1Seed, player2Seed: Player2Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            first.Advance();
            second.Advance();
        }

        Assert.Equal(first.Player1Inputs, second.Player1Inputs);
        Assert.Equal(first.Player2Inputs, second.Player2Inputs);
        Assert.Equal(StateChecksum.Compute(in first.State), StateChecksum.Compute(in second.State));
    }

    [Fact]
    public void ADifferentSeedPlaysDifferently()
    {
        using var first = new BotMatch(_defaults, Player1Seed, player2Seed: Player2Seed);
        using var second = new BotMatch(_defaults, Player1Seed + 1, player2Seed: Player2Seed);

        for (int frame = 0; frame < Frames; frame++)
        {
            first.Advance();
            second.Advance();
        }

        Assert.NotEqual(first.Player1Inputs, second.Player1Inputs);
    }

    [Fact]
    public void TwoBotsFightAndStayInSyncUnderRollback()
    {
        using var match = new BotMatch(_defaults, Player1Seed, player2Seed: Player2Seed, syncTest: true);
        var seen = new HashSet<FighterAction>();
        var intents = new HashSet<BotIntent>();

        for (int frame = 0; frame < Frames; frame++)
        {
            match.Advance();
            seen.Add(match.State.Fighters[0].Action);
            seen.Add(match.State.Fighters[1].Action);
            intents.Add(match.Player1.Intent);
        }

        Assert.True(
            match.State.Round.Round > 1 || match.State.Round.Phase != RoundPhase.Fighting,
            "No round ended in 10,000 frames; the bots don't fight.");
        Assert.Contains(FighterAction.Attack, seen);
        Assert.Contains(FighterAction.Hitstun, seen);
        Assert.Contains(FighterAction.Blockstun, seen);
        Assert.Contains(FighterAction.Jump, seen);
        Assert.Equal(
            [BotIntent.Wait, BotIntent.Approach, BotIntent.Attack, BotIntent.Block, BotIntent.Punish],
            Sorted(intents));
    }

    [Fact]
    public void ABotWalksUpToAnOpponentThatStandsStill()
    {
        using var match = new BotMatch(_defaults, Player1Seed, player2: new Neutral());
        int startDistance = Distance(in match.State);

        for (int frame = 0; frame < 600; frame++)
        {
            match.Advance();
        }

        Assert.True(Distance(in match.State) < startDistance, "The bot didn't come closer.");
        Assert.True(match.State.Fighters[1].Health < BotMatch.Character.Stats.Health, "The bot never hit.");
    }

    [Fact]
    public void ABotBlocksAttacksAsOftenAsItsBlockChanceSays()
    {
        var attacker = new BotSettings { BlockChancePercent = 0, AttackChancePercent = 100 };

        var alwaysBlocks = new BotSettings { BlockChancePercent = 100, AttackChancePercent = 0 };
        var neverBlocks = new BotSettings { BlockChancePercent = 0, AttackChancePercent = 0 };

        Assert.True(BlockstunFrames(alwaysBlocks, attacker) > 0);
        Assert.Equal(0, BlockstunFrames(neverBlocks, attacker));
    }

    [Fact]
    public void ALongerReactionDelayActsLater()
    {
        int fast = FirstPressFrame(reactionDelay: 0);
        int slow = FirstPressFrame(reactionDelay: BotSettings.MaxReactionDelayFrames);

        Assert.True(fast < slow, $"Reacting in 0 frames pressed on frame {fast}, in {BotSettings.MaxReactionDelayFrames} frames on frame {slow}.");
    }

    [Fact]
    public void SettingsOutsideTheirRangeAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBot(new BotSettings { ReactionDelayFrames = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateBot(new BotSettings { ReactionDelayFrames = BotSettings.MaxReactionDelayFrames + 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBot(new BotSettings { BlockChancePercent = 101 }));
    }

    private static int BlockstunFrames(BotSettings defender, BotSettings attacker)
    {
        using var match = new BotMatch(defender, Player1Seed, attacker, Player2Seed);
        int frames = 0;
        for (int frame = 0; frame < Frames; frame++)
        {
            match.Advance();
            frames += match.State.Fighters[0].Action == FighterAction.Blockstun ? 1 : 0;
        }

        return frames;
    }

    // The first frame the bot presses a button against a still opponent: it approaches, sees it is in reach after its
    // reaction time, then attacks.
    private static int FirstPressFrame(int reactionDelay)
    {
        var settings = new BotSettings
        {
            ReactionDelayFrames = reactionDelay,
            AttackChancePercent = 100,
            ProjectileChancePercent = 0,
        };
        using var match = new BotMatch(settings, Player1Seed, player2: new Neutral());
        for (int frame = 0; frame < Frames; frame++)
        {
            match.Advance();
            if ((match.Player1Inputs[frame].Bits & ~InputWord.DirectionMask) != 0)
            {
                return frame;
            }
        }

        throw new InvalidOperationException("The bot never pressed a button.");
    }

    private static CpuBot CreateBot(BotSettings settings) =>
        new(
            new FixedObservation(),
            MoveRepertoire.Build(BotMatch.Character, BotMatch.Character),
            default,
            settings,
            Player1Seed);

    private static int Distance(in MatchState state) =>
        Math.Abs(state.Fighters[1].Position.X - state.Fighters[0].Position.X);

    private static List<BotIntent> Sorted(HashSet<BotIntent> intents)
    {
        var sorted = new List<BotIntent>(intents);
        sorted.Sort();
        return sorted;
    }

    private sealed class Neutral : IInputSource
    {
        public InputWord Next() => InputWord.Neutral;
    }

    private sealed class FixedObservation : IObservationSource
    {
        public BotObservation Observe() => default;
    }
}
