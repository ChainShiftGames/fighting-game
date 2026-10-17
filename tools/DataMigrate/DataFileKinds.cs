using System;

namespace DataMigrate;

/// <summary>Tells a data file's kind from its path under the data root ('/' separated).</summary>
public static class DataFileKinds
{
    public static DataFileKind Of(string relativePath)
    {
        string[] parts = relativePath.Split('/');
        if (parts.Length == 0 || !relativePath.EndsWith(".json", StringComparison.Ordinal))
        {
            return DataFileKind.Other;
        }

        return parts[0] switch
        {
            "characters" when parts.Length == 4 && parts[2] == "moves" => DataFileKind.Move,
            "characters" when parts.Length == 3 && parts[2] == "character.json" => DataFileKind.Character,
            "rules" => DataFileKind.Rules,
            "input" => DataFileKind.Input,
            _ => DataFileKind.Other,
        };
    }
}
