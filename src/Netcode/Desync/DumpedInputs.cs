namespace Netcode.Desync;

/// <summary>Both players' confirmed input words of one frame, in a desync dump.</summary>
public sealed record DumpedInputs(int Frame, int Player1, int Player2);
