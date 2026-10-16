using System;
using System.Text.Json;
using Sim.Data.Sources;

namespace Sim.Data.Validation;

/// <summary>Reads one JSON data file and hands out a <see cref="FieldReader"/> for its root object.</summary>
public static class JsonDataFile
{
    // Strict standard JSON: no comments or trailing commas, so every tool (editor plugin, schema validators,
    // agents) reads the files the same way.
    private static readonly JsonDocumentOptions _options = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32,
    };

    /// <summary>
    /// The root object of <paramref name="path"/>, or null when the file is missing, isn't valid JSON or isn't an
    /// object (the reason is added to <paramref name="errors"/>).
    /// </summary>
    public static FieldReader? Open(IDataSource source, string path, DataErrors errors)
    {
        if (!source.FileExists(path))
        {
            errors.Add(path, "", "file does not exist (paths are case-sensitive)");
            return null;
        }

        JsonElement root;
        try
        {
            // The parsed values are copied into definitions straight away, so the document can be cloned and
            // released here instead of being kept alive by the caller.
            using JsonDocument document = JsonDocument.Parse(WithoutByteOrderMark(source.ReadFile(path)), _options);
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            long line = (exception.LineNumber ?? 0) + 1;
            long column = (exception.BytePositionInLine ?? 0) + 1;
            errors.Add(path, "", $"not valid JSON at line {line}, byte {column}: {FirstSentence(exception.Message)}");
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add(path, "", "the file must contain a JSON object");
            return null;
        }

        return new FieldReader(root, path, "", errors);
    }

    // Some Windows editors save UTF-8 with a byte order mark; it carries no data, so it is skipped rather than
    // rejected.
    private static ReadOnlyMemory<byte> WithoutByteOrderMark(byte[] bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? bytes.AsMemory(3)
            : bytes;

    // System.Text.Json appends its own path and position details; the line and byte are reported above already.
    private static string FirstSentence(string message)
    {
        int end = message.IndexOf(". ", StringComparison.Ordinal);
        return end < 0 ? message : message[..(end + 1)];
    }
}
