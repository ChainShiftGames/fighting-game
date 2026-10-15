namespace Benchmarks.Drivers;

/// <summary>Timing and allocations of a driver benchmark, plus how many timed frames only waited.</summary>
/// <param name="FrameThreadBytes">Bytes allocated by the frames' own thread, per driver; the rest of
/// <see cref="OperationResult.AllocatedBytes"/> came from Backdash's background threads.</param>
internal sealed record FrameResult(OperationResult Timing, long FrameThreadBytes, long WaitedFrames);
