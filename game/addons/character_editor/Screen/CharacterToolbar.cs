#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>The editor's top bar: which character is open, reload/validate/save, and the last result.</summary>
[Tool]
public partial class CharacterToolbar : HBoxContainer
{
    private readonly OptionButton _characters = new() { TooltipText = "Folders in data/characters/" };
    private readonly Button _reload = new() { Text = "Reload", TooltipText = "Read the files again" };
    private readonly Button _validate = new() { Text = "Validate", TooltipText = "Check the data as the game loads it" };
    private readonly Button _save = new() { Text = "Save", TooltipText = "Validate, then write the changed files" };
    private readonly Label _status = new()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
    };

    public CharacterToolbar()
    {
        AddChild(new Label { Text = "Character" });
        foreach (Control control in new Control[] { _characters, _reload, _validate, _save, _status })
        {
            AddChild(control);
        }

        _characters.ItemSelected += index => CharacterChosen?.Invoke(_characters.GetItemText((int)index));
        _reload.Pressed += () => ReloadPressed?.Invoke();
        _validate.Pressed += () => ValidatePressed?.Invoke();
        _save.Pressed += () => SavePressed?.Invoke();
    }

    public event Action<string>? CharacterChosen;

    public event Action? ReloadPressed;

    public event Action? ValidatePressed;

    public event Action? SavePressed;

    /// <summary>Lists the characters and selects <paramref name="selected"/>, without raising an event.</summary>
    public void ShowCharacters(IReadOnlyList<string> ids, string? selected)
    {
        _characters.Clear();
        foreach (string id in ids)
        {
            _characters.AddItem(id);
        }

        Select(selected);
    }

    /// <summary>Shows <paramref name="id"/> as the open character, e.g. after a switch was cancelled.</summary>
    public void Select(string? id)
    {
        for (int i = 0; i < _characters.ItemCount; i++)
        {
            if (_characters.GetItemText(i) == id)
            {
                _characters.Select(i);
                return;
            }
        }

        _characters.Select(-1);
    }

    /// <param name="hasDocument">A character is open, so validating and saving make sense.</param>
    /// <param name="hasChanges">It has unsaved changes.</param>
    public void ShowState(bool hasDocument, bool hasChanges, string status)
    {
        _validate.Disabled = !hasDocument;
        _save.Disabled = !hasChanges;
        _status.Text = hasChanges ? $"{status} (unsaved changes)" : status;
    }

    /// <summary>Without a data folder there is nothing to open or save.</summary>
    public void Disable(string reason)
    {
        foreach (BaseButton button in new BaseButton[] { _characters, _reload, _validate, _save })
        {
            button.Disabled = true;
        }

        _status.Text = reason;
    }
}
#endif
