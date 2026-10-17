using System;
using Backdash;
using Backdash.Synchronizing.Input.Confirmed;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Follows how many frames of a Backdash session have both players' inputs confirmed (its input listener).</summary>
/// <remarks>
/// Backdash reports confirmed frames at the start of a frame, after it has rolled back and resimulated every frame
/// whose prediction was wrong, so the frames it reports already ran with those inputs, or have yet to run.
/// </remarks>
public sealed class ConfirmedInputFrames : IInputListener<InputWord>
{
    /// <summary>Frames from the start whose inputs no player can change any more; 0 before the first.</summary>
    public int Count { get; private set; }

    void IInputListener<InputWord>.OnSessionStart(InputContext<InputWord> context)
    {
    }

    // Backdash numbers frames from 0 and reports them in order.
    void IInputListener<InputWord>.OnConfirmed(in Frame frame, in ConfirmedInputs<InputWord> inputs) =>
        Count = frame.Number + 1;

    void IInputListener<InputWord>.OnSessionClose()
    {
    }

    void IDisposable.Dispose()
    {
    }
}
