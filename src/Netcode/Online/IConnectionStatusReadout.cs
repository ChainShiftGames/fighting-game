namespace Netcode.Online;

/// <summary>Reads whether an online match still hears from the other PC, for the player's on-screen message.</summary>
public interface IConnectionStatusReadout
{
    ConnectionStatus ReadConnectionStatus();
}
