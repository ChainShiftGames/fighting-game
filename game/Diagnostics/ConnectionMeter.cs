using System.Globalization;
using Netcode.Online;

namespace FightingGame.Diagnostics;

/// <summary>
/// Samples an online match's connection once a second (P0-22): Backdash's ping and frame advantage, the transport's
/// own ping (Steam's relay) and the rollbacks, keeping each ping's range since the match started.
/// </summary>
public sealed class ConnectionMeter
{
    private const int FramesPerMinute = 60 * 60;

    private readonly IConnectionReadout _readout;
    private readonly ITransportLink? _link;
    private readonly long _ticksPerSample;
    private readonly SampleRange _ping = new();
    private readonly SampleRange _linkPing = new();
    private long _nextSampleAt;
    private ConnectionStats? _connection;
    private TransportLinkStats? _linkStats;

    /// <param name="readout">The online match driver's connection readings.</param>
    /// <param name="link">The transport's own readings; null when it has none (direct UDP).</param>
    /// <param name="ticksPerSecond">Resolution of the timestamps passed to <see cref="Sample"/>.</param>
    public ConnectionMeter(IConnectionReadout readout, ITransportLink? link, long ticksPerSecond)
    {
        _readout = readout;
        _link = link;
        _ticksPerSample = ticksPerSecond;
    }

    /// <summary>Samples taken so far; one per second of running match.</summary>
    public int SampleCount => _ping.Count;

    /// <summary>The latest sampled ping; null until the match runs (for the player's ping indicator).</summary>
    public int? PingMilliseconds => _connection?.PingMilliseconds;

    /// <summary>Takes a sample once a second while the match runs. Returns true when it took one.</summary>
    public bool Sample(long timestamp)
    {
        if (timestamp < _nextSampleAt)
        {
            return false;
        }

        // Before the first frame the peers are still syncing, and their pings aren't the match's.
        if (!_readout.TryReadConnection(out ConnectionStats connection) || connection.Frame == 0)
        {
            return false;
        }

        _nextSampleAt = timestamp + _ticksPerSample;
        _connection = connection;
        _ping.Add(connection.PingMilliseconds);

        if (_link is not null && _link.TryRead(out TransportLinkStats link))
        {
            _linkStats = link;
            _linkPing.Add(link.PingMilliseconds);
        }

        return true;
    }

    /// <summary>Latest readings and ranges, one topic per line joined by <paramref name="separator"/>.</summary>
    public string Describe(string separator) =>
        string.Join(separator, DescribeBackdash(), DescribeTransport(), DescribeRollbacks());

    private string DescribeBackdash() => _connection is { } connection
        ? string.Create(
            CultureInfo.InvariantCulture,
            $"ping {connection.PingMilliseconds} ms ({Range(_ping)}) | behind {connection.LocalFramesBehind} f, " +
            $"other behind {connection.RemoteFramesBehind} f | unacked inputs {connection.PendingInputs}")
        : "ping: no reading yet";

    private string DescribeTransport()
    {
        if (_link is null)
        {
            return "transport: no own readings";
        }

        if (_linkStats is not { } link)
        {
            return "transport: not connected yet";
        }

        string route = link.IsRelayed ? "relayed" : "direct";
        string delivered = link.DeliveredPercent < 0 ? "?" : link.DeliveredPercent.ToString(CultureInfo.InvariantCulture);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"transport ping {link.PingMilliseconds} ms ({Range(_linkPing)}), {route}, {delivered} % delivered");
    }

    private string DescribeRollbacks()
    {
        if (_connection is not { } connection)
        {
            return "rollbacks: no reading yet";
        }

        double perMinute = (double)connection.Rollbacks * FramesPerMinute / connection.Frame;
        double averageFrames = connection.Rollbacks == 0 ? 0 : (double)connection.RolledBackFrames / connection.Rollbacks;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"rollbacks {connection.Rollbacks} ({perMinute:F0}/min), avg {averageFrames:F1} f, " +
            $"longest {connection.LongestRollbackFrames} f");
    }

    private static string Range(SampleRange range) =>
        string.Create(
            CultureInfo.InvariantCulture, $"min {range.Min}, avg {range.Average:F0}, max {range.Max} over {range.Count} s");
}
