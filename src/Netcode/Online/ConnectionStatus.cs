namespace Netcode.Online;

/// <summary>How the connection to the other PC is doing, for the player's on-screen message (ADR-0005 §4).</summary>
/// <param name="State">Connected, interrupted or disconnected.</param>
/// <param name="SecondsUntilDisconnect">
/// While <see cref="ConnectionState.Interrupted"/>: whole seconds left, rounded up, before the match ends; 0 otherwise.
/// </param>
public readonly record struct ConnectionStatus(ConnectionState State, int SecondsUntilDisconnect)
{
    public static ConnectionStatus Connected => new(ConnectionState.Connected, 0);

    public static ConnectionStatus Disconnected => new(ConnectionState.Disconnected, 0);
}
