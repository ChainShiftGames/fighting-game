using System;
using Netcode.Desync;

namespace Soak;

/// <summary>
/// Bot soak test (ADR-0006, P2-12): plays online matches between two in-process peers for a set time and fails on
/// the first desync or stall, printing the seed that replays the run's inputs.
/// </summary>
/// <remarks>
/// <c>dotnet run --project tests/Soak -c Release -- --minutes 2</c> for a short local run. Lag, jitter and loss are
/// random per packet and the timing is real, so a seed replays the inputs, not the exact network.
/// </remarks>
internal static class Program
{
    private const int Passed = 0;
    private const int Failed = 1;
    private const int InvalidArguments = 2;

    public static int Main(string[] args)
    {
        if (!SoakOptions.TryParse(args, out SoakOptions options, out string error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(SoakOptions.Usage);
            return InvalidArguments;
        }

        uint seed = options.Seed ?? (uint)Random.Shared.NextInt64(uint.MaxValue);
        var game = new SoakGame();
        string replay = $"Replay the inputs with: dotnet run --project tests/Soak -c Release -- --seed {seed}";
        Console.WriteLine($"Soak test: {options.Duration.TotalMinutes} min, seed {seed}");
        Console.WriteLine($"Conditions: {game.Describe()}");

        var run = new SoakRun(game, seed, options, Console.Out);
        try
        {
            run.Run();
        }
        catch (DesyncDetectedException desync)
        {
            Console.Error.WriteLine($"FAIL: desync in match {run.Matches}. {desync}");
            Console.Error.WriteLine(replay);
            return Failed;
        }
        catch (TimeoutException stall)
        {
            Console.Error.WriteLine($"FAIL: match {run.Matches} stalled. {stall.Message}");
            Console.Error.WriteLine(replay);
            return Failed;
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"FAIL: crashed in match {run.Matches}.");
            Console.Error.WriteLine(replay);
            throw;
        }

        Console.WriteLine(
            $"PASS: {run.Matches} matches, {run.ComparedFrames} confirmed frames matched on both peers, no desync. " +
            $"Seed {seed}.");
        return Passed;
    }
}
