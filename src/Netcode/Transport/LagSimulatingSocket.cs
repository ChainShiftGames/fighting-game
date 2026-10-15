using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Backdash.Network.Client;

namespace Netcode.Transport;

/// <summary>
/// Wraps a real socket and makes the network worse on purpose: drops, delays and reorders the packets it sends,
/// as set by <see cref="NetworkSimulation"/>. Development only.
/// </summary>
/// <remarks>Allocates per delayed packet; fine for a dev tool, and it never runs inside a logic frame.</remarks>
public sealed class LagSimulatingSocket : IPeerSocket
{
    private const int Percent = 100;

    private readonly IPeerSocket _socket;
    private readonly NetworkSimulation _simulation;

    public LagSimulatingSocket(IPeerSocket socket, NetworkSimulation simulation)
    {
        _socket = socket;
        _simulation = simulation;
    }

    public AddressFamily AddressFamily => _socket.AddressFamily;

    public int Port => _socket.Port;

    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken) =>
        _socket.ReceiveFromAsync(buffer, address, cancellationToken);

    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        _socket.ReceiveAsync(buffer, cancellationToken);

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken)
    {
        // The caller reuses both buffers as soon as this returns, so a delayed send needs its own copies.
        var address = new SocketAddress(socketAddress.Family, socketAddress.Size);
        socketAddress.Buffer.CopyTo(address.Buffer);
        byte[] packet = buffer.ToArray();
        return Send(buffer.Length, token => _socket.SendToAsync(packet, address, token), cancellationToken);
    }

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        byte[] packet = buffer.ToArray();
        return Send(buffer.Length, token => _socket.SendToAsync(packet, remoteEndPoint, token), cancellationToken);
    }

    public void Update() => _socket.Update();

    public void Close() => _socket.Close();

    public void Dispose() => _socket.Dispose();

    // Reports the packet as sent even when it's dropped or still waiting: a real network gives no signal either.
    private ValueTask<int> Send(int length, Func<CancellationToken, ValueTask<int>> send, CancellationToken cancellationToken)
    {
        if (Random.Shared.Next(Percent) < _simulation.PacketLossPercent)
        {
            return ValueTask.FromResult(length);
        }

        _ = SendLater(NextDelay(), send, cancellationToken);
        return ValueTask.FromResult(length);
    }

    private TimeSpan NextDelay()
    {
        int jitter = _simulation.JitterMilliseconds;
        int roundTrip = _simulation.PingMilliseconds + Random.Shared.Next(-jitter, jitter + 1);
        return TimeSpan.FromMilliseconds(Math.Max(roundTrip, 0) / 2.0);
    }

    private static async Task SendLater(TimeSpan delay, Func<CancellationToken, ValueTask<int>> send, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await send(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // The session closed while the packet was in flight: it's lost, as on a real network.
        }
    }
}
