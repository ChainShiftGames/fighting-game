namespace Sim.Input;

/// <summary>The fixed SOCD rule (ADR-0002): left + right = neutral, up + down = up.</summary>
public static class SocdResolver
{
    public static InputWord Resolve(InputWord raw)
    {
        ushort bits = raw.Bits;
        if (raw.IsHeld(InputWord.Left | InputWord.Right))
        {
            bits &= unchecked((ushort)~(InputWord.Left | InputWord.Right));
        }

        if (raw.IsHeld(InputWord.Up | InputWord.Down))
        {
            bits &= unchecked((ushort)~InputWord.Down);
        }

        return new InputWord(bits);
    }
}
