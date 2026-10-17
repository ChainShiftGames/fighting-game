using System.Text.Json.Nodes;

namespace DataMigrate.Migrations;

/// <summary>Move schema 2 to 3 (ADR-0023): adds optional conditions, flags, cost and follow-ups; nothing to rewrite.</summary>
/// <remarks>Only the version changes, so a reader of version 2 refuses the new fields instead of misreading them.</remarks>
public sealed class MoveConditions : IDataMigration
{
    public DataFileKind Kind => DataFileKind.Move;

    public int FromVersion => 2;

    public void Apply(JsonObject file, string path)
    {
    }
}
