#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;
using Sim.Data.Characters.Reading;
using Sim.Data.Sources;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>Picks one of the characters in <c>data/characters/</c>; the Characters and Hit feel pages share the choice.</summary>
[Tool]
public partial class CharacterPicker : HBoxContainer
{
    // Like the Character tab's title: the character's name in the heading font, as a flat picker.
    private readonly OptionButton _list = new() { ThemeTypeVariation = MapTheme.Flat, Flat = true };
    private IReadOnlyList<string> _ids = [];

    public CharacterPicker()
    {
        const string Tip = "Which character's files this page edits (data/characters/<id>/).";
        AddChild(new Label
        {
            Text = "CHARACTER",
            TooltipText = Tip,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = MapTheme.Section,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });
        _list.TooltipText = Tip;
        AddChild(_list);
        _list.ItemSelected += index => Picked?.Invoke(_ids[(int)index]);
    }

    public event Action<string>? Picked;

    /// <summary>The picked character's ID; null without characters.</summary>
    public string? Current => _list.Selected >= 0 && _list.Selected < _ids.Count ? _ids[_list.Selected] : null;

    /// <summary>Lists the characters again, keeping the pick when it still exists.</summary>
    public void Fill(string dataRoot, string? keep)
    {
        _ids = new DirectoryDataSource(dataRoot).ListDirectories(CharacterSchema.CharactersDirectory);
        _list.Clear();
        foreach (string id in _ids)
        {
            _list.AddItem(MoveNames.Words(id));
        }

        int index = keep is null ? 0 : Math.Max(0, IndexOf(keep));
        if (_ids.Count > 0)
        {
            _list.Select(index);
        }
    }

    public void Select(string id) => _list.Select(IndexOf(id));

    public override void _Ready()
    {
        MapLook look = MapLook.Of(this);
        _list.AddThemeFontOverride("font", look.Display);
        _list.AddThemeFontSizeOverride("font_size", look.FontSize(18f));
    }

    private int IndexOf(string id)
    {
        for (int i = 0; i < _ids.Count; i++)
        {
            if (_ids[i] == id)
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
