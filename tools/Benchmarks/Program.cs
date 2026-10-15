using System;
using System.Collections.Generic;
using Benchmarks.Operations;
using Sim.Toy;

namespace Benchmarks;

/// <summary>Measures the game-logic operations of ADR-0003 and exits non-zero when a budget gate fails.</summary>
internal static class Program
{
    private const int Passed = 0;
    private const int BudgetExceeded = 1;
    private const int InvalidArguments = 2;

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
        var snapshotSave = new SnapshotSaveOperation(simulation, options.RollbackFrames);
        var snapshotLoad = new SnapshotLoadOperation(simulation, options.RollbackFrames);
        var checksum = new ChecksumOperation(simulation);

        OperationResult tickResult = timer.Measure(ref tick);
        List<OperationResult> results =
        [
            tickResult,
            timer.Measure(ref resimulation),
            timer.Measure(ref snapshotSave),
            timer.Measure(ref snapshotLoad),
            timer.Measure(ref checksum),
        ];

        ResultTable.Write(Console.Out, results);

        List<string> violations = BudgetCheck.FindViolations(results, tickResult, options.MaxTickMicroseconds);
        foreach (string violation in violations)
        {
            Console.Error.WriteLine($"FAIL: {violation}");
        }

        return violations.Count == 0 ? Passed : BudgetExceeded;
    }
}
