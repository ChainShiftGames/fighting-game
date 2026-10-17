using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data;
using Sim.Data.Json;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Saving from the editor writes data in the hand-written layout and never changes what it means.</summary>
public sealed class JsonDataWriterTests
{
    [Fact]
    public void RewritingEveryCharacterFileKeepsTheDataHash()
    {
        DirectoryDataSource disk = new(TestGameData.DataRoot);
        Dictionary<string, byte[]> rewritten = new(StringComparer.Ordinal);
        foreach (string path in CharacterFiles())
        {
            rewritten[path] = Encoding.UTF8.GetBytes(JsonDataWriter.Write(JsonNode.Parse(disk.ReadFile(path))!));
        }

        GameData data = new GameDataLoader(new OverlayDataSource(disk, rewritten)).Load();

        Assert.Equal(TestGameData.Data.Hash, data.Hash);
    }

    // Not character.json: its move list is wrapped by hand into attacks and basic states, which no width rule
    // reproduces; the first save from the editor refills it.
    [Fact]
    public void TestmansMoveFilesKeepTheirLayout()
    {
        DirectoryDataSource disk = new(TestGameData.DataRoot);
        List<string> changed = [];
        foreach (string path in CharacterFiles())
        {
            if (!path.Contains("/moves/", StringComparison.Ordinal))
            {
                continue;
            }

            string text = Encoding.UTF8.GetString(disk.ReadFile(path)).ReplaceLineEndings("\n");
            if (JsonDataWriter.Write(JsonNode.Parse(text)!) != text)
            {
                changed.Add(path);
            }
        }

        Assert.Empty(changed);
    }

    [Fact]
    public void ShortObjectsAndListsStayOnOneLine()
    {
        string text = JsonDataWriter.Write(JsonNode.Parse(
            """{ "input": { "motion": "5", "buttons": ["a", "b"] }, "on": ["hit"], "empty": {}, "none": [] }""")!);

        Assert.Equal(
            "{\n  \"input\": { \"motion\": \"5\", \"buttons\": [\"a\", \"b\"] },\n  \"on\": [\"hit\"],\n"
            + "  \"empty\": {},\n  \"none\": []\n}\n",
            text);
    }

    [Fact]
    public void ObjectsWithManyFieldsGetOneFieldPerLine()
    {
        string text = JsonDataWriter.Write(JsonNode.Parse("""{ "hit": { "a": 1, "b": 2, "c": 3, "d": 4, "e": 5 } }""")!);

        Assert.Equal("{\n  \"hit\": {\n    \"a\": 1,\n    \"b\": 2,\n    \"c\": 3,\n    \"d\": 4,\n    \"e\": 5\n  }\n}\n", text);
    }

    [Fact]
    public void ListsOfObjectsPutEachObjectOnItsOwnLine()
    {
        string text = JsonDataWriter.Write(JsonNode.Parse("""{ "velocity": [{ "x": 1 }, { "x": 2, "on": ["hit"] }] }""")!);

        Assert.Equal(
            "{\n  \"velocity\": [\n    { \"x\": 1 },\n    {\n      \"x\": 2,\n      \"on\": [\"hit\"]\n    }\n  ]\n}\n",
            text);
    }

    [Fact]
    public void LongListsOfNamesFillLinesUpToTheLineWidth()
    {
        JsonArray names = [];
        for (int i = 0; i < 40; i++)
        {
            names.Add($"move_{i:00}");
        }

        string text = JsonDataWriter.Write(new JsonObject { ["moves"] = names });

        string[] lines = text.Split('\n');
        Assert.True(lines.Length > 4);
        Assert.All(lines, line => Assert.InRange(line.Length, 0, JsonDataWriter.LineWidth));
        Assert.StartsWith("    \"move_00\", \"move_01\",", lines[2], StringComparison.Ordinal);
        Assert.Equal(names.ToJsonString(), JsonNode.Parse(text)!["moves"]!.ToJsonString());
    }

    [Fact]
    public void NamesKeepTheirCharactersUnescaped()
    {
        string text = JsonDataWriter.Write(new JsonObject { ["displayName"] = "Zoë + \"Q\"" });

        Assert.Equal("{\n  \"displayName\": \"Zoë + \\\"Q\\\"\"\n}\n", text);
    }

    private static IEnumerable<string> CharacterFiles()
    {
        string root = TestGameData.DataRoot;
        foreach (string file in Directory.GetFiles(Path.Combine(root, "characters"), "*.json", SearchOption.AllDirectories))
        {
            yield return Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
