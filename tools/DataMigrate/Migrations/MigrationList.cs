using System.Collections.Generic;

namespace DataMigrate.Migrations;

/// <summary>Every migration step, in the order they were added; one line per format change (ADR-0021).</summary>
public static class MigrationList
{
    public static IReadOnlyList<IDataMigration> All { get; } =
    [
        new MoveHitsAsList(),
    ];
}
