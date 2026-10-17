using System;
using System.Collections.Generic;
using DataMigrate;
using DataMigrate.Migrations;

// Usage: dotnet run --project tools/DataMigrate -- [--check] <data root>...
// --check only lists files that need migrating and exits 1 if there are any (for CI).
// --format <file>... rewrites the given files in the shared hand-written layout (e.g. files an agent generated).
if (args.Length > 0 && args[0] == "--format")
{
    foreach (string file in args[1..])
    {
        DataFileFormatter.Format(file);
        Console.WriteLine($"formatted: {file}");
    }

    return 0;
}

bool check = Array.IndexOf(args, "--check") >= 0;
List<string> roots = [.. Array.FindAll(args, arg => arg != "--check")];
if (roots.Count == 0)
{
    Console.Error.WriteLine("Usage: DataMigrate [--check] <data root>...");
    return 2;
}

var migrator = new DataMigrator(MigrationList.All);
int total = 0;
foreach (string root in roots)
{
    foreach (string path in migrator.Run(root, write: !check))
    {
        Console.WriteLine($"{(check ? "needs migrating" : "migrated")}: {root}/{path}");
        total++;
    }
}

Console.WriteLine($"{total} file(s) {(check ? "need migrating" : "migrated")}.");
return check && total > 0 ? 1 : 0;
