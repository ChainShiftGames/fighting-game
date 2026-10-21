using System.Text;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>Text edits on a JSON file's UTF-8 bytes, for rewrites that keep the rest of the file as it was.</summary>
public static class JsonTextEdits
{
    /// <summary>The whitespace that starts the line holding <paramref name="index"/>.</summary>
    public static string IndentOfLine(byte[] bytes, int index) =>
        Encoding.UTF8.GetString(bytes, LineStart(bytes, index), IndentLength(bytes, LineStart(bytes, index)));

    /// <summary>Where the line holding <paramref name="index"/> starts.</summary>
    public static int LineStart(byte[] bytes, int index)
    {
        int lineStart = index;
        while (lineStart > 0 && bytes[lineStart - 1] != (byte)'\n')
        {
            lineStart--;
        }

        return lineStart;
    }

    /// <summary>The text with bytes <paramref name="start"/> to <paramref name="end"/> replaced.</summary>
    public static string Splice(byte[] bytes, int start, int end, string replacement) =>
        Encoding.UTF8.GetString(bytes, 0, start) + replacement + Encoding.UTF8.GetString(bytes, end, bytes.Length - end);

    private static int IndentLength(byte[] bytes, int lineStart)
    {
        int end = lineStart;
        while (end < bytes.Length && bytes[end] is (byte)' ' or (byte)'\t')
        {
            end++;
        }

        return end - lineStart;
    }
}
