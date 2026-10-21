using System;
using System.IO;
using FightingGame.Presentation.World;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A character's art file open in the editor for its animation timing (developer setting, ADR-0028): the text, which
/// the animation track rewrites one move at a time, and whether it changed since it was opened or saved. It lives in
/// <c>game/Assets/Characters</c>, outside <c>data/</c>, so saving it never changes the data hash.
/// </summary>
public sealed class ArtTimingFile
{
    private readonly string _path;
    private string _text;

    private ArtTimingFile(string path, string text)
    {
        _path = path;
        _text = text;
        Problem = Check(text);
    }

    public bool IsChanged { get; private set; }

    /// <summary>Why the file can't be read (fix it by hand first); empty when it can.</summary>
    public string Problem { get; }

    /// <param name="path">The art file's full path on disk.</param>
    /// <returns>Null when the character has no art file (it is drawn as a placeholder).</returns>
    public static ArtTimingFile? Open(string path) => File.Exists(path) ? new ArtTimingFile(path, File.ReadAllText(path)) : null;

    /// <summary>The file as text, for tests and for the file it opened from.</summary>
    public static ArtTimingFile FromText(string path, string text) => new(path, text);

    public string Text => _text;

    /// <summary>The move's contact frames and keys now; none when the file can't be read.</summary>
    public MoveAnimationTiming Timing(string moveId)
    {
        FighterArtSettings? settings = FighterArtFile.Read(_text, out _);
        return settings is null ? MoveAnimationTiming.None : new MoveAnimationTiming(settings.ContactsFor(moveId), settings.KeysFor(moveId));
    }

    /// <summary>Writes the move's timing into the text; the file on disk changes on <see cref="Save"/>.</summary>
    /// <returns>Why nothing changed (the file can't be read, or wouldn't be with this timing); empty when it did.</returns>
    public string SetTiming(string moveId, MoveAnimationTiming timing)
    {
        if (Problem.Length > 0)
        {
            return Problem;
        }

        string text;
        try
        {
            text = ArtTimingText.Write(_text, moveId, timing);
        }
        catch (FormatException exception)
        {
            return exception.Message;
        }

        if (Check(text) is { Length: > 0 } problem)
        {
            return problem;
        }

        IsChanged |= text != _text;
        _text = text;
        return string.Empty;
    }

    /// <summary>Writes the file when it changed.</summary>
    public void Save()
    {
        if (IsChanged)
        {
            File.WriteAllText(_path, _text);
            IsChanged = false;
        }
    }

    private static string Check(string text)
    {
        FighterArtFile.Read(text, out string error);
        return error;
    }
}
