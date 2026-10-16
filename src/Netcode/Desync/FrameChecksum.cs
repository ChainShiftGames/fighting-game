namespace Netcode.Desync;

/// <summary>The checksum of one confirmed frame's state, as the two PCs exchange them.</summary>
public readonly record struct FrameChecksum(int Frame, ulong Checksum);
