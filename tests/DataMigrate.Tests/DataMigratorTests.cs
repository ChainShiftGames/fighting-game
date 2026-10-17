using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DataMigrate;
using DataMigrate.Migrations;
using Xunit;

namespace DataMigrate.Tests;

public sealed class DataMigratorTests
{
    private const string MoveV1 = """
        {
          "schemaVersion": 1,
          "id": "5a",
          "category": "normal",
          "hit": { "damage": 300, "guard": "mid" },
          "projectiles": []
        }
        """;

    private readonly DataMigrator _migrator = new(MigrationList.All);

    [Theory]
    [InlineData("data")]
    [InlineData("tests/Sim.Data.Tests/Fixtures/data")]
    [InlineData("tests/Scenarios/Fixtures/data")]
    public void EveryRepositoryDataRootIsCurrent(string root)
    {
        Assert.Empty(_migrator.Run(Path.Combine(RepositoryRoot, root), write: false));
    }

    [Fact]
    public void AMoveHitBecomesAOneItemHitsListInItsPlace()
    {
        string migrated = _migrator.Migrate("characters/man/moves/5a.json", Encoding.UTF8.GetBytes(MoveV1))!;

        Assert.Contains("\"schemaVersion\": 2", migrated, StringComparison.Ordinal);
        Assert.Contains("""
              "hits": [
                { "damage": 300, "guard": "mid" }
              ],
              "projectiles": []
            """.Replace("\r\n", "\n", StringComparison.Ordinal), migrated, StringComparison.Ordinal);
        Assert.DoesNotContain("\"hit\":", migrated, StringComparison.Ordinal);
    }

    [Fact]
    public void ACurrentFileIsLeftAlone()
    {
        string current = MoveV1.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal);

        Assert.Null(_migrator.Migrate("characters/man/moves/5a.json", Encoding.UTF8.GetBytes(current)));
    }

    [Theory]
    [InlineData("input/buttons.json")]
    [InlineData("rules/match.json")]
    [InlineData("schema/move.schema.json")]
    public void OnlyFilesOfTheMigratedKindChange(string path)
    {
        Assert.Null(_migrator.Migrate(path, Encoding.UTF8.GetBytes(MoveV1)));
    }

    [Fact]
    public void ACharacterGetsAnEmptyMoveRolesObjectAfterItsStates()
    {
        const string CharacterV1 = """
            {
              "schemaVersion": 1,
              "id": "man",
              "states": { "idle": "idle" },
              "moves": ["idle"]
            }
            """;

        string migrated = _migrator.Migrate("characters/man/character.json", Encoding.UTF8.GetBytes(CharacterV1))!;

        Assert.Contains("\"schemaVersion\": 2", migrated, StringComparison.Ordinal);
        Assert.True(
            migrated.IndexOf("\"states\"", StringComparison.Ordinal) <
            migrated.IndexOf("\"moveRoles\": {}", StringComparison.Ordinal));
        Assert.True(
            migrated.IndexOf("\"moveRoles\"", StringComparison.Ordinal) <
            migrated.IndexOf("\"moves\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("characters/man/moves/5a.json", DataFileKind.Move)]
    [InlineData("characters/man/character.json", DataFileKind.Character)]
    [InlineData("rules/modules/burst.json", DataFileKind.Rules)]
    [InlineData("input/motions.json", DataFileKind.Input)]
    [InlineData("schema/move.schema.json", DataFileKind.Other)]
    public void TellsFileKindsApartByPath(string path, DataFileKind kind)
    {
        Assert.Equal(kind, DataFileKinds.Of(path));
    }

    private static string RepositoryRoot => typeof(DataMigratorTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RepositoryRoot").Value!;
}
