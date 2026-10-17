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
    public static JsonObject? Read(IDataSource source, string path, DataErrors errors)
    {
        ReadOnlySpan<byte> bytes = source.ReadFile(path);
        // Sim.Data accepts a UTF-8 BOM; the writer doesn't add one back.
        if (bytes.StartsWith("﻿"u8))
        {
            bytes = bytes[3..];
        }

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
