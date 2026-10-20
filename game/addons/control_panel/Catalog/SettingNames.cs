using System;
using System.Text;
using FightingGame.Addons.ControlPanel.Documents;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>Readable labels and units for settings named only by a data key or ID.</summary>
public static class SettingNames
{
    /// <summary><c>roman_cancel</c> → "Roman cancel".</summary>
    public static string FromId(string id) => Capitalised(id.Replace('_', ' '));

    /// <summary><c>unblockableAttacks</c> → "Unblockable attacks".</summary>
    public static string FromKey(string key)
    {
        var words = new StringBuilder(key.Length + 4);
        foreach (char letter in key)
        {
            if (char.IsUpper(letter) && words.Length > 0)
            {
                words.Append(' ');
            }

            words.Append(char.ToLowerInvariant(letter));
        }

        return Capitalised(words.ToString());
    }

    /// <summary>The unit a key's name implies: <c>…Frames</c>, <c>…Window</c> or <c>…Permille</c>.</summary>
    public static SettingUnit UnitOfKey(string key) =>
        key.EndsWith("Frames", StringComparison.Ordinal) || key.EndsWith("Window", StringComparison.Ordinal)
            ? SettingUnit.Frames
            : key.EndsWith("Permille", StringComparison.Ordinal) ? SettingUnit.Permille : SettingUnit.None;

    private static string Capitalised(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
