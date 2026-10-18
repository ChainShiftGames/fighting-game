using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data.Rules;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>A move's flags, cost and follow-ups (ADR-0023) as the editor's forms see and edit them.</summary>
public sealed class MoveConditionEditingTests
{
    private static readonly DirectoryDataSource _disk = new(TestGameData.DataRoot);
    private static readonly SchemaObject _moveSchema = EditingContext.Read(_disk).MoveSchema;

    public static TheoryData<string, string> FlagLists => new()
    {
        { "", "setsFlags" },
        { "", "clearsFlags" },
        { "requires", "flags" },
        { "requires", "notFlags" },
    };

    [Theory]
    [MemberData(nameof(FlagLists))]
    public void FlagListsOfferTheFlagNamesInFileOrder(string parent, string name)
    {
        SchemaField field = (parent == "" ? _moveSchema : _moveSchema.ObjectOf(parent)).Field(name)!;

        Assert.Equal(SchemaFieldKind.ChoiceList, field.Kind);
        Assert.Equal(FighterFlagsLoader.Load(_disk, new DataErrors())!.Names.Names, field.Choices);
    }

    [Fact]
    public void TheChoicesComeFromTheFlagsFileBeingEdited()
    {
        OverlayDataSource source = new(_disk, new Dictionary<string, byte[]>
        {
            [FighterFlagsSchema.File] = Encoding.UTF8.GetBytes("""
                { "schemaVersion": 1, "flags": [{ "name": "drive", "clearedWhenHit": false }] }
                """),
        });

        Assert.Equal(["drive"], EditingContext.Read(source).MoveSchema.Field("setsFlags")!.Choices);
    }

    [Fact]
    public void AnUnreadableFlagsFileOffersNoNames()
    {
        OverlayDataSource source = new(_disk, new Dictionary<string, byte[]>(), [FighterFlagsSchema.File]);

        SchemaField field = EditingContext.Read(source).MoveSchema.Field("setsFlags")!;

        Assert.Equal((SchemaFieldKind.ChoiceList, 0), (field.Kind, field.Choices.Count));
    }

    [Fact]
    public void AFlagListTheSchemaAlreadyEnumeratesKeepsItsChoices()
    {
        SchemaObject schema = DataSchema.Parse("""
            { "properties": { "setsFlags": { "type": "array", "items": { "enum": ["a"] } }, "other": { "type": "array" } } }
            """).Root;

        SchemaObject offered = MoveFlagFields.WithFlagChoices(schema, ["stance"]);

        Assert.Equal(["a"], offered.Field("setsFlags")!.Choices);
        Assert.Equal(SchemaFieldKind.Other, offered.Field("other")!.Kind);
    }

    [Fact]
    public void CostAndFollowUpsAreSchemaFormFields()
    {
        SchemaObject cost = _moveSchema.ObjectOf("cost");

        Assert.Equal([SchemaFieldKind.Integer, SchemaFieldKind.Integer], cost.Fields.Select(field => field.Kind));
        Assert.Equal(SchemaFieldKind.Boolean, _moveSchema.Field("followUpOnly")!.Kind);
        Assert.Equal(SchemaFieldKind.Boolean, _moveSchema.Field("cancels")!.Items!.Field("followUp")!.Kind);
    }

    [Fact]
    public void AnAddedCostStartsAtTheSchemasSmallestValues()
    {
        JsonNode cost = SchemaDefaults.ValueFor(_moveSchema.Field("cost")!)!;

        Assert.Equal("""{"channel":0,"amount":1}""", cost.ToJsonString());
    }

    [Fact]
    public void TickedFlagsAndAddedFieldsLandInSchemaOrder()
    {
        JsonObject move = JsonNode.Parse("""
            { "id": "a", "category": "special", "usableIn": "ground", "input": {}, "boxes": [] }
            """)!.AsObject();

        JsonStringList.Set(move, "setsFlags", "stance", included: true, keepWhenEmpty: false, _moveSchema.FieldNames);
        JsonFieldOrder.Set(move, "followUpOnly", true, _moveSchema.FieldNames);
        JsonFieldOrder.Set(move, "requires", SchemaDefaults.ValueFor(_moveSchema.Field("requires")!),
            _moveSchema.FieldNames);
        JsonStringList.Set(move["requires"]!.AsObject(), "notFlags", "install", included: true, keepWhenEmpty: false,
            _moveSchema.ObjectOf("requires").FieldNames);

        Assert.Equal(
            ["id", "category", "usableIn", "requires", "setsFlags", "followUpOnly", "input", "boxes"],
            move.Select(entry => entry.Key));
        Assert.Equal("""{"notFlags":["install"]}""", move["requires"]!.ToJsonString());
    }

    [Fact]
    public void UntickingTheLastFlagRemovesTheList()
    {
        JsonObject move = new() { ["setsFlags"] = new JsonArray("stance") };

        JsonStringList.Set(move, "setsFlags", "stance", included: false, keepWhenEmpty: false, _moveSchema.FieldNames);

        Assert.False(move.ContainsKey("setsFlags"));
    }

    [Fact]
    public void AFlagNoLongerInTheFileIsStillOfferedSoItCanBeUnticked()
    {
        JsonObject move = new() { ["setsFlags"] = new JsonArray("install", "removed") };

        Assert.Equal(["stance", "install", "removed"], JsonStringList.Offered(["stance", "install"], move, "setsFlags"));
    }
}
