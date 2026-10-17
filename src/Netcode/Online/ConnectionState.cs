namespace Netcode.Online;

/// <summary>Whether the other PC is still heard from (ADR-0005 §4).</summary>
public enum ConnectionState
{
    /// <summary>Packets arrive (or the match hasn't started yet).</summary>
    Connected,

    /// <summary>Nothing arrived for the warning time. The match waits; it resumes if the connection comes back in time.</summary>
    Interrupted,

    /// <summary>Nothing arrived for the end time: the match is over, without a result.</summary>
    Disconnected,
}
