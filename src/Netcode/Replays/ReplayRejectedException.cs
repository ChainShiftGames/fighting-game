using System;

namespace Netcode.Replays;

/// <summary>A replay file this build can't play back; the message says why in words a player understands.</summary>
public sealed class ReplayRejectedException : Exception
{
    public ReplayRejectedException(ReplayRejection reason, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public ReplayRejection Reason { get; }
}
