#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The open character's moves in input priority order (the <c>moves</c> list of <c>character.json</c>), with
/// buttons to raise or lower a move's priority.
/// </summary>
[Tool]
public partial class MoveList : VBoxContainer
{
    private const string CategoryField = "category";

    private readonly ItemList _items = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly Button _up = new() { Text = "Up", TooltipText = "Raise the selected move's input priority" };
    private readonly Button _down = new() { Text = "Down", TooltipText = "Lower the selected move's input priority" };
    private int _count;

    public MoveList()
    {
        CustomMinimumSize = new Vector2(220f, 0f);
        AddChild(new Label { Text = "Moves (input priority)" });
        AddChild(_items);
        var buttons = new HBoxContainer();
        buttons.AddChild(_up);
        buttons.AddChild(_down);
        AddChild(buttons);
        _items.ItemSelected += index => OnSelected((int)index);
        _up.Pressed += () => RequestMove(-1);
        _down.Pressed += () => RequestMove(1);
        UpdateButtons();
    }

    /// <summary>The move at this list position was selected.</summary>
    public event Action<int>? MoveSelected;

    /// <summary>The user asked to swap the move at the first position with the one at the second.</summary>
    public event Action<int, int>? SwapRequested;

    public int SelectedIndex => _items.GetSelectedItems() is [int index, ..] ? index : -1;

    /// <summary>Lists the document's moves (none when null) and selects <paramref name="selectedIndex"/>.</summary>
    public void ShowMoves(CharacterDocument? document, int selectedIndex)
    {
        _items.Clear();
        IReadOnlyList<string> moveIds = document?.MoveIds ?? [];
        _count = moveIds.Count;
        for (int i = 0; i < moveIds.Count; i++)
        {
            EditedFile? move = document!.Move(moveIds[i]);
            int item = _items.AddItem($"{i + 1}. {moveIds[i]}  ({Category(move)})");
            _items.SetItemTooltip(item, move?.Path ?? "No file for this move: see the problems below.");
        }

        if (selectedIndex >= 0 && selectedIndex < _count)
        {
            _items.Select(selectedIndex);
            _items.EnsureCurrentIsVisible();
        }

        UpdateButtons();
    }

    private static string Category(EditedFile? move) =>
        move is null ? "missing file" : move.Root[CategoryField]?.ToString() ?? "no category";

    private void OnSelected(int index)
    {
        UpdateButtons();
        MoveSelected?.Invoke(index);
    }

    private void RequestMove(int offset)
    {
        int index = SelectedIndex;
        if (index >= 0 && index + offset >= 0 && index + offset < _count)
        {
            SwapRequested?.Invoke(index, index + offset);
        }
    }

    private void UpdateButtons()
    {
        int index = SelectedIndex;
        _up.Disabled = index <= 0;
        _down.Disabled = index < 0 || index >= _count - 1;
    }
}
#endif
