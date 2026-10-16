using System.Threading;

namespace Netcode.Transport;

/// <summary>Packets thrown away on arrival, before the netcode read them (developer readings).</summary>
/// <remarks>Counted on the network thread and read from the game thread.</remarks>
public sealed class DroppedPackets
{
    private int _malformed;
    private int _oversized;

    /// <summary>Packets that weren't exactly one well-formed netcode message.</summary>
    public int Malformed => Volatile.Read(ref _malformed);

    /// <summary>Packets bigger than <see cref="ITransport.MaxPacketSize"/>.</summary>
    public int Oversized => Volatile.Read(ref _oversized);

    internal void CountMalformed() => Interlocked.Increment(ref _malformed);

    internal void CountOversized() => Interlocked.Increment(ref _oversized);
}
