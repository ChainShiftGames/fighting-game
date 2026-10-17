using Sim.Input;

namespace Netcode.Drivers.Training;

/// <summary>Turns an input word between screen directions and directions relative to a fighter's facing.</summary>
/// <remarks>
/// Recordings are kept as if facing right, so a recording made on one side plays back the same moves from the other
/// side: forward stays forward. The same swap converts both ways.
/// </remarks>
public static class FacingInputs
{
    private const ushort Horizontal = InputWord.Left | InputWord.Right;

    /// <param name="facing">+1 facing right (the word is unchanged), -1 facing left (left and right swap).</param>
    public static InputWord Turn(InputWord input, int facing)
    {
        if (facing > 0)
        {
            return input;
        }

        ushort horizontal = (ushort)(input.Bits & Horizontal);
        ushort turned = horizontal is InputWord.Left or InputWord.Right ? (ushort)(horizontal ^ Horizontal) : horizontal;
        return new InputWord((ushort)((input.Bits & ~Horizontal) | turned));
    }
}
