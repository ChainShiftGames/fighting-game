using System;
using System.Diagnostics;
using System.IO;
using Netcode.Online;

namespace Soak;

/// <summary>Plays soak matches back to back until the time is up; the first failure ends the run.</summary>
internal sealed class SoakRun
{
    private static readonly TimeSpan _progressInterval = TimeSpan.FromMinutes(1);

    private readonly SoakGame _game;
    private readonly TextWriter _log;
    private readonly uint _seed;
    private readonly SoakOptions _options;
    private readonly Stopwatch _clock = new();
    private TimeSpan _nextProgress;
    private long _comparedFrames;

    public SoakRun(SoakGame game, uint seed, SoakOptions options, TextWriter log)
    {
        _game = game;
        _seed = seed;
        _options = options;
        _log = log;
    }

    /// <summary>Matches played, including the one cut short when the time ran out.</summary>
    public int Matches { get; private set; }

    /// <summary>Confirmed frames compared between the peers over all matches.</summary>
    public long ComparedFrames => _comparedFrames;

    /// <summary>Runs the soak.</summary>
    /// <exception cref="Netcode.Desync.DesyncDetectedException">The peers disagreed.</exception>
    /// <exception cref="TimeoutException">A match stopped advancing.</exception>
    public void Run()
    {
        _clock.Start();
        _nextProgress = _progressInterval;
        while (_clock.Elapsed < _options.Duration)
        {
            Matches++;
            PlayMatch(Matches);
        }
    }

    private void PlayMatch(int number)
    {
        uint player1Seed = MatchSeeds.Player1(_seed, number);
        uint player2Seed = MatchSeeds.Player2(_seed, number);
        string reproduction = $"Soak seed {_seed}, match {number} (P1 inputs seed {player1Seed}, P2 {player2Seed}); " +
            _game.Describe();
        Log($"Match {number}: P1 seed {player1Seed}, P2 seed {player2Seed}");

        using var match = new SoakMatch(_game, player1Seed, player2Seed, _options.DumpDirectory, reproduction);
        try
        {
            while (!match.IsOver && _clock.Elapsed < _options.Duration)
            {
                match.Step();
                ReportProgressWhenDue(match, number);
            }
        }
        catch (Exception)
        {
            // Whether the peers still had a connection tells a desync of the logic from one a dropped link caused.
            Log($"Match {number} failed at {Status(match)}");
            throw;
        }
        finally
        {
            _comparedFrames += match.ComparedThroughFrame;
        }

        Log($"Match {number}: {(match.IsOver ? "match over" : "time up")} at {Status(match)}");
    }

    private void ReportProgressWhenDue(SoakMatch match, int number)
    {
        if (_clock.Elapsed < _nextProgress)
        {
            return;
        }

        _nextProgress += _progressInterval;
        Log($"Match {number}: {Status(match)}");
    }

    private void Log(string line) => _log.WriteLine($"[{_clock.Elapsed:hh\\:mm\\:ss}] {line}");

    private static string Status(SoakMatch match) =>
        $"frames {match.Player1Frame} / {match.Player2Frame}, matched through {match.ComparedThroughFrame}; " +
        $"P1 {Connection(match.Player1Connection)}; P2 {Connection(match.Player2Connection)}";

    private static string Connection(IConnectionReadout readout) =>
        readout.TryReadConnection(out ConnectionStats stats)
            ? $"ping {stats.PingMilliseconds} ms, {stats.Rollbacks} rollbacks (longest {stats.LongestRollbackFrames})"
            : "no connection stats";
}
