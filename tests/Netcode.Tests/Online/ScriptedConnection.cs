using Netcode.Online;

namespace Netcode.Tests.Online;

/// <summary>A connection whose ping and status the test sets; a null ping reads as no connection yet.</summary>
internal sealed class ScriptedConnection : IConnectionReadout, IConnectionStatusReadout
{
    public int? PingMilliseconds { get; set; }

    public ConnectionStatus Status { get; set; } = ConnectionStatus.Connected;

    public int Reads { get; private set; }

    public bool TryReadConnection(out ConnectionStats stats)
    {
        Reads++;
        stats = new ConnectionStats(0, PingMilliseconds ?? 0, 0, 0, 0, 0, 0, 0);
        return PingMilliseconds is not null;
    }

    public ConnectionStatus ReadConnectionStatus() => Status;
}
