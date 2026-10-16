using System;
using System.IO;
using Sim.Input;
using Sim.State;

namespace Netcode.Desync;

/// <summary>
/// Compares every confirmed frame's checksum with the other PC's and, on the first mismatch, writes this PC's desync
/// dump and throws (ADR-0005 §5).
/// </summary>
/// <remarks>
/// The match driver feeds it: <see cref="RecordFrame"/> for every frame it runs (again for each resimulated frame)
/// and <see cref="ConfirmThrough"/> once frames can no longer be rolled back. Both PCs run one, so both write a dump.
/// Per frame it copies the state into its history and hashes it once; it allocates nothing until a desync.
/// </remarks>
public sealed class DesyncMonitor<TState>
    where TState : unmanaged
{
    private readonly DesyncDumpSettings _settings;
    private readonly DesyncDumpHeader _header;
    private readonly IChecksumLink _link;
    private readonly TimeProvider _clock;
    private readonly FrameHistory<TState> _history;
    private readonly ChecksumComparer _comparer;
    private int _confirmedFrame;

    public DesyncMonitor(DesyncDumpSettings settings, DesyncDumpHeader header, IChecksumLink link, TimeProvider clock)
    {
        _settings = settings;
        _header = header;
        _link = link;
        _clock = clock;
        _history = new FrameHistory<TState>(settings.HistoryFrames);
        _comparer = new ChecksumComparer(settings.HistoryFrames);
    }

    /// <summary>The newest frame both PCs agree on; 0 before the first.</summary>
    public int LastAgreedFrame => _comparer.LastAgreedFrame;

    /// <summary>Records a frame this PC ran: the state after it and both players' inputs.</summary>
    public void RecordFrame(int frame, ref readonly TState state, InputWord player1, InputWord player2) =>
        _history.Record(frame, in state, player1, player2);

    /// <summary>
    /// Marks every frame up to <paramref name="frame"/> final: sends their checksums to the other PC, then compares
    /// all checksums that have arrived.
    /// </summary>
    /// <exception cref="DesyncDetectedException">The PCs disagree; this PC's dump is at its <c>DumpPath</c>.</exception>
    public void ConfirmThrough(int frame)
    {
        for (int next = _confirmedFrame + 1; next <= frame; next++)
        {
            var checksum = new FrameChecksum(next, StateChecksum.Compute(in _history.Get(next).State));

            // Sent before comparing, so the other PC can find the desync too and write its own dump.
            _link.Send(checksum);
            _confirmedFrame = next;
            if (_comparer.AddLocal(checksum, out ChecksumMismatch mismatch))
            {
                Fail(mismatch);
            }
        }

        ReceiveRemoteChecksums();
    }

    /// <summary>Compares the checksums the other PC has sent so far, without confirming new frames.</summary>
    /// <exception cref="DesyncDetectedException">The PCs disagree; this PC's dump is at its <c>DumpPath</c>.</exception>
    public void ReceiveRemoteChecksums()
    {
        while (_link.TryReceive(out FrameChecksum remote))
        {
            if (_comparer.AddRemote(remote, out ChecksumMismatch mismatch))
            {
                Fail(mismatch);
            }
        }
    }

    private void Fail(ChecksumMismatch mismatch)
    {
        DesyncDump dump = DesyncDumpBuilder.Build(_header, mismatch, _history, _clock.GetUtcNow());
        string message = $"Checksums differ at frame {mismatch.Frame} (last agreed frame {mismatch.LastAgreedFrame}): " +
            $"this PC {DesyncDumpBuilder.Hex(mismatch.LocalChecksum)}, " +
            $"other PC {DesyncDumpBuilder.Hex(mismatch.RemoteChecksum)}.";
        string path;
        try
        {
            path = DesyncDumpFile.Write(_settings.Folder, dump);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The dump is lost, but the desync itself must still surface.
            throw new DesyncDetectedException($"{message} Writing the dump failed.", exception);
        }

        throw new DesyncDetectedException($"{message} Dump: {path}", path);
    }
}
