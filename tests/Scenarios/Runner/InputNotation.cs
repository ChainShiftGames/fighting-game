using System;
using System.Collections.Generic;
using Sim.Input;

namespace Scenarios.Tests.Runner;

/// <summary>Reads and writes one frame of input in numpad notation: <c>5</c>, <c>6a</c>, <c>2c+d</c>, <c>4+6</c>.</summary>
/// <remarks>
/// Parts are joined by <c>+</c>; each is a numpad direction, a button id, or a direction followed by a button.
/// Directions are relative to the fighter's facing, as players write them (6 = forward), so words here are "as if
/// facing right" (6 = Right). <see cref="ForFacing"/> turns them into the absolute word the logic reads. Button
/// ids are those of the button set in data (ADR-0010).
/// </remarks>
public static class InputNotation
{
    public static InputWord Parse(string text)
    {
        int bits = 0;
        foreach (string part in text.Split('+'))
        {
            if (part.Length == 0)
            {
                throw new FormatException($"'{text}' has an empty part around a '+'");
            }

            int buttonStart = 0;
            if (char.IsAsciiDigit(part[0]))
            {
                bits |= DirectionBits(part[0], text);
                buttonStart = 1;
            }

            if (buttonStart < part.Length)
            {
                bits |= ButtonMask(part[buttonStart..], text);
            }
        }

        return new InputWord((ushort)bits);
    }

    public static string Format(InputWord input)
    {
        List<string> parts = DirectionParts(input);
        List<string> buttons = ButtonParts(input);
        if (buttons.Count > 0)
        {
            parts[^1] += buttons[0];
            parts.AddRange(buttons.GetRange(1, buttons.Count - 1));
        }

        return string.Join("+", parts);
    }

    /// <summary>Mirrors left and right for a fighter facing left; its own inverse, so it also converts back.</summary>
    public static InputWord ForFacing(InputWord input, int facing)
    {
        if (facing >= 0)
        {
            return input;
        }

        int bits = input.Bits & ~(InputWord.Left | InputWord.Right);
        bits |= input.IsHeld(InputWord.Left) ? InputWord.Right : 0;
        bits |= input.IsHeld(InputWord.Right) ? InputWord.Left : 0;
        return new InputWord((ushort)bits);
    }

    // Numpad layout 7 8 9 / 4 5 6 / 1 2 3, written facing right.
    private static int DirectionBits(char digit, string text)
    {
        if (digit == '0')
        {
            throw new FormatException($"'{text}': directions are numpad digits 1-9 (5 = neutral)");
        }

        int index = digit - '1';
        int row = index / 3;
        int column = index % 3;
        int bits = row == 0 ? InputWord.Down : row == 2 ? InputWord.Up : 0;
        bits |= column == 0 ? InputWord.Left : column == 2 ? InputWord.Right : 0;
        return bits;
    }

    private static int ButtonMask(string button, string text)
    {
        if (Buttons.TryFind(button, out int index))
        {
            return Buttons.MaskOf(index);
        }

        if (char.IsAsciiDigit(button[0]))
        {
            throw new FormatException(
                $"'{text}' holds more than one direction; a motion is one input per frame, e.g. 'frame 1: p1 2, 3, 6c'");
        }

        throw new FormatException(
            $"'{button}' in '{text}' isn't a button; buttons: {string.Join(", ", ButtonIds())}");
    }

    private static List<string> DirectionParts(InputWord input)
    {
        bool left = input.IsHeld(InputWord.Left);
        bool right = input.IsHeld(InputWord.Right);
        bool up = input.IsHeld(InputWord.Up);
        bool down = input.IsHeld(InputWord.Down);
        if ((left && right) || (up && down))
        {
            // Unresolved SOCD (only raw device input looks like this): list every held direction.
            List<string> held = [];
            AddIf(held, left, "4");
            AddIf(held, right, "6");
            AddIf(held, up, "8");
            AddIf(held, down, "2");
            return held;
        }

        int digit = 5 + (right ? 1 : 0) - (left ? 1 : 0) + (up ? 3 : 0) - (down ? 3 : 0);
        return [((char)('0' + digit)).ToString()];
    }

    private static List<string> ButtonParts(InputWord input)
    {
        List<string> buttons = [];
        for (int index = 0; index < Buttons.Count; index++)
        {
            AddIf(buttons, Buttons.IsHeld(input, index), Buttons.IdOf(index));
        }

        // Spare bits (ADR-0010) have no id; show them by number so a mismatch is still readable.
        for (int bit = InputWord.FirstButtonBit; bit < InputWord.BitCount; bit++)
        {
            AddIf(buttons, (input.Bits & (1 << bit) & ~Buttons.AllButtons) != 0, $"bit{bit}");
        }

        return buttons;
    }

    private static ButtonSet Buttons => ScenarioData.Shared.Buttons;

    private static IEnumerable<string> ButtonIds()
    {
        for (int index = 0; index < Buttons.Count; index++)
        {
            yield return Buttons.IdOf(index);
        }
    }

    private static void AddIf(List<string> parts, bool condition, string part)
    {
        if (condition)
        {
            parts.Add(part);
        }
    }
}
