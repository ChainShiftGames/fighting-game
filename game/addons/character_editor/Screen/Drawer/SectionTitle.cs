#if TOOLS
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>A small capitalised heading over a group of controls, with an optional note on the right.</summary>
[Tool]
public partial class SectionTitle : HBoxContainer
{
    private readonly Label _title = new() { ThemeTypeVariation = MapTheme.Section, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _note = new() { ThemeTypeVariation = MapTheme.MutedLabel };

    public SectionTitle()
    {
        AddChild(_title);
        AddChild(_note);
    }

    public static SectionTitle Of(string title, string note = "")
    {
        var section = new SectionTitle();
        section.Show(title, note);
        return section;
    }

    public void Show(string title, string note = "")
    {
        _title.Text = title.ToUpper(CultureInfo.InvariantCulture);
        _note.Text = note;
    }
}
#endif
