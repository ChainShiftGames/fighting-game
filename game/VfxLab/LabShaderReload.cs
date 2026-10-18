using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FightingGame.VfxLab;

/// <summary>Reloads changed shaders in place, so materials using them pick up the new code without a restart.</summary>
public static class LabShaderReload
{
    /// <param name="changed">The files that changed.</param>
    /// <param name="all">Every watched shader: a changed include reloads them all, since any of them may include it.</param>
    public static void Reload(IReadOnlyList<string> changed, IEnumerable<string> all)
    {
        List<string> includes = changed.Where(path => path.EndsWith(".gdshaderinc", StringComparison.Ordinal)).ToList();
        IEnumerable<string> shaders = includes.Count > 0
            ? all.Where(path => path.EndsWith(".gdshader", StringComparison.Ordinal))
            : changed.Where(path => path.EndsWith(".gdshader", StringComparison.Ordinal));
        foreach (string path in includes.Concat(shaders))
        {
            ResourceLoader.Load(path, string.Empty, ResourceLoader.CacheMode.Replace);
        }
    }
}
