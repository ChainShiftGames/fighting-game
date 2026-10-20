#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>Plain-language facts, one per row: what it is and a hint on the left, the number on the right.</summary>
[Tool]
public partial class FactList : VBoxContainer
{
    /// <summary>One row; <see cref="Parts"/> are drawn side by side, each in its own colour (e.g. +2 / −2).</summary>
    public sealed record Fact(string Label, string Hint, IReadOnlyList<(string Text, Color Color)> Parts);

    public FactList()
    {
        AddThemeConstantOverride("separation", 0);
    }

    public static Fact Plain(string label, string hint, string value) => new(label, hint, [(value, MapPalette.Text)]);

    public void ShowFacts(IReadOnlyList<Fact> facts)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < facts.Count; i++)
        {
            AddChild(Row(facts[i], last: i == facts.Count - 1));
        }
    }

    private static Control Row(Fact fact, bool last)
    {
        var row = new HBoxContainer();
        var words = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        words.AddThemeConstantOverride("separation", 0);
        words.AddChild(new Label { Text = fact.Label, ThemeTypeVariation = MapTheme.FactLabel });
        if (fact.Hint.Length > 0)
        {
            words.AddChild(new Label { Text = fact.Hint, ThemeTypeVariation = MapTheme.FactHint });
        }

        row.AddChild(words);
        var value = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        value.AddThemeConstantOverride("separation", 0);
        foreach ((string text, Color color) in fact.Parts)
        {
            var part = new Label { Text = text, ThemeTypeVariation = MapTheme.FactValue };
            part.AddThemeColorOverride("font_color", color);
            value.AddChild(part);
        }

        row.AddChild(value);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 9);
        box.AddChild(row);
        if (!last)
        {
            box.AddChild(new HSeparator());
        }

        return box;
    }
}
#endif
