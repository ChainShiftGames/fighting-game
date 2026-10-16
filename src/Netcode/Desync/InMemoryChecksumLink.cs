using System.Collections.Generic;

namespace Netcode.Desync;

/// <summary>One end of a checksum link between two peers in the same process (tests, soak runs, bot matches).</summary>
/// <remarks>Not thread-safe: both peers must run on one thread, as the in-process peers do.</remarks>
public sealed class InMemoryChecksumLink : IChecksumLink
{
    private readonly Queue<FrameChecksum> _inbox = new();
    private InMemoryChecksumLink? _other;

    public static (InMemoryChecksumLink Player1, InMemoryChecksumLink Player2) CreatePair()
    {
        var player1 = new InMemoryChecksumLink();
        var player2 = new InMemoryChecksumLink { _other = player1 };
        player1._other = player2;
        return (player1, player2);
    }

    public void Send(FrameChecksum checksum) => _other?._inbox.Enqueue(checksum);

    public bool TryReceive(out FrameChecksum checksum) => _inbox.TryDequeue(out checksum);
}
