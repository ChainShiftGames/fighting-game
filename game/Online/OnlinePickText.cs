using System;
using System.Globalization;
using System.IO;

namespace FightingGame.Online;

/// <summary>
/// A <see cref="FighterPick"/> as the text the lobby passes between the two PCs (ADR-0042): <c>colour/character id</c>,
/// e.g. <c>2/testman</c>. The stage joins as another field once there is stage select (UI-06): the host's is played.
/// </summary>
public static class OnlinePickText
{
    private const char Separator = '/';

    /// <summary>The text for a pick; a pick without a character can't go online (the default lineup has no id here).</summary>
    public static string Write(FighterPick pick)
    {
        ArgumentException.ThrowIfNullOrEmpty(pick.CharacterId);
        return string.Create(CultureInfo.InvariantCulture, $"{pick.Colour}{Separator}{pick.CharacterId}");
    }

    /// <summary>Reads the other PC's pick; null when the text isn't one (an older or newer build).</summary>
    public static FighterPick? TryRead(string text)
    {
        int separator = text.IndexOf(Separator, StringComparison.Ordinal);
        if (separator <= 0 || separator == text.Length - 1 ||
            !byte.TryParse(text.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out byte colour))
        {
            return null;
        }

        return new FighterPick(text[(separator + 1)..], colour);
    }

    /// <summary>Reads a pick for the match start; an empty text is no pick (the default lineup's character).</summary>
    /// <exception cref="InvalidDataException">The text isn't a pick.</exception>
    public static FighterPick Read(string text)
    {
        if (text.Length == 0)
        {
            return FighterPick.Default;
        }

        return TryRead(text) ?? throw new InvalidDataException(
            $"The other player's character pick '{text}' can't be read: the two games probably differ.");
    }
}
