using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Input;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;
using Sim.Tests.Modules;
using Xunit;

namespace Sim.Tests.Pipeline;

/// <summary>Meter read and spent through the core (ADR-0011, P3-13), and the move lock request.</summary>
public sealed class MeterAccessTests
{
    private const int Player1 = 0;
    private const int Player2 = 1;

    [Fact]
    public void AnyModuleReadsAndSpendsTheGaugesMeterThroughTheCore()
    {
        var results = new List<(bool Spent, int Left)>();
        var spender = Spender(Player1, [30, 30, 50], results);

        Run(frames: 3, new SpentMeterGauge(1), spender);

        Assert.Equal([(true, 70), (true, 40), (false, 40)], results);
    }

    [Fact]
    public void SpendingIsPerFighter()
    {
        var results = new List<(bool Spent, int Left)>();
        var spender = Spender(Player2, [60], results);
        int player1Meter = -1;
        var reader = new ScriptedModule(3) { OnEndOfFrame = context => player1Meter = context.MeterOf(Player1) };

        Run(frames: 1, new SpentMeterGauge(1), spender, reader);

        Assert.Equal([(true, 40)], results);
        Assert.Equal(SpentMeterGauge.StartMeter, player1Meter);
    }

    [Fact]
    public void WithoutAGaugeEveryFighterHasNoMeterAndSpendingFails()
    {
        var results = new List<(bool Spent, int Left)>();

        Run(frames: 1, Spender(Player1, [0], results));

        Assert.Equal([(false, 0)], results);
    }

    [Fact]
    public void SpendingANegativeAmountFailsLoudly()
    {
        var spender = new ScriptedModule(2) { OnAfterInput = context => context.TrySpendMeter(Player1, -1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => Run(frames: 1, new SpentMeterGauge(1), spender));
    }

    [Fact]
    public void MoveLocksFromSeveralModulesCombine()
    {
        var supers = new ScriptedModule(1)
        {
            OnAfterInput = context =>
                context.RequestMoveLock(Player2, new MoveLockRequest { Categories = MoveCategory.Super }),
        };
        var throws = new ScriptedModule(2)
        {
            OnAfterInput = context =>
                context.RequestMoveLock(Player2, new MoveLockRequest { Categories = MoveCategory.Throw }),
        };

        MatchState state = Run(frames: 1, supers, throws);

        Assert.Equal(MoveCategory.Super | MoveCategory.Throw, state.Fighters[Player2].LockedMoveCategories);
        Assert.Equal(MoveCategory.None, state.Fighters[Player1].LockedMoveCategories);
    }

    // Spends one amount per frame and records whether it worked and the meter left after.
    private static ScriptedModule Spender(int fighter, int[] amounts, List<(bool Spent, int Left)> results) =>
        new(2)
        {
            OnAfterInput = context =>
            {
                bool spent = context.TrySpendMeter(fighter, amounts[context.State.Frame]);
                results.Add((spent, context.MeterOf(fighter)));
            },
        };

    // No state machine stage, so a move lock stays visible after the frame.
    private static MatchState Run(int frames, params IMechanicModule[] modules)
    {
        TickPipeline pipeline = Pipelines.Create(modules);
        MatchState state = default;
        for (int frame = 0; frame < frames; frame++)
        {
            pipeline.Tick(ref state, InputWord.Neutral, InputWord.Neutral);
        }

        return state;
    }
}
