using System;
using System.Collections.Generic;
using Benchmarks.Drivers;
using Benchmarks.Operations;
using Sim.State;
using Sim.Toy;

namespace Benchmarks;

/// <summary>Measures the game-logic operations of ADR-0003 and exits non-zero when a budget gate fails.</summary>
internal static class Program
{
    private const int Passed = 0;
    private const int BudgetExceeded = 1;
    private const int InvalidArguments = 2;

    private const string ToyStateName = "toy";
    private const string MatchStateName = "MatchState";

    public static int Main(string[] args)
    {
        if (!BenchmarkOptions.TryParse(args, out BenchmarkOptions options, out string error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(BenchmarkOptions.Usage);
            return InvalidArguments;
        }

        var simulation = new ToySimulation(ToyRules.Default);
        var inputs = new InputSequence(options.InputSeed);
        var timer = new OperationTimer(options);

        var tick = new TickOperation(simulation, inputs);
        var resimulation = new ResimulationOperation(simulation, inputs, options.RollbackFrames);
        ToyState toyState = simulation.CreateInitialState();
        var snapshotSave = new SnapshotSaveOperation<ToyState>(ToyStateName, toyState, options.RollbackFrames);
        var snapshotLoad = new SnapshotLoadOperation<ToyState>(ToyStateName, toyState, options.RollbackFrames);
        var checksum = new ChecksumOperation<ToyState>(ToyStateName, toyState);

        // The real match state has no tick yet (P1-03), but its size already sets the snapshot and checksum cost.
        MatchState matchState = default;
        var matchSave = new SnapshotSaveOperation<MatchState>(MatchStateName, matchState, options.RollbackFrames);
        var matchLoad = new SnapshotLoadOperation<MatchState>(MatchStateName, matchState, options.RollbackFrames);
        var matchChecksum = new ChecksumOperation<MatchState>(MatchStateName, matchState);

        OperationResult tickResult = timer.Measure(ref tick);
        List<OperationResult> results =
        [
            tickResult,
            timer.Measure(ref resimulation),
            timer.Measure(ref snapshotSave),
            timer.Measure(ref snapshotLoad),
            timer.Measure(ref checksum),
            timer.Measure(ref matchSave),
            timer.Measure(ref matchLoad),
            timer.Measure(ref matchChecksum),
        ];

        ResultTable.Write(Console.Out, results);
        if (options.IncludeBackdash)
        {
            ResultTable.WriteBackdash(Console.Out, MeasureBackdash(simulation, inputs, options));
        }

        List<string> violations = BudgetCheck.FindViolations(results, tickResult, options.MaxTickMicroseconds);
        foreach (string violation in violations)
        {
            Console.Error.WriteLine($"FAIL: {violation}");
        }

        return violations.Count == 0 ? Passed : BudgetExceeded;
    }

    // Not gated: Backdash's own time and allocations aren't ours to fix (ADR-0013), so they are only recorded.
    private static List<FrameResult> MeasureBackdash(
        ToySimulation simulation, InputSequence inputs, BenchmarkOptions options)
    {
        var timer = new FrameTimer(options);
        return [Measure(timer, new SyncTestFrameBenchmark(simulation, inputs, options.RollbackFrames)),
            Measure(timer, new PeerFrameBenchmark(simulation, inputs, options.RollbackFrames))];
    }

    // One session at a time: allocations are counted for the whole process, so another session's network threads
    // must not be running.
    private static FrameResult Measure(FrameTimer timer, IDriverBenchmark benchmark)
    {
        using (benchmark)
        {
            return timer.Measure(benchmark);
        }
    }
}
