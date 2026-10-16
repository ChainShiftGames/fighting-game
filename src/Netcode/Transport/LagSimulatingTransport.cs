using System;
using System.Threading;
using System.Threading.Tasks;

namespace Netcode.Transport;

/// <summary>
/// Wraps a real transport (UDP or Steam) and makes the network worse on purpose: drops, delays and reorders the
/// packets it sends, as set by <see cref="NetworkSimulation"/>. Development only.
/// </summary>
/// <remarks>Allocates per delayed packet; fine for a dev tool, and it never runs inside a logic frame.</remarks>
public sealed class LagSimulatingTransport : ITransport
{
    private const int Percent = 100;

    private readonly ITransport _transport;
    private readonly NetworkSimulation _simulation;
    private readonly CancellationTokenSource _closing = new();

    public LagSimulatingTransport(ITransport transport, NetworkSimulation simulation)
    {
        _transport = transport;
        _simulation = simulation;
    }

    // A dropped or still-waiting packet looks sent: a real network gives no signal either.
    public void Send(ReadOnlySpan<byte> packet)
    {
        if (Random.Shared.Next(Percent) < _simulation.PacketLossPercent)
        {
            return;
        }

        // The caller reuses its buffer as soon as this returns, so a delayed packet needs its own copy.
        _ = SendLater(packet.ToArray(), NextDelay());
    }

    public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        _transport.ReceiveAsync(buffer, cancellationToken);

    public void Poll() => _transport.Poll();

    public void Dispose()
    {
        _closing.Cancel();
        _transport.Dispose();
    }

    private TimeSpan NextDelay()
    {
        int jitter = _simulation.JitterMilliseconds;
        int roundTrip = _simulation.PingMilliseconds + Random.Shared.Next(-jitter, jitter + 1);
        return TimeSpan.FromMilliseconds(Math.Max(roundTrip, 0) / 2.0);
    }

    private async Task SendLater(byte[] packet, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _closing.Token).ConfigureAwait(false);
            _transport.Send(packet);
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            // The transport closed while the packet was in flight: it's lost, as on a real network.
        }
    }
}
