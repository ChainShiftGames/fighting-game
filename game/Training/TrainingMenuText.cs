using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;

namespace FightingGame.Training;

/// <summary>What the placeholder training menu says: the summary line while closed, every entry with its key while open.</summary>
public static class TrainingMenuText
{
    /// <summary>One line shown during training while the menu is closed.</summary>
    public static string Summary(IReadOnlyList<ITrainingMenuEntry> entries, Key openKey) =>
        $"Training — {(entries.Count > 0 ? entries[0].Describe() : string.Empty)} — {openKey} menu";

    public static string Describe(IReadOnlyList<ITrainingMenuEntry> entries, Key closeKey)
    {
        var text = new StringBuilder("Training\n\n");
        foreach (ITrainingMenuEntry entry in entries)
        {
            text.Append($"{KeyName(entry.Key)}   {entry.Describe()}\n");
        }

        return text.Append($"\n{closeKey} or Esc   close\n").ToString();
    }

    // Digit keys read "1", not "Key1".
    private static string KeyName(Key key) => key is >= Key.Key0 and <= Key.Key9
        ? ((int)(key - Key.Key0)).ToString(CultureInfo.InvariantCulture)
        : key.ToString();
}
