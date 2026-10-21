#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// A move's hit, hurt, push and throw boxes, one frame at a time: pick the frame, draw and drag boxes on the
/// <see cref="BoxCanvas"/>, edit the selected box's fields, and copy a frame's boxes onto another frame.
/// </summary>
/// <remarks>
/// The panel drives <see cref="Canvas"/> but doesn't hold it, so the owner can show it elsewhere (the drawer's
/// preview); the frame this panel picks is the frame the canvas shows.
/// </remarks>
[Tool]
public partial class BoxPanel : VBoxContainer
{
    private const string SelectOnly = "(select only)";

    private readonly SpinBox _frame = new() { MinValue = 1, Step = 1, Rounded = true };
    private readonly Label _frameCount = new();
    private readonly OptionButton _drawKind = new() { TooltipText = "What a drag on empty space draws" };
    private readonly CheckBox _thisFrameOnly = new()
    {
        Text = "Edit this frame only",
        ButtonPressed = true,
        TooltipText = "Dragging a box that spans several frames splits this frame out of it first",
    };

    private readonly SpinBox _copyTo = new() { MinValue = 1, Step = 1, Rounded = true };
    private readonly BoxCanvas _canvas = new();
    private readonly Label _hint = new()
    {
        Text = "Click a box to select it. Pick a kind and drag to draw one, even over boxes of other kinds (they can't be picked while drawing; pick \"(select only)\" to edit them). Right-drag pans, the wheel zooms; hold Alt to place edges off the pixel grid.",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = Look.MapTheme.MutedLabel,
    };
    private readonly Label _selectedTitle = new();
    private readonly SchemaForm _selectedForm = new();
    private readonly Button _removeFromFrame = new() { Text = "Remove from this frame" };
    private readonly Button _delete = new() { Text = "Delete box", TooltipText = "Remove the box from all its frames" };
    private JsonObject _move = [];
    private SchemaObject _moveSchema = SchemaObject.Empty;

    public BoxPanel()
    {
        AddChild(_hint);
        AddChild(Row(new Label { Text = "Frame" }, Step("‹", -1), _frame, Step("›", 1), _frameCount));
        AddChild(Row(new Label { Text = "Draw" }, _drawKind, _thisFrameOnly));
        var copy = new Button { Text = "Copy this frame's boxes to frame" };
        copy.Pressed += CopyFrame;
        AddChild(Row(copy, _copyTo));
        _selectedTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _selectedTitle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_selectedTitle);
        AddChild(Row(_removeFromFrame, _delete));
        AddChild(_selectedForm);

        _frame.ValueChanged += _ => ShowFrame();
        _drawKind.ItemSelected += index => _canvas.DrawKind = index == 0 ? null : _drawKind.GetItemText((int)index);
        _thisFrameOnly.Toggled += on => _canvas.ThisFrameOnly = on;
        _canvas.SelectionChanged += ShowSelected;
        _canvas.Edited += () =>
        {
            ShowSelected(_canvas.Selected);
            Changed?.Invoke();
        };
        _selectedForm.Changed += () =>
        {
            _canvas.QueueRedraw();
            Changed?.Invoke();
        };
        _removeFromFrame.Pressed += () => RemoveSelected(thisFrameOnly: true);
        _delete.Pressed += () => RemoveSelected(thisFrameOnly: false);
    }

    /// <summary>A box changed, or one was added or removed.</summary>
    public event Action? Changed;

    /// <summary>The frame shown changed, so the timeline can mark it.</summary>
    public event Action<int>? FrameShown;

    public int Frame => (int)_frame.Value;

    /// <summary>The canvas this panel draws on; the owner adds it where it should appear.</summary>
    public BoxCanvas Canvas => _canvas;

    /// <summary>Shows the character's model behind the boxes, when it has one.</summary>
    public void ShowCharacter(string? characterId, IReadOnlyList<string> moveIds) =>
        _canvas.ShowCharacter(characterId, moveIds);

    public void ShowMove(JsonObject move, SchemaObject moveSchema)
    {
        bool sameMove = move == _move;
        _move = move;
        _moveSchema = moveSchema;
        ShowKinds();
        int frames = Math.Max(1, FramePhases.Of(move)?.TotalFrames ?? 1);
        _frame.MaxValue = frames;
        _copyTo.MaxValue = frames;
        _frameCount.Text = $"of {frames}";
        // A new move starts on frame 1; the same move (its frame data changed) stays on its frame.
        _frame.SetValueNoSignal(sameMove ? Math.Min(Frame, frames) : 1);
        ShowFrame(sameMove ? _canvas.Selected : null);
    }

    /// <summary>Shows a frame picked elsewhere, e.g. on the timeline.</summary>
    public void SelectFrame(int frame) => _frame.Value = frame;

    private static HFlowContainer Row(params Control[] controls)
    {
        var row = new HFlowContainer();
        foreach (Control control in controls)
        {
            row.AddChild(control);
        }

        return row;
    }

    private Button Step(string text, int offset)
    {
        var button = new Button { Text = text };
        button.Pressed += () => _frame.Value = Frame + offset;
        return button;
    }

    private void ShowKinds()
    {
        string? current = _canvas.DrawKind;
        _drawKind.Clear();
        _drawKind.AddItem(SelectOnly);
        IReadOnlyList<string> kinds = BoxSchema().Field(MoveFields.Kind)?.Choices ?? [];
        for (int i = 0; i < kinds.Count; i++)
        {
            _drawKind.AddItem(kinds[i]);
            if (kinds[i] == current)
            {
                _drawKind.Select(i + 1);
            }
        }

        if (current is null || !Contains(kinds, current))
        {
            _drawKind.Select(0);
            _canvas.DrawKind = null;
        }
    }

    private void ShowFrame() => ShowFrame(_canvas.Selected);

    private void ShowFrame(JsonObject? selected)
    {
        _canvas.ShowFrame(_move, _moveSchema.FieldNames, Frame, selected);
        ShowSelected(selected);
        FrameShown?.Invoke(Frame);
    }

    private void ShowSelected(JsonObject? box)
    {
        bool shown = box is not null && MoveBoxes.Covers(box, Frame);
        _selectedTitle.Text = shown ? "Selected box" : "No box selected: click one, or pick a kind and drag to draw";
        _removeFromFrame.Visible = _delete.Visible = _selectedForm.Visible = shown;
        if (shown)
        {
            _selectedForm.Show(box!, BoxSchema());
        }
    }

    private void RemoveSelected(bool thisFrameOnly)
    {
        JsonObject box = _canvas.Selected!;
        MoveBoxes.Remove(_move, thisFrameOnly ? MoveBoxes.Isolate(_move, box, Frame) : box);
        ShowFrame(null);
        Changed?.Invoke();
    }

    private void CopyFrame()
    {
        MoveBoxes.CopyFrame(_move, Frame, (int)_copyTo.Value, _moveSchema.FieldNames);
        ShowFrame(null);
        Changed?.Invoke();
    }

    private SchemaObject BoxSchema() => _moveSchema.Field(MoveFields.Boxes)?.Items ?? SchemaObject.Empty;

    private static bool Contains<T>(IReadOnlyList<T> items, T item)
    {
        foreach (T existing in items)
        {
            if (Equals(existing, item))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
