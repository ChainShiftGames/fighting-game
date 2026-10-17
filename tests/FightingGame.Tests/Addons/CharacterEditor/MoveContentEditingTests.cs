using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Cancel windows, inputs, categories, new moves and move references (P3-41).</summary>
public sealed class MoveContentEditingTests
{
    private readonly DirectoryDataSource _disk = new(TestGameData.DataRoot);
    private readonly SchemaObject _moveSchema;

    public MoveContentEditingTests()
    {
        _moveSchema = DataSchema.Read(_disk, "move")!.Root;
    }

    [Fact]
    public void ANewCancelWindowRunsFromTheFirstActiveFrameToTheEnd()
    {
        JsonObject move = Read("2a");
        move.Remove("cancels");

        JsonObject window = MoveCancels.Add(move, _moveSchema.FieldNames);

        FramePhases phases = FramePhases.Of(move)!;
        Assert.Equal(phases.FirstActiveFrame, window["firstFrame"]!.GetValue<int>());
        Assert.Equal(phases.TotalFrames, window["lastFrame"]!.GetValue<int>());
        Assert.Equal(["hit"], JsonStringList.Read(window, "on"));
        // Placed where the schema lists cancels: after boxes, before hit.
        Assert.Equal(IndexOf(move, "boxes") + 1, IndexOf(move, "cancels"));

        MoveCancels.Remove(move, window);
        Assert.False(move.ContainsKey("cancels"));
    }

    [Fact]
    public void CancelTargetsAreTheAttacks()
    {
        IReadOnlyList<string> targets = MoveCancels.TargetsOf(CharacterDocument.Open(_disk, "testman"));

        Assert.Contains("5a", targets);
        Assert.Contains("236c", targets);
        Assert.DoesNotContain("idle", targets);
    }

    [Fact]
    public void AnOptionalTargetListGoesWhenItsLastMemberDoes()
    {
        JsonObject window = new() { ["on"] = new JsonArray("hit") };

        JsonStringList.Set(window, "into", "5b", included: true, keepWhenEmpty: false, ["on", "into"]);
        JsonStringList.Set(window, "into", "5b", included: true, keepWhenEmpty: false, ["on", "into"]);
        Assert.Equal(["5b"], JsonStringList.Read(window, "into"));

        JsonStringList.Set(window, "into", "5b", included: false, keepWhenEmpty: false, ["on", "into"]);
        Assert.False(window.ContainsKey("into"));

        JsonStringList.Set(window, "on", "hit", included: false, keepWhenEmpty: true, ["on", "into"]);
        Assert.Empty(window["on"]!.AsArray());
    }

    [Fact]
    public void OneButtonIsButtonAndSeveralAreButtons()
    {
        JsonObject move = Read("5a");

        MoveInput.SetButtons(move, ["a", "b"], _moveSchema.FieldNames);
        Assert.Equal(["a", "b"], MoveInput.Buttons(move));
        Assert.False(move["input"]!.AsObject().ContainsKey("button"));

        MoveInput.SetButtons(move, ["c"], _moveSchema.FieldNames);
        Assert.Equal("c", move["input"]!["button"]!.GetValue<string>());
        Assert.False(move["input"]!.AsObject().ContainsKey("buttons"));

        MoveInput.SetMotion(move, "236", _moveSchema.FieldNames);
        Assert.Equal("236", MoveInput.Motion(move));
    }

    [Fact]
    public void ChangingBetweenBasicAndAttackKeepsTheLength()
    {
        JsonObject move = Read("5a");
        int frames = FramePhases.Of(move)!.TotalFrames;

        MoveCategory.Set(move, "basic", _moveSchema, "a");
        Assert.Equal(FramePhases.Basic(frames), FramePhases.Of(move));
        Assert.False(move.ContainsKey("input"));
        Assert.False(move.ContainsKey("usableIn"));

        MoveCategory.Set(move, "special", _moveSchema, "a");
        Assert.Equal(frames, FramePhases.Of(move)!.TotalFrames);
        Assert.False(move.ContainsKey("duration"));
        Assert.Equal("ground", move["usableIn"]!.GetValue<string>());
        Assert.Equal(["a"], MoveInput.Buttons(move));
    }

    [Fact]
    public void ANewAttackHasEveryFieldItsCategoryNeeds()
    {
        JsonObject move = MoveTemplate.New("5e", "normal", _moveSchema, "a");

        Assert.Equal("5e", move["id"]!.GetValue<string>());
        Assert.Equal(FramePhases.Attack(1, 1, 0), FramePhases.Of(move));
        Assert.Equal("5", MoveInput.Motion(move));
        Assert.Equal(["$schema", "schemaVersion", "id", "category", "frameData", "usableIn", "input", "boxes"], Keys(move));
    }

    [Fact]
    public void ACopyHasTheNewIdAndLeavesTheSourceAlone()
    {
        JsonObject source = Read("5a");

        JsonObject copy = MoveTemplate.Copy(source, "5a_copy", _moveSchema);

        Assert.Equal("5a_copy", copy["id"]!.GetValue<string>());
        Assert.Equal("5a", source["id"]!.GetValue<string>());
        Assert.Equal(Keys(source), Keys(copy));
    }

    [Fact]
    public void ReferencesListStatesAndCancelTargets()
    {
        CharacterDocument document = CharacterDocument.Open(_disk, "testman");

        Assert.Equal(["character.json: states.idle"], MoveReferences.Find(document, "idle"));
        // 5a gatlings into itself, which doesn't count: deleting it removes that window too.
        IReadOnlyList<string> places = MoveReferences.Find(document, "5a");
        Assert.NotEmpty(places);
        Assert.DoesNotContain("5a.json: cancel 1 into", places);
    }

    private JsonObject Read(string moveId) =>
        JsonNode.Parse(_disk.ReadFile($"characters/testman/moves/{moveId}.json"))!.AsObject();

    private static List<string> Keys(JsonObject obj)
    {
        List<string> keys = [];
        foreach (KeyValuePair<string, JsonNode?> field in obj)
        {
            keys.Add(field.Key);
        }

        return keys;
    }

    private static int IndexOf(JsonObject obj, string key) => Keys(obj).IndexOf(key);
}
