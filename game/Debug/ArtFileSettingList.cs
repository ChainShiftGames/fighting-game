using System;
using System.Collections.Generic;
using System.IO;

namespace FightingGame.Debug;

/// <summary>
/// The developer settings in characters' art files (<c>game/Assets/Characters</c>, ADR-0028) that the debug panel
/// lists: one row per art file that holds the setting.
/// </summary>
public static class ArtFileSettingList
{
    private const string CameraTurnKey = "cameraTurnDegrees";
    private const int MaxCameraTurnDegrees = 90;
    private const int CameraTurnArrowStep = 5;

    /// <param name="charactersRoot">The folder with one folder per character, each holding <c>&lt;id&gt;_art.json</c>.</param>
    public static DataFileSetting[] CameraTurns(string charactersRoot)
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
            var setting = new DataFileSetting(
                $"{id}: turn toward camera (°)", $"{id}/{id}_art.json", CameraTurnKey, 0, MaxCameraTurnDegrees, CameraTurnArrowStep);
            if (Holds(setting, charactersRoot))
            {
                settings.Add(setting);
            }
        }

        return [.. settings];
    }

    private static bool Holds(DataFileSetting setting, string root)
    {
        if (!File.Exists(DataFilePath.In(root, setting.File)))
        {
            return false;
        }

        try
        {
            setting.Read(root);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
