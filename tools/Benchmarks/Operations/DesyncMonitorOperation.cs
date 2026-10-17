using System;
using System.IO;
using Netcode.Desync;
using Sim.Input;
using Sim.State;

namespace Benchmarks.Operations;

/// <summary>
/// What the online desync check adds to one frame on one PC (ADR-0005 §5): keep the frame in the history, then, once
/// it is final, hash it, send the checksum and compare it with the other PC's.
/// </summary>
/// <remarks>
/// The other PC's matching checksum is handed in through an in-memory link, so every call compares. The transport
/// link's packet is not included: its send is a network call, timed with the Backdash peer frames.
/// </remarks>
internal struct DesyncMonitorOperation : IBenchmarkOperation
{
    private readonly DesyncMonitor<MatchState> _monitor;
    private readonly InMemoryChecksumLink _otherPc;
    private readonly MatchState _state;
    private readonly ulong _checksum;
    private int _frame;

    public DesyncMonitorOperation(MatchState state, int historyFrames)
    {
        (InMemoryChecksumLink thisPc, InMemoryChecksumLink otherPc) = InMemoryChecksumLink.CreatePair();
        var settings = new DesyncDumpSettings
        {
            // Never written: the checksums always match.
            Folder = Path.Combine(Path.GetTempPath(), "fg-benchmark-desyncs"),
            HistoryFrames = historyFrames,
        };
        _monitor = new DesyncMonitor<MatchState>(
            settings, new DesyncDumpHeader("benchmark", "benchmark", true), thisPc, TimeProvider.System);
        _otherPc = otherPc;
        _state = state;
        _checksum = StateChecksum.Compute(in state);
    }

    public readonly string Name => "Desync monitor (record + confirm a frame)";

    public void Run()
    {
        _frame++;
        _otherPc.Send(new FrameChecksum(_frame, _checksum));
        _monitor.RecordFrame(_frame, in _state, InputWord.Neutral, InputWord.Neutral);
        _monitor.ConfirmThrough(_frame);

        // Takes what this PC sent, as the other PC would, so the link's queue stays at its size.
        _otherPc.TryReceive(out _);
    }
}
