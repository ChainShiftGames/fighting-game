using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Reads a data file into an editable JSON tree. Only checks that it is a strict JSON object; every other rule is
/// left to Sim.Data's validation, so the editor never has rules of its own.
/// </summary>
public static class JsonDataReader
{
    private static readonly JsonDocumentOptions _strictJson = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        // A tree keeps only one of two equal keys, so saving would silently drop the other.
        AllowDuplicateProperties = false,
    };

    /// <summary>The file's root object, or null after adding the reason to <paramref name="errors"/>.</summary>
    public static JsonObject? Read(IDataSource source, string path, DataErrors errors) =>
        Parse(WithoutBom(source.ReadFile(path)), path, errors);

    /// <summary>The file ready for editing, with its list layout; null after adding the reason to errors.</summary>
    public static EditedFile? Open(IDataSource source, string path, DataErrors errors)
    {
        ReadOnlySpan<byte> bytes = WithoutBom(source.ReadFile(path));
        JsonObject? root = Parse(bytes, path, errors);
        return root is null ? null : new EditedFile(path, root, ListLineStarts.Read(bytes));
    }

    // Sim.Data accepts a UTF-8 BOM; the writer doesn't add one back.
    private static ReadOnlySpan<byte> WithoutBom(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith("﻿"u8) ? bytes[3..] : bytes;

    private static JsonObject? Parse(ReadOnlySpan<byte> bytes, string path, DataErrors errors)
    {
        try
        {
            if (JsonNode.Parse(bytes, documentOptions: _strictJson) is JsonObject root)
            {
                return root;
            }

            errors.Add(path, "", "the file must hold one JSON object");
        }
        catch (JsonException exception)
        {
            errors.Add(path, "", $"isn't valid JSON: {exception.Message}");
        }

        return null;
    }
}
