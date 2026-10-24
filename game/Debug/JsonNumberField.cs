using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FightingGame.Debug;

/// <summary>
/// Reads and rewrites one whole-number field of a JSON data file's text, by its key or a dotted path into nested
/// objects (<c>animations.hitstun.clipFrom</c>).
/// </summary>
/// <remarks>
/// Rewrites only the number, so the file keeps its exact layout, comments and key order and the git diff shows just
/// the value; a field added where it was missing goes first in its object. Validation stays with the file's loader,
/// which reloads it afterwards.
/// </remarks>
public static class JsonNumberField
{
    // Art files carry // comments; data files have none, so skipping them changes nothing there.
    private static readonly JsonReaderOptions _options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <exception cref="FormatException">The field is missing or isn't a whole number.</exception>
    public static int Read(string json, string key) =>
        Locate(Encoding.UTF8.GetBytes(json), key) is { IsFound: true } field
            ? field.Value
            : throw new FormatException($"'{key}' isn't there as a whole number.");

    /// <summary>The field's value, or <paramref name="missing"/> when its object is there without it.</summary>
    /// <exception cref="FormatException">The object holding the field is missing, or the field isn't a whole number.</exception>
    public static int ReadOr(string json, string key, int missing) =>
        Locate(Encoding.UTF8.GetBytes(json), key).IsFound ? Read(json, key) : missing;

    /// <exception cref="FormatException">The field is missing or isn't a whole number.</exception>
    public static string Write(string json, string key, int value)
    {
        Read(json, key);
        return WriteOrAdd(json, key, value);
    }

    /// <summary>Like <see cref="Write"/>, adding the field to its object when it's missing.</summary>
    /// <exception cref="FormatException">The object holding the field is missing, or the field isn't a whole number.</exception>
    public static string WriteOrAdd(string json, string key, int value)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        FieldLocation field = Locate(utf8, key);
        string number = value.ToString(CultureInfo.InvariantCulture);
        if (field.IsFound)
        {
            return Splice(utf8, field.Start, field.End, number);
        }

        string name = key[(key.LastIndexOf('.') + 1)..];
        int afterBrace = field.Start + 1;
        bool isEmpty = NextNonSpace(utf8, afterBrace) == '}';
        return Splice(utf8, afterBrace, afterBrace, $" \"{name}\": {number}{(isEmpty ? " " : ",")}");
    }

    // Found: Start..End is the number. Missing: Start is the '{' of the object it belongs in.
    private static FieldLocation Locate(byte[] utf8, string key)
    {
        string[] path = key.Split('.');
        var reader = new Utf8JsonReader(utf8, _options);
        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new FormatException("The file isn't a JSON object.");
            }

            for (int level = 0; level < path.Length; level++)
            {
                int objectStart = (int)reader.TokenStartIndex;
                if (!FindProperty(ref reader, path[level]))
                {
                    return level == path.Length - 1
                        ? new FieldLocation(false, objectStart, objectStart, 0)
                        : throw new FormatException($"'{string.Join('.', path[..(level + 1)])}' is missing.");
                }

                bool isLast = level == path.Length - 1;
                if (!isLast && reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new FormatException($"'{string.Join('.', path[..(level + 1)])}' isn't an object.");
                }

                if (isLast)
                {
                    return reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value)
                        ? new FieldLocation(true, (int)reader.TokenStartIndex, (int)reader.BytesConsumed, value)
                        : throw new FormatException($"'{key}' isn't a whole number.");
                }
            }
        }
        catch (JsonException exception)
        {
            throw new FormatException(exception.Message, exception);
        }

        throw new FormatException($"'{key}' is an empty path.");
    }

    // From an object's '{', moves the reader onto the named property's value; false when the object ends without it.
    private static bool FindProperty(ref Utf8JsonReader reader, string name)
    {
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            bool isMatch = reader.ValueTextEquals(name);
            reader.Read();
            if (isMatch)
            {
                return true;
            }

            reader.Skip();
        }

        return false;
    }

    private static char NextNonSpace(byte[] utf8, int from)
    {
        for (int i = from; i < utf8.Length; i++)
        {
            if (!char.IsWhiteSpace((char)utf8[i]))
            {
                return (char)utf8[i];
            }
        }

        return '\0';
    }

    private static string Splice(byte[] utf8, int start, int end, string text) =>
        string.Concat(
            Encoding.UTF8.GetString(utf8, 0, start), text, Encoding.UTF8.GetString(utf8, end, utf8.Length - end));

    private readonly record struct FieldLocation(bool IsFound, int Start, int End, int Value);
}
