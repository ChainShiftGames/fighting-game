namespace Netcode.Online;

/// <summary>Reads how an online match's connection is doing, for the developer overlay (P0-22).</summary>
public interface IConnectionReadout
{
    /// <summary>False when there is no other player (SyncTest) or no reading yet.</summary>
    bool TryReadConnection(out ConnectionStats stats);
}
