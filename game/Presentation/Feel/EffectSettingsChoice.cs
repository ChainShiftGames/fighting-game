using System.IO;
using Godot;

namespace FightingGame.Presentation.Feel;

/// <summary>Loads the effect settings for a match from the project's <see cref="EffectSettingsFile"/>.</summary>
public static class EffectSettingsChoice
{
    /// <summary>Reads the file again, so a change from the control panel shows up in the next match.</summary>
    /// <exception cref="InvalidDataException">The file is missing or invalid; the match doesn't start.</exception>
    public static EffectSettings Load()
    {
        string path = EffectSettingsFile.ResPath;
        if (!Godot.FileAccess.FileExists(path))
        {
            throw new InvalidDataException($"{path} is missing.");
        }

        return EffectSettingsFile.Read(Godot.FileAccess.GetFileAsString(path), out string error)
            ?? throw new InvalidDataException($"{path}: {error}");
    }
}
