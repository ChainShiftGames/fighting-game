using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Sparks;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A character's art file open in the editor for what the drawer edits in it (developer settings): animation timing
/// (ADR-0028) and hit sparks. The text is rewritten one move at a time, and it tracks whether it changed since it was
/// opened or saved. It lives in
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
    public string SetTiming(string moveId, MoveAnimationTiming timing) =>
        Rewrite(() => ArtTimingText.Write(_text, moveId, timing));

    /// <summary>The move's spark settings, one per hit the file lists; none when it lists none or can't be read.</summary>
    public IReadOnlyList<HitSparkSettings> Sparks(string moveId) =>
        FighterArtFile.Read(_text, out _) is { } settings && settings.Sparks.TryGetValue(moveId, out IReadOnlyList<HitSparkSettings>? sparks)
            ? sparks
            : [];

    /// <summary>Writes the move's spark settings (one per hit) into the text, like <see cref="SetTiming"/>.</summary>
    public string SetSparks(string moveId, IReadOnlyList<HitSparkSettings> sparks) =>
        Rewrite(() => ArtSparkText.Write(_text, moveId, sparks));

    /// <summary>Writes the file when it changed.</summary>
    public void Save()
    {
        if (IsChanged)
        {
            File.WriteAllText(_path, _text);
            IsChanged = false;
        }
    }

    private string Rewrite(Func<string> write)
    {
        if (Problem.Length > 0)
        {
            return Problem;
        }

        string text;
        try
        {
            text = write();
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

    private static string Check(string text)
    {
        FighterArtFile.Read(text, out string error);
        return error;
    }
}
