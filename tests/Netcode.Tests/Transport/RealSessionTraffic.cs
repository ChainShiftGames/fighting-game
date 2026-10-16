using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Netcode.Tests.Transport;

/// <summary>Every packet both peers of a real in-memory session sent, run long enough for each kind of message.</summary>
/// <remarks>Shared by the tests of one class (xUnit class fixture), as the session takes a couple of seconds.</remarks>
public sealed class RealSessionTraffic
{
    public const ushort SyncRequest = 1;
    public const ushort SyncReply = 2;
    public const ushort Input = 3;
    public const ushort QualityReport = 4;
    public const ushort QualityReply = 5;
    public const ushort ConsistencyCheckRequest = 8;
    public const ushort ConsistencyCheckReply = 9;

    private const int FightingFrames = 300;

    // Quality reports and consistency checks go out about once a second, so the run waits for them.
    private static readonly ushort[] _expectedTypes =
        [SyncRequest, SyncReply, Input, QualityReport, QualityReply, ConsistencyCheckRequest, ConsistencyCheckReply];

    public RealSessionTraffic()
    {
        using var match = new InMemoryMatch(FightingFrames);
        match.RunUntil(() => match.Player1.CurrentFrame >= FightingFrames && SeesEveryType(match));
        Packets = [.. match.Player1Transport.Sent, .. match.Player2Transport.Sent];
    }

    public IReadOnlyList<byte[]> Packets { get; }

    public static ushort TypeOf(byte[] packet) => BinaryPrimitives.ReadUInt16BigEndian(packet);

    /// <summary>One sent packet of each message type seen.</summary>
    public IEnumerable<byte[]> OnePerType() => Packets.GroupBy(TypeOf).Select(group => group.First());

    private static bool SeesEveryType(InMemoryMatch match)
    {
        var seen = new HashSet<ushort>(match.Player1Transport.Sent.Select(TypeOf));
        seen.UnionWith(match.Player2Transport.Sent.Select(TypeOf));
        return seen.IsSupersetOf(_expectedTypes);
    }
}
