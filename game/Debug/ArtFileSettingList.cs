using System;
using System.Collections.Generic;
using System.IO;

namespace FightingGame.Debug;

/// <summary>
/// The developer settings in characters' art files (<c>game/Assets/Characters</c>, ADR-0028) that the debug panel
/// lists: per art file, the rows for the settings it holds, so hit-feel looks can be tuned while playtesting.
/// </summary>
public static class ArtFileSettingList
{
    private const string CameraTurnKey = "cameraTurnDegrees";
    private const int MaxCameraTurnDegrees = 90;
    private const int CameraTurnArrowStep = 5;

    private const string ReHitFadeKey = "reHitFadeFrames";
    private const int MaxReHitFadeFrames = 30;

    private const string ClipFromKey = "clipFrom";
    private const string ClipToKey = "clipTo";
    private const int MaxClipFrame = 600;
    private const int ClipFromArrowStep = 5;

    // The hit and block reactions' move IDs, as the characters' files name them; the rows are for the ones a file
    // animates.
    private static readonly string[] _reactionMoves =
        ["hitstun", "crouch_hitstun", "air_hitstun", "blockstun", "crouch_blockstun", "air_blockstun"];

    /// <param name="charactersRoot">The folder with one folder per character, each holding <c>&lt;id&gt;_art.json</c>.</param>
    public static DataFileSetting[] LookSettings(string charactersRoot)
    {
        var settings = new List<DataFileSetting>();
        if (!Directory.Exists(charactersRoot))
        {
            return [];
        }

        string[] folders = Directory.GetDirectories(charactersRoot);
        Array.Sort(folders, StringComparer.Ordinal);
        foreach (string folder in folders)
        {
            string id = Path.GetFileName(folder);
            string file = $"{id}/{id}_art.json";
            AddIfHeld(settings, charactersRoot, new DataFileSetting(
                $"{id}: turn toward camera (°)", file, CameraTurnKey, 0, MaxCameraTurnDegrees, CameraTurnArrowStep));
            List<DataFileSetting> reactions = ReactionClipStarts(id, file, charactersRoot);
            if (reactions.Count > 0)
            {
                settings.Add(new DataFileSetting(
                    $"{id}: re-hit fade (frames)", file, ReHitFadeKey, 0, MaxReHitFadeFrames, 1)
                { WhenMissing = 0 });
                settings.AddRange(reactions);
            }
        }

        return [.. settings];
    }

    // A reaction's clip start sets the pose a hit lands on and hitstop holds; it must stay before the clip's end.
    private static List<DataFileSetting> ReactionClipStarts(string id, string file, string root)
    {
        var settings = new List<DataFileSetting>();
        foreach (string move in _reactionMoves)
        {
            string entry = $"animations.{move}";
            var probe = new DataFileSetting(string.Empty, file, $"{entry}.{ClipToKey}", 0, MaxClipFrame, 1)
            {
                WhenMissing = MaxClipFrame + 1,
            };
            if (TryRead(probe, root) is not { } clipTo)
            {
                continue;
            }

            settings.Add(new DataFileSetting(
                $"{id}: {move} starts at clip frame", file, $"{entry}.{ClipFromKey}", 0, clipTo - 1, ClipFromArrowStep)
            {
                WhenMissing = 0,
            });
        }

        return settings;
    }

    private static void AddIfHeld(List<DataFileSetting> settings, string root, DataFileSetting setting)
    {
        if (TryRead(setting, root) is not null)
        {
            settings.Add(setting);
        }
    }

    private static int? TryRead(DataFileSetting setting, string root)
    {
        if (!File.Exists(DataFilePath.In(root, setting.File)))
        {
            return null;
        }

        try
        {
            return setting.Read(root);
        }
        catch (Exception exception) when (exception is FormatException or System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
