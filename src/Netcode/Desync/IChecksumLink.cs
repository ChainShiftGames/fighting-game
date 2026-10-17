namespace Netcode.Desync;

/// <summary>Carries confirmed-frame checksums to the other PC and back (ADR-0005 §5).</summary>
/// <remarks>
/// A swap point: in-process peers (tests, soak runs) pass them in memory, online matches send them over the
/// match's transport (ADR-0019). Each side's checksums must arrive in frame order; none may be dropped silently.
/// </remarks>
public interface IChecksumLink
{
    void Send(FrameChecksum checksum);

    /// <summary>Takes the next checksum the other PC sent, if one has arrived.</summary>
    bool TryReceive(out FrameChecksum checksum);

    /// <summary>
    /// This PC found a desync and stops running frames: the checksums it already sent must still reach the other PC,
    /// so that one finds the desync too and writes its own dump.
    /// </summary>
    void FinishAfterDesync();
}
