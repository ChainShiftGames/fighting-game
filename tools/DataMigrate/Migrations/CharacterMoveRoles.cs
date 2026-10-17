using System.Text.Json.Nodes;

namespace DataMigrate.Migrations;

/// <summary>Character schema 1 to 2 (ADR-0023): adds the required <c>moveRoles</c> object, empty, after <c>states</c>.</summary>
/// <remarks>Which move fills a role (e.g. <c>burst</c>) is a design choice, so the migration names none.</remarks>
public sealed class CharacterMoveRoles : IDataMigration
{
    public DataFileKind Kind => DataFileKind.Character;

    public int FromVersion => 1;

    public void Apply(JsonObject file, string path)
    {
        if (file.ContainsKey("moveRoles"))
        {
            return;
        }

        int states = file.IndexOf("states");
        file.Insert(states < 0 ? file.Count : states + 1, "moveRoles", new JsonObject());
    }
}
