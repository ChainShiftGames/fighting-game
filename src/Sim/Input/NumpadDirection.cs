namespace Sim.Input;

/// <summary>Reads an input word's direction in numpad notation, relative to where the fighter faces.</summary>
/// <remarks>
/// 6 is forward and 4 is back for the fighter's facing; 8 up, 2 down, 5 neutral. The word is already SOCD-cleaned,
/// so left+right never both appear.
/// </remarks>
public static class NumpadDirection
{
    public const int Neutral = 5;
    public const int Down = 2;

    /// <param name="facing">+1 facing right, -1 facing left.</param>
    public static int Of(InputWord input, int facing)
    {
        ushort forward = facing >= 0 ? InputWord.Right : InputWord.Left;
        ushort back = facing >= 0 ? InputWord.Left : InputWord.Right;
        int column = input.IsHeld(forward) ? 1 : input.IsHeld(back) ? -1 : 0;
        int row = input.IsHeld(InputWord.Up) ? 3 : input.IsHeld(InputWord.Down) ? -3 : 0;
        return Neutral + column + row;
    }

    public static bool IsUp(int direction) => direction >= 7;

    public static bool IsDown(int direction) => direction <= 3;

    public static bool IsForward(int direction) => direction % 3 == 0;

    public static bool IsBack(int direction) => direction % 3 == 1;
}
