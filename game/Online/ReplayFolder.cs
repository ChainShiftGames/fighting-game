using System;
using System.Globalization;
using System.IO;
using Netcode.Replays;
using Sim.Data;

namespace FightingGame.Online;

/// <summary>Where this PC keeps its replays: one file per match, named after when it was saved.</summary>
/// <remarks>
/// Every saved match is kept; when to delete old ones, and a menu to pick one, are undecided. No engine calls, so the
/// host passes the folder as a plain path (it uses <c>user://replays</c>).
/// </remarks>
public sealed class ReplayFolder
{
    /// <summary>The file extension of replay files.</summary>
    public const string Extension = ".fgreplay";

    // Two matches saved within one second get a numbered name rather than overwriting each other.
    private const int MaxNumberedNames = 100;

    private readonly string _path;

    /// <param name="path">The folder's full path; created when the first replay is saved.</param>
    public ReplayFolder(string path)
    {
        _path = path;
    }

    /// <summary>The file name for a replay saved at <paramref name="savedAt"/>, e.g. <c>2026-10-17_14-03-22_local.fgreplay</c>.</summary>
    /// <param name="kind">What kind of match it was, e.g. "local" or "online-p2"; part of the name.</param>
    /// <param name="number">More than 1 when another replay was saved within the same second.</param>
    public static string FileName(DateTime savedAt, string kind, int number = 1)
    {
        string time = savedAt.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string suffix = number > 1 ? string.Create(CultureInfo.InvariantCulture, $"_{number}") : string.Empty;
        return $"{time}_{kind}{suffix}{Extension}";
    }

    /// <summary>Loads a replay file this build can play back.</summary>
    /// <exception cref="ReplayRejectedException">It was recorded by another build or other data, or is damaged.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    public static Replay Load(string path, GameData data)
    {
        using FileStream stream = File.OpenRead(path);
        return ReplayReader.Read(stream, GameVersion.Current, data.Hash.ToBytes());
    }

    /// <summary>Saves <paramref name="replay"/> under a new name; never overwrites a file.</summary>
    /// <returns>The saved file's full path.</returns>
    /// <exception cref="IOException">The folder or file can't be written.</exception>
    public string Save(Replay replay, DateTime savedAt, string kind)
    {
        Directory.CreateDirectory(_path);
        for (int number = 1; number <= MaxNumberedNames; number++)
        {
            string file = Path.Combine(_path, FileName(savedAt, kind, number));
            if (File.Exists(file))
            {
                continue;
            }

            using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write);
            ReplayWriter.Write(stream, replay);
            return file;
        }

        throw new IOException($"Too many replays named {FileName(savedAt, kind)} in {_path}.");
    }
}
