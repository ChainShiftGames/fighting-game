namespace Sim.Input;

/// <summary>One player's directions and buttons for one frame, packed into 16 bits (ADR-0010).</summary>
public readonly record struct InputWord(ushort Bits)
{
    // Direction bits are fixed; button bit positions come from the button set in data (ADR-0010).
    public const ushort Up = 1 << 0;
    public const ushort Down = 1 << 1;
    public const ushort Left = 1 << 2;
    public const ushort Right = 1 << 3;

    public static InputWord Neutral => default;

    public bool IsHeld(ushort mask) => (Bits & mask) == mask;
}
