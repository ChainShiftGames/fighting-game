using System;
using System.Threading;
using System.Threading.Tasks;

namespace Netcode.Transport;

/// <summary>Carries an online match's packets between this PC and the other one (direct UDP, Steam, …).</summary>
/// <remarks>
/// Unreliable like UDP: a packet may be lost, delayed or reordered; the netcode resends what matters. Transports
/// never see netcode types, so a transport can be added or replaced without touching Backdash (ADR-0005 §2).
/// </remarks>
public interface ITransport : IDisposable
{
    /// <summary>
    /// Largest packet a transport has to carry, so it fits in one unfragmented datagram on any route. Anything
    /// bigger that arrives is dropped as oversized.
    /// </summary>
    const int MaxPacketSize = 1200;

    /// <summary>Sends one packet of at most <see cref="MaxPacketSize"/> bytes to the other PC.</summary>
    /// <remarks>
    /// May be called from several threads. A packet the network refuses is lost, not an error: only a closed
    /// transport throws.
    /// </remarks>
    void Send(ReadOnlySpan<byte> packet);

    /// <summary>Waits for the next packet from the other PC and copies it into <paramref name="buffer"/>.</summary>
    /// <returns>
    /// The packet's full length. When that is more than <paramref name="buffer"/> holds, only the start was copied,
    /// so the caller can tell an oversized packet from a full buffer.
    /// </returns>
    /// <remarks>Packets from anyone but the other PC never arrive here.</remarks>
    ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    /// Called on the game thread once per frame, before the netcode reads what arrived. Transports that collect
    /// packets by polling (Steam) do it here; the others do nothing.
    /// </summary>
    void Poll();
}
