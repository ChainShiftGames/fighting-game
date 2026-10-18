using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FightingGame.VfxLab;

/// <summary>
/// Notices when a sheet or an effect shader is saved from outside (an agent, an editor), so the Lab reloads it live
/// (ADR-0029 §7). Polls modified times; the Lab calls it twice a second.
/// </summary>
public sealed class LabFileWatch
{
    private static readonly string[] _extensions = [".gdshader", ".gdshaderinc"];

    private readonly string[] _folders;
    private readonly Dictionary<string, ulong> _seen = new(StringComparer.Ordinal);

    public LabFileWatch(params string[] folders)
    {
        _folders = folders;

        // The first look only learns the times.
        Changed();
    }

    /// <summary>The watched files that changed (or appeared) since the last call.</summary>
    public List<string> Changed()
    {
        List<string> changed = [];
        foreach (string path in Watched())
        {
            ulong time = FileAccess.GetModifiedTime(path);
            if (!_seen.TryGetValue(path, out ulong before) || before != time)
            {
                changed.Add(path);
            }

            _seen[path] = time;
        }

        return changed;
    }

    /// <summary>Every shader and include in the watched folders.</summary>
    public IEnumerable<string> Shaders() => Watched().Where(path => !path.EndsWith(".json", StringComparison.Ordinal));

    private IEnumerable<string> Watched()
    {
        foreach (string folder in _folders)
        {
            if (FileAccess.FileExists($"{folder}/sheet.json"))
            {
                yield return $"{folder}/sheet.json";
            }

            foreach (string file in DirAccess.GetFilesAt(folder).Where(file => _extensions.Any(ext => file.EndsWith(ext, StringComparison.Ordinal))))
            {
                yield return $"{folder}/{file}";
            }
        }
    }
}
