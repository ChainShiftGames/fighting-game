namespace Sim.Data.Modules;

/// <summary>
/// Paths and version of the module lineup: <c>rules/modules.json</c> (hook order) and one
/// <c>rules/modules/&lt;name&gt;.json</c> per module (on/off and config); schemas in <c>data/schema/</c>.
/// </summary>
public static class ModuleLineupSchema
{
    public const int Version = 1;

    public const string OrderFile = "rules/modules.json";

    public const string ModulesDirectory = "rules/modules";

    /// <summary>The key in a module file that switches the module on or off; no config key may use it.</summary>
    public const string EnabledKey = "enabled";

    private const string JsonExtension = ".json";

    public static string ModuleFile(string name) => $"{ModulesDirectory}/{name}{JsonExtension}";

    /// <summary>The module name a file in <see cref="ModulesDirectory"/> belongs to, or null for a non-JSON file.</summary>
    public static string? ModuleName(string fileName) =>
        fileName.EndsWith(JsonExtension, System.StringComparison.Ordinal) ? fileName[..^JsonExtension.Length] : null;
}
