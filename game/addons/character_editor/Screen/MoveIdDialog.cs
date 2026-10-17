#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Asks for a new move's id (and, for a new move, its category). OK stays disabled while the id can't name a new
/// move file (<see cref="MoveIdCheck"/>), with the reason shown under it.
/// </summary>
[Tool]
public partial class MoveIdDialog : ConfirmationDialog
{
    private readonly LineEdit _id = new() { PlaceholderText = "e.g. 5e or 214b" };
    private readonly OptionButton _category = new();
    private readonly HBoxContainer _categoryRow = new();
    private readonly Label _problem = new();
    private IReadOnlyList<string> _moveIds = [];

    public MoveIdDialog()
    {
        var layout = new VBoxContainer();
        layout.AddChild(_id);
        _categoryRow.AddChild(new Label { Text = "Category" });
        _categoryRow.AddChild(_category);
        layout.AddChild(_categoryRow);
        layout.AddChild(_problem);
        AddChild(layout);
        RegisterTextEnter(_id);
        _id.TextChanged += _ => Check();
        Confirmed += () => Accepted?.Invoke(_id.Text, _categoryRow.Visible ? _category.GetItemText(_category.Selected) : "");
    }

    /// <summary>The id, and the chosen category ("" when the dialog didn't ask for one).</summary>
    public event Action<string, string>? Accepted;

    /// <param name="categories">The categories to choose from; empty to not ask (a copy keeps its own).</param>
    /// <param name="moveIds">The character's move list, which the id must not clash with.</param>
    public void Ask(string title, string suggestedId, IReadOnlyList<string> categories, IReadOnlyList<string> moveIds)
    {
        Title = title;
        _moveIds = moveIds;
        _id.Text = suggestedId;
        _category.Clear();
        foreach (string category in categories)
        {
            _category.AddItem(category);
        }

        _categoryRow.Visible = categories.Count > 0;
        Check();
        PopupCentered(new Vector2I(360, 0));
        _id.GrabFocus();
        _id.SelectAll();
    }

    private void Check()
    {
        string? problem = MoveIdCheck.Problem(_id.Text, _moveIds);
        _problem.Text = problem ?? "";
        GetOkButton().Disabled = problem is not null;
    }
}
#endif
