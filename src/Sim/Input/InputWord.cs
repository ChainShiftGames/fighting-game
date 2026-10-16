namespace Sim.Input;

/// <summary>One player's directions and buttons for one frame, packed into 16 bits (ADR-0010).</summary>
/// <remarks>
/// Layout, lowest bit first:
/// <list type="bullet">
/// <item>Bits 0–3: directions <see cref="Up"/>, <see cref="Down"/>, <see cref="Left"/>, <see cref="Right"/>. Fixed in
/// code. A raw word from a device may hold opposite directions together; the input step resolves them (ADR-0002).</item>
/// <item>Bits 4–15: buttons. Which bit is which button comes from the <see cref="ButtonSet"/> in data, never from
/// code. The default set (<c>data/input/buttons.json</c>) puts A, B, C, D, Special, Dash on bits 4–9, leaving bits
/// 10–15 spare for future buttons.</item>
/// </list>
/// Bits the button set doesn't define are dropped by the input step, so the logic never sees them.
/// </remarks>
public readonly record struct InputWord(ushort Bits)
{
    /// <summary>Bits in one input word.</summary>
    public const int BitCount = 16;

    public const ushort Up = 1 << 0;
    public const ushort Down = 1 << 1;
    public const ushort Left = 1 << 2;
    public const ushort Right = 1 << 3;

    /// <summary>All four direction bits.</summary>
    public const ushort DirectionMask = Up | Down | Left | Right;

    /// <summary>The lowest bit a button may use: everything below it is a direction.</summary>
    public const int FirstButtonBit = 4;

    public static InputWord Neutral => default;

    public bool IsHeld(ushort mask) => (Bits & mask) == mask;
}
