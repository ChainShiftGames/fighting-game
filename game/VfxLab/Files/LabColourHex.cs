using System;
using System.Globalization;

namespace FightingGame.VfxLab.Files;

/// <summary>Reads and writes colours as <c>#rrggbb</c> or <c>#rrggbbaa</c>, the way sheets and feedback files hold them.</summary>
public static class LabColourHex
{
    public static bool TryParse(string text, out LabValue colour)
    {
        colour = default;
        if (text.Length is not (7 or 9) || text[0] != '#')
        {
            return false;
        }

        float[] numbers = [0f, 0f, 0f, 1f];
        for (int i = 0; i < (text.Length - 1) / 2; i++)
        {
            if (!int.TryParse(text.AsSpan(1 + (i * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int channel))
            {
                return false;
            }

            numbers[i] = channel / 255f;
        }

        colour = new LabValue(LabValueFormat.Colour, numbers);
        return true;
    }

    public static string Format(LabValue colour)
    {
        float[] n = colour.Numbers;
        string rgb = $"#{Channel(n[0])}{Channel(n[1])}{Channel(n[2])}";
        return n.Length < 4 || n[3] >= 1f ? rgb : rgb + Channel(n[3]);
    }

    private static string Channel(float value) =>
        ((int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f)).ToString("x2", CultureInfo.InvariantCulture);
}
