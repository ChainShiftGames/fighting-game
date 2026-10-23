using System.Text;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data.Json;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Move list changes, frame data changes and layout preservation (P3-41).</summary>
public sealed class MoveEditingTests
{
    private readonly DirectoryDataSource _disk = new(TestGameData.DataRoot);

    [Fact]
    public void SavingCharacterJsonKeepsTheHandWrappedMoveList()
    {
        const string CharacterFile = "characters/testman/character.json";
        byte[] bytes = _disk.ReadFile(CharacterFile);
        string text = Encoding.UTF8.GetString(bytes).ReplaceLineEndings("\n");

        string written = JsonDataWriter.Write(JsonNode.Parse(text)!, ListLineStarts.Read(Encoding.UTF8.GetBytes(text)));

        Assert.Equal(text, written);
    }

    [Fact]
    public void AddingAndRemovingAMoveKeepsListAndFilesTogether()
    {
        CharacterDocument document = CharacterDocument.Open(_disk, "testman");
        JsonObject copy = document.Move("5a")!.Root.DeepClone().AsObject();
        copy["id"] = "5a_copy";

        document.AddMove("5a_copy", copy, 1);

        Assert.Equal("5a_copy", document.MoveIds[1]);
        Assert.NotNull(document.Move("5a_copy"));
        Assert.Empty(document.RemovedFiles);

        document.RemoveMove("5a_copy");
        Assert.DoesNotContain("5a_copy", document.MoveIds);
        Assert.Empty(document.RemovedFiles);

        document.RemoveMove("5a");
        Assert.Equal(["characters/testman/moves/5a.json"], document.RemovedFiles);
    }

    [Fact]
    public void ACaseOnlyDifferentIdIsRefused()
    {
        Assert.NotNull(MoveIdCheck.Problem("5A", ["5a"]));
        Assert.NotNull(MoveIdCheck.Problem("../x", []));
        Assert.Null(MoveIdCheck.Problem("5e", ["5a"]));
    }

    [Fact]
    public void LongerStartupKeepsBoxesOnTheirPhases()
    {
        // 5a: startup 5, active 3 (5..7), recovery 8 (8..15); hit box 5..7, hurt box 1..15, cancel 5..12.
        JsonObject move = EditorFixtures.Testman5A();

        MoveFrameData.Set(move, FramePhases.Attack(7, 3, 8), moveRanges: true, []);

        JsonArray boxes = move["boxes"]!.AsArray();
        Assert.Equal((1, 17), Range(boxes[0]!));
        Assert.Equal((7, 9), Range(boxes[3]!));
        Assert.Equal((7, 14), Range(move["cancels"]![0]!));
        Assert.Equal(7, move["frameData"]!["startup"]!.GetValue<int>());
    }

    [Fact]
    public void ShrinkingActiveFramesClampsRanges()
    {
        FrameRemap remap = new(FramePhases.Attack(5, 3, 8), FramePhases.Attack(5, 1, 8));

        Assert.Equal(5, remap.First(5));
        Assert.Equal(5, remap.Last(7));
        Assert.Equal(6, remap.First(8));
        Assert.Equal(13, remap.Last(15));
    }

    [Fact]
    public void TheTimelineShowsPhasesBoxesAndCancels()
    {
        JsonObject move = EditorFixtures.Testman5A();

        MoveTimeline timeline = MoveTimeline.Of(move);

        Assert.Equal(15, timeline.FrameCount);
        Assert.Equal("Frames", timeline.Rows[0].Label);
        Assert.Equal(3, timeline.Rows[0].Spans.Count);
        Assert.Contains(timeline.Rows, row => row.Label == "Hit boxes");
        Assert.Contains(timeline.Rows, row => row.Label.StartsWith("Cancel 1", System.StringComparison.Ordinal));
    }

    [Fact]
    public void TheMoveSchemaDescribesHitProperties()
    {
        DataSchema schema = DataSchema.Read(_disk, "move")!;

        SchemaObject hit = schema.Root.Field("hits")!.Items!;

        Assert.Equal(SchemaFieldKind.Integer, hit.Field("damage")!.Kind);
        Assert.Equal(["mid", "low", "overhead", "unblockable"], hit.Field("guard")!.Choices);
        Assert.False(hit.Field("chipDamage")!.IsRequired);
        Assert.Equal(SchemaFieldKind.ChoiceList, schema.Root.Field("cancels")!.Items!.Field("on")!.Kind);
    }

    private static (int, int) Range(JsonNode range) =>
        (range["firstFrame"]!.GetValue<int>(), range["lastFrame"]!.GetValue<int>());
}
