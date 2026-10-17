using System;
using System.Threading;
using Netcode.Desync;

namespace Netcode.Transport;

/// <summary>
/// Carries the desync check's checksums over the match's own transport, next to Backdash's packets (ADR-0019).
/// </summary>
/// <remarks>
/// Unreliable packets, about one per checksum sent, that repeat every checksum the other PC hasn't acknowledged yet: a
/// lost packet is covered by the next one, with no resend timer while the match runs. Each packet also acknowledges
/// the other PC's checksums, so repeats stop once they arrived. The receiving side passes checksums on in frame order,
/// each exactly once, whatever order and how often packets arrive. Fixed buffers allocated once: nothing is
/// allocated per frame. Packets arrive on the network thread and are taken on the game thread, hence the lock.
/// </remarks>
internal sealed class TransportChecksumLink : IChecksumLink, IThreadPoolWorkItem
{
    // After a desync this PC stops running frames, so nothing else would repeat its last checksums: a few resends
    // over a couple of seconds get them to the other PC even through ADR-0005's packet loss.
    private const int ResendsAfterDesync = 20;
    private static readonly TimeSpan _resendInterval = TimeSpan.FromMilliseconds(100);

    private readonly Lock _lock = new();
    private readonly ulong[] _unacknowledged;
    private readonly FrameChecksum[] _received;
    private readonly ulong[] _packetChecksums = new ulong[ChecksumMessage.MaxChecksums];
    private readonly byte[] _packet = new byte[ChecksumMessage.MaxSize];
    private ITransport? _transport;
    private int _sentThrough;
    private int _acknowledgedThrough;
    private int _receivedThrough;
    private int _receivedNext;
    private int _receivedCount;
    private Timer? _resends;
    private int _resendsLeft;
    private int _transmitQueued;

    /// <param name="capacity">
    /// Checksums kept each way: how far the other PC's acknowledgements, and the game thread's reading, may lag.
    /// The desync history length (developer setting), which bounds the comparison the same way.
    /// </param>
    public TransportChecksumLink(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _unacknowledged = new ulong[capacity];
        _received = new FrameChecksum[capacity];
    }

    /// <summary>The transport the session opened; packets go out on it from now on.</summary>
    public void Attach(ITransport transport)
    {
        lock (_lock)
        {
            _transport = transport;
        }
    }

    /// <exception cref="ArgumentException">Not the frame after the last one sent.</exception>
    /// <exception cref="InvalidOperationException">
    /// The other PC hasn't acknowledged a whole history's worth of checksums: it stopped comparing.
    /// </exception>
    public void Send(FrameChecksum checksum)
    {
        lock (_lock)
        {
            if (checksum.Frame != _sentThrough + 1)
            {
                throw new ArgumentException(
                    $"Checksums go out in frame order: expected frame {_sentThrough + 1}, got {checksum.Frame}.", nameof(checksum));
            }

            if (_sentThrough - _acknowledgedThrough >= _unacknowledged.Length)
            {
                throw new InvalidOperationException(
                    $"The other PC hasn't acknowledged a checksum since frame {_acknowledgedThrough} for " +
                    $"{_unacknowledged.Length} frames. Raise the desync history length.");
            }

            _unacknowledged[checksum.Frame % _unacknowledged.Length] = checksum.Checksum;
            _sentThrough = checksum.Frame;
        }

        // Perf: a socket send costs the frame thread tens of µs (twice the rest of the frame on loopback UDP), so the
        // packet goes out from the thread pool, as Backdash's own packets go out from its send thread. Queuing this
        // object allocates nothing; checksums sent before the packet is built share it.
        if (Interlocked.Exchange(ref _transmitQueued, 1) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
        }
    }

    void IThreadPoolWorkItem.Execute()
    {
        Volatile.Write(ref _transmitQueued, 0);
        lock (_lock)
        {
            Transmit();
        }
    }

    public bool TryReceive(out FrameChecksum checksum)
    {
        lock (_lock)
        {
            if (_receivedCount == 0)
            {
                checksum = default;
                return false;
            }

            checksum = _received[_receivedNext];
            _receivedNext = (_receivedNext + 1) % _received.Length;
            _receivedCount--;
            return true;
        }
    }

    public void FinishAfterDesync()
    {
        lock (_lock)
        {
            if (_resends is not null || _sentThrough == _acknowledgedThrough)
            {
                return;
            }

            _resendsLeft = ResendsAfterDesync;
            _resends = new Timer(_ => Resend(), null, _resendInterval, _resendInterval);
        }
    }

    /// <summary>Takes a checksum packet from the other PC (network thread).</summary>
    /// <returns>False when the packet is malformed; it is ignored then.</returns>
    public bool Deliver(ReadOnlySpan<byte> packet)
    {
        if (!ChecksumMessage.TryRead(packet, out int acknowledged, out int firstFrame, out int count))
        {
            return false;
        }

        lock (_lock)
        {
            // An acknowledgement of a checksum never sent isn't from this match's other PC.
            if (acknowledged > _sentThrough)
            {
                return false;
            }

            _acknowledgedThrough = Math.Max(_acknowledgedThrough, acknowledged);
            for (int i = 0; i < count; i++)
            {
                int frame = firstFrame + i;

                // Older ones are duplicates; after a gap (a reordered packet) the rest comes again, as it isn't acknowledged.
                if (frame <= _receivedThrough)
                {
                    continue;
                }

                if (frame > _receivedThrough + 1 || _receivedCount == _received.Length)
                {
                    break;
                }

                _received[(_receivedNext + _receivedCount) % _received.Length] =
                    new FrameChecksum(frame, ChecksumMessage.ChecksumAt(packet, i));
                _receivedCount++;
                _receivedThrough = frame;
            }

            return true;
        }
    }

    // Caller holds the lock: the packet buffer is shared by the thread-pool sends and the resend timer.
    private void Transmit()
    {
        if (_transport is null)
        {
            return;
        }

        int count = Math.Min(_sentThrough - _acknowledgedThrough, ChecksumMessage.MaxChecksums);
        int firstFrame = _acknowledgedThrough + 1;
        for (int i = 0; i < count; i++)
        {
            _packetChecksums[i] = _unacknowledged[(firstFrame + i) % _unacknowledged.Length];
        }

        int length = ChecksumMessage.Write(_receivedThrough, firstFrame, _packetChecksums.AsSpan(0, count), _packet);
        try
        {
            _transport.Send(_packet.AsSpan(0, length));
        }
        catch (ObjectDisposedException)
        {
            // The match is over and its transport closed: like any packet sent then, this one is lost.
            _transport = null;
        }
    }

    private void Resend()
    {
        lock (_lock)
        {
            if (_resendsLeft-- <= 0 || _transport is null || _sentThrough == _acknowledgedThrough)
            {
                _resends?.Dispose();
                return;
            }

            Transmit();
        }
    }
}
