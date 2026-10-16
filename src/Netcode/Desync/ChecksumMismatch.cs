namespace Netcode.Desync;

/// <summary>The first frame whose checksums differ between the two PCs, and the last frame they agreed on.</summary>
/// <param name="LastAgreedFrame">0 when no frame has matched yet.</param>
public readonly record struct ChecksumMismatch(int Frame, ulong LocalChecksum, ulong RemoteChecksum, int LastAgreedFrame);
