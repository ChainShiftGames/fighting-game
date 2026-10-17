namespace Netcode.Drivers;

/// <summary>How much of a running match is final: frames no rollback can change any more.</summary>
public interface IConfirmedFrameReadout
{
    /// <summary>
    /// Frames from the start that ran with both players' confirmed inputs, so they will never change; 0 before the
    /// first. At most <see cref="IMatchDriver{TState}.CurrentFrame"/>.
    /// </summary>
    int ConfirmedFrame { get; }
}
