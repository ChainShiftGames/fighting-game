using System.Text.Json.Nodes;

namespace DataMigrate.Migrations;

/// <summary>Move schema 1 to 2 (ADR-0022): the single <c>hit</c> object becomes a one-item <c>hits</c> list.</summary>
/// <remarks>The list takes the old field's place, so the file's field order stays as it was.</remarks>
public sealed class MoveHitsAsList : IDataMigration
{
    public DataFileKind Kind => DataFileKind.Move;

    public int FromVersion => 1;

    public void Apply(JsonObject file, string path)
    {
        int index = file.IndexOf("hit");
        if (index < 0)
        {
            return;
        }

        JsonNode? hit = file["hit"];
        file.RemoveAt(index);
        file.Insert(index, "hits", new JsonArray(hit));
    }
}
