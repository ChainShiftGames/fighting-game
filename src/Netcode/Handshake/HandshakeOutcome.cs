namespace Netcode.Handshake;

/// <summary>Where the version handshake with the other PC stands.</summary>
public enum HandshakeOutcome
{
    /// <summary>No hello from the other PC yet, or it hasn't seen ours.</summary>
    Waiting,

    /// <summary>Same build, data and rules on both PCs: the match may start.</summary>
    Agreed,

    /// <summary>Something differs; the match never starts.</summary>
    Refused,
}
