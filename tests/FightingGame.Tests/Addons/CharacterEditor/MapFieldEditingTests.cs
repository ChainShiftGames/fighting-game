using System.Linq;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Keys a schema allows without listing, such as move roles and a mechanic's config (ADR-0023).</summary>
public sealed class MapFieldEditingTests
{
    private static readonly SchemaObject _characterSchema =
        DataSchema.Read(new DirectoryDataSource(TestGameData.DataRoot), "character")!.Root;

    private static readonly SchemaField _moveRoles = _characterSchema.Field("moveRoles")!;

    [Fact]
    public void TheCharacterSchemaReadsMoveRolesAsAMapOfIds()
    {
        Assert.Equal(SchemaFieldKind.Map, _moveRoles.Kind);
        Assert.True(_moveRoles.IsRequired);
        Assert.Equal(SchemaFieldKind.Text, _moveRoles.Value!.Kind);
    }

    [Fact]
    public void AnObjectWithFixedFieldsIsNoMap()
    {
        DataSchema schema = DataSchema.Parse("""
            {
              "properties": {
                "stats": { "type": "object", "additionalProperties": false, "properties": { "health": { "type": "integer" } } },
                "free": { "type": "object", "additionalProperties": { "type": "integer", "minimum": 1 } }
              }
            }
            """);

        Assert.Equal(SchemaFieldKind.Object, schema.Root.Field("stats")!.Kind);
        SchemaField free = schema.Root.Field("free")!;
        Assert.Equal((SchemaFieldKind.Map, SchemaFieldKind.Integer, 1L), (free.Kind, free.Value!.Kind, free.Value.Minimum));
    }

    [Fact]
    public void AMechanicHasAFixedNameAndIntegerKeysForItsConfig()
    {
        SchemaObject mechanic = _characterSchema.ObjectOf("mechanic");

        Assert.Equal(["name"], mechanic.FieldNames);
        Assert.Equal(SchemaFieldKind.Integer, mechanic.Extra!.Kind);
    }

    [Fact]
    public void AnObjectAllowingNoOtherKeysHasNoExtra()
    {
        Assert.Null(_characterSchema.ObjectOf("stats").Extra);
    }

    [Fact]
    public void AnAddedEntryGoesLastWithTheValuesDefault()
    {
        JsonObject roles = new() { ["burst"] = "burst" };

        JsonMap.Add(roles, "install", _moveRoles.Value!);

        Assert.Equal(["burst", "install"], roles.Select(entry => entry.Key));
        Assert.Equal("", roles["install"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("burst", false)]
    [InlineData("install", true)]
    public void AKeyCanBeAddedOnlyWhenNewAndNotEmpty(string key, bool allowed)
    {
        JsonObject roles = new() { ["burst"] = "burst" };

        Assert.Equal(allowed, JsonMap.AddProblem(roles, key) is null);
    }

    [Fact]
    public void ANewMapStartsEmpty()
    {
        Assert.Equal("{}", SchemaDefaults.ValueFor(_moveRoles)!.ToJsonString());
    }
}
