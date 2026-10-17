using System;
using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Whether an id can name a new move file. Only what keeps files safe is checked here; Sim.Data's validation
/// reports every other id rule (lowercase, length) like any other problem.
/// </summary>
public static class MoveIdCheck
{
    /// <summary>The id can be used as a file name: ASCII letters, digits and '_', so "../x" never becomes a path.</summary>
    public static bool IsFileName(string id)
    {
        if (id.Length == 0)
        {
            return false;
        }

        foreach (char c in id)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Why <paramref name="id"/> can't name a new move; null when it can.</summary>
    /// <param name="moveIds">The character's move list.</param>
    public static string? Problem(string id, IReadOnlyList<string> moveIds)
    {
        if (!IsFileName(id))
        {
            return "Use letters, digits and _ only.";
        }

        foreach (string existing in moveIds)
        {
            // Windows and macOS file names ignore case, so "5A" would overwrite 5a.json there.
            if (string.Equals(existing, id, StringComparison.OrdinalIgnoreCase))
            {
                return existing == id ? $"'{id}' already exists." : $"'{existing}' differs only in case.";
            }
        }

        return null;
    }
}
