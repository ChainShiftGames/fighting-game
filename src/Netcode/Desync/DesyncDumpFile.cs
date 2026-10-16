using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Netcode.Desync;

/// <summary>Writes desync dumps as JSON files and reads them back (for <c>tools/DesyncDiff</c>).</summary>
public static class DesyncDumpFile
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Writes <paramref name="dump"/> into <paramref name="folder"/> and returns the file's path.</summary>
    /// <remarks>Never overwrites an earlier dump: each desync keeps its own file.</remarks>
    public static string Write(string folder, DesyncDump dump)
    {
        Directory.CreateDirectory(folder);
        string name = string.Create(
            CultureInfo.InvariantCulture,
            $"desync-{dump.WrittenAt.UtcDateTime:yyyyMMdd-HHmmss}-frame{dump.FirstMismatchFrame}-p{dump.LocalPlayer}");
        string path = Path.Combine(folder, name + ".json");
        for (int copy = 2; File.Exists(path); copy++)
        {
            path = Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"{name}-{copy}.json"));
        }

        using FileStream file = new(path, FileMode.CreateNew, FileAccess.Write);
        JsonSerializer.Serialize(file, dump, _options);
        return path;
    }

    /// <exception cref="InvalidDataException">The file is not a dump this version can read.</exception>
    public static DesyncDump Read(string path)
    {
        using FileStream file = File.OpenRead(path);
        DesyncDump dump;
        try
        {
            dump = JsonSerializer.Deserialize<DesyncDump>(file, _options)
                ?? throw new InvalidDataException($"{path} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{path} is not a desync dump: {exception.Message}", exception);
        }

        if (dump.FormatVersion != DesyncDump.CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"{path} is dump format {dump.FormatVersion}; this build reads format {DesyncDump.CurrentFormatVersion}.");
        }

        return dump;
    }
}
