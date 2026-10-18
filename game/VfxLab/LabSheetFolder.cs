using System.Collections.Generic;
using System.Linq;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab;

/// <summary>The Lab's sheets on disk: one folder each under <see cref="Root"/>, with <c>sheet.json</c> and <c>feedback.json</c>.</summary>
public static class LabSheetFolder
{
    public const string Root = "res://VfxLab/Sheets";

    public static string FolderOf(string sheetId) => $"{Root}/{sheetId}";

    /// <summary>The ids of the sheets there are, sorted.</summary>
    public static List<string> List() => DirAccess.GetDirectoriesAt(Root)
        .Where(id => FileAccess.FileExists($"{FolderOf(id)}/sheet.json"))
        .Order(System.StringComparer.Ordinal)
        .ToList();

    /// <param name="error">Why the sheet or its feedback couldn't be read; empty when both were.</param>
    public static LabSession? Open(string sheetId, out string error)
    {
        string folder = FolderOf(sheetId);
        LabSheet? sheet = LabSheetFile.Read(FileAccess.GetFileAsString($"{folder}/sheet.json"), folder, out error);
        if (sheet is null)
        {
            error = $"{folder}/sheet.json: {error}";
            return null;
        }

        LabFeedback feedback = new();
        string feedbackPath = $"{folder}/feedback.json";
        if (FileAccess.FileExists(feedbackPath))
        {
            feedback = LabFeedbackFile.Read(FileAccess.GetFileAsString(feedbackPath), out error) ?? feedback;
            error = error.Length > 0 ? $"{feedbackPath}: {error}; starting with no feedback" : string.Empty;
        }

        return new LabSession(sheet, feedback);
    }

    public static void Save(string sheetId, LabFeedback feedback) =>
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"{FolderOf(sheetId)}/feedback.json"), LabFeedbackFile.Write(feedback));
}
