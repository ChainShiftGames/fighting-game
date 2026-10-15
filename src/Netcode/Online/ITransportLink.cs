namespace Netcode.Online;

/// <summary>What a platform transport itself knows about its link to the other PC, e.g. Steam's relay ping.</summary>
public interface ITransportLink
{
    /// <summary>False while there is no link to read yet (not connected).</summary>
    bool TryRead(out TransportLinkStats stats);
}
