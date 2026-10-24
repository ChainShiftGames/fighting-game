using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FightingGame.UI.Framework;

/// <summary>Turns <see cref="MenuText"/> into the words shown, through an <see cref="IMenuTranslator"/>.</summary>
public sealed class MenuTextFormatter
{
    private readonly IMenuTranslator _translator;

    public MenuTextFormatter(IMenuTranslator translator)
    {
        _translator = translator;
    }

    public string Format(MenuText text)
    {
        if (!text.IsKey)
        {
            return text.Value;
        }

        string template = _translator.Translate(text.Value);
        if (text.Arguments.Count == 0)
        {
            return template;
        }

        object[] arguments = new object[text.Arguments.Count];
        for (int i = 0; i < arguments.Length; i++)
        {
            arguments[i] = Format(text.Arguments[i]);
        }

        return string.Format(CultureInfo.InvariantCulture, template, arguments);
    }

    /// <summary>One line per text; an empty text is a blank line.</summary>
    public string FormatLines(IReadOnlyList<MenuText> lines)
    {
        var joined = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                joined.Append('\n');
            }

            joined.Append(Format(lines[i]));
        }

        return joined.ToString();
    }
}
