using System.IO;

namespace FightingGame.Input;

/// <summary>The built-in controller layouts (<c>default_bindings.jsonc</c>), embedded in the game assembly.</summary>
public static class DefaultBindings
{
    private const string Resource = "FightingGame.Input.default_bindings.jsonc";

    public static string ReadJson()
    {
        using Stream stream = typeof(DefaultBindings).Assembly.GetManifestResourceStream(Resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
