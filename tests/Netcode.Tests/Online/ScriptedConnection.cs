using Netcode.Online;

namespace Netcode.Tests.Online;

/// <summary>A connection whose ping the test sets; null reads as no connection yet.</summary>
internal sealed class ScriptedConnection : IConnectionReadout
{
    public int? PingMilliseconds { get; set; }

    public int Reads { get; private set; }

    public bool TryReadConnection(out ConnectionStats stats)
    {
        Reads++;
        stats = new ConnectionStats(0, PingMilliseconds ?? 0, 0, 0, 0, 0, 0, 0);
        return PingMilliseconds is not null;
    }
}
