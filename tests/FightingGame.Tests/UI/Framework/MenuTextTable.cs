using System.Collections.Generic;
using System.IO;
using System.Text;
using FightingGame.UI.Framework;

namespace FightingGame.Tests.UI.Framework;

/// <summary>The game's menu translation table (game/Assets/UI/menu_text.csv), read without Godot.</summary>
internal sealed class MenuTextTable : IMenuTranslator
{
    private readonly Dictionary<string, string> _english = [];

    private MenuTextTable()
    {
        string path = Path.Combine(TestGameData.RepositoryRoot, "game", "Assets", "UI", "menu_text.csv");
        List<List<string>> rows = Parse(File.ReadAllText(path));
        int english = rows[0].IndexOf("en");
        for (int i = 1; i < rows.Count; i++)
        {
            _english.Add(rows[i][0], rows[i][english]);
        }
    }

    public static MenuTextTable English { get; } = new();

    public static MenuTextFormatter Formatter { get; } = new(English);

    public IReadOnlyCollection<string> Keys => _english.Keys;

    public bool Has(string key) => _english.ContainsKey(key);

    public string Translate(string key) => _english.TryGetValue(key, out string? text) ? text : key;

    // Godot's CSV rules: commas separate, quotes wrap fields with commas, "" is a quote.
    private static List<List<string>> Parse(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\n')
            {
                row.Add(field.ToString().TrimEnd('\r'));
                field.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                field.Append(c);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
