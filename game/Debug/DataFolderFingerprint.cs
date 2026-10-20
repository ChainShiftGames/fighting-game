using System;
using System.IO;

namespace FightingGame.Debug;

/// <summary>
/// A cheap summary of the data folder's JSON files (paths, sizes, last write times): it changes when a file is
/// saved, added or deleted, without reading any file's contents.
/// </summary>
public static class DataFolderFingerprint
{
    /// <summary>The folder's fingerprint; only compare it with others taken in the same run.</summary>
    public static int Of(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.Ordinal);
        HashCode hash = new();
        foreach (string file in files)
        {
            hash.Add(file, StringComparer.Ordinal);
            var info = new FileInfo(file);
            // Deleted since it was listed (a save in progress): the next check sees the folder as it settles.
            if (info.Exists)
            {
                hash.Add(info.Length);
                hash.Add(info.LastWriteTimeUtc.Ticks);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>The same summary for one file outside the folder; it changes when the file is saved or deleted.</summary>
    public static int OfFile(string path)
    {
        var info = new FileInfo(path);
        return info.Exists ? HashCode.Combine(info.Length, info.LastWriteTimeUtc.Ticks) : 0;
    }
}
