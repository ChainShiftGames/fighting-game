#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Edits the selected move: category, frame data with its timeline, boxes frame by frame, input, hit and other
/// properties, and cancel windows. Velocity and projectiles are shown in the file view below.
/// </summary>
[Tool]
public partial class MoveEditor : VBoxContainer
{
    // Edited by this editor's own panels, so the generic properties form leaves them out.
    private static readonly HashSet<string> _ownPanelFields =
    [
        MoveFields.Schema, MoveFields.SchemaVersion, MoveFields.Id, MoveFields.Category, MoveFields.Duration,
        MoveFields.FrameData, MoveFields.Input, MoveFields.Cancels, MoveFields.Boxes,
    ];

    // Not for basic moves; shown for one only when the file has them, so they can be removed.
    private static readonly HashSet<string> _attackOnlyFields =
        [MoveFields.UsableIn, MoveFields.Hit, MoveFields.Throw, MoveFields.Projectiles];

    private readonly Label _path = new();
    private readonly OptionButton _category = new();
    private readonly FrameDataPanel _frames = new();
    private readonly FrameTimeline _timeline = new();
    private readonly BoxPanel _boxes = new();
    private readonly Label _inputTitle = Section("Input");
    private readonly MoveInputPanel _input = new();
    private readonly SchemaForm _properties = new();
    private readonly CancelPanel _cancels = new();
    private readonly TextEdit _json = new()
    {
        Editable = false,
        CustomMinimumSize = new Vector2(0f, 300f),
        WrapMode = TextEdit.LineWrappingMode.None,
    };

    private readonly List<Control> _moveControls = [];
    private EditedFile? _move;
    private EditingContext? _context;
    private IReadOnlyList<string> _cancelTargets = [];

    public MoveEditor()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_path);
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "Category" });
        header.AddChild(_category);
        _moveControls.AddRange(
        [
            header, Section("Frame data"), _frames, _timeline, Section("Boxes"), _boxes, _inputTitle, _input, Section("Properties"), _properties,
            Section("Cancel windows"), _cancels, Section("File (velocity and projectiles: edit the file for now)"),
            _json,
        ]);
        foreach (Control control in _moveControls)
        {
            AddChild(control);
        }

        _category.ItemSelected += index => ChangeCategory(_category.GetItemText((int)index));
        _frames.FramesChanged += () =>
        {
            // Boxes and cancel windows may have moved with their phase.
            _boxes.ShowMove(_move!.Root, _context!.MoveSchema);
            _cancels.ShowMove(_move.Root, _context.MoveSchema, _cancelTargets);
            OnChanged(listChanged: false);
        };
        _input.Changed += () => OnChanged(listChanged: false);
        _boxes.Changed += () => OnChanged(listChanged: false);
        _boxes.FrameShown += _timeline.MarkFrame;
        _timeline.FrameClicked += _boxes.SelectFrame;
        _properties.Changed += () => OnChanged(listChanged: false);
        _cancels.Changed += () => OnChanged(listChanged: false);
        ShowMove(null, null, []);
    }

    /// <summary>The move's file changed; <c>true</c> when the move list's label for it may have changed too.</summary>
    public event Action<bool>? Edited;

    /// <summary>Shows the move; nothing when null (no move selected, or its file is missing).</summary>
    /// <param name="cancelTargets">Move ids a cancel window can go into: the character's attacks.</param>
    public void ShowMove(EditedFile? move, EditingContext? context, IReadOnlyList<string> cancelTargets)
    {
        _move = move;
        _context = context;
        _cancelTargets = cancelTargets;
        _path.Text = move?.Path ?? "No move selected";
        foreach (Control control in _moveControls)
        {
            control.Visible = move is not null && context is not null;
        }

        if (move is null || context is null)
        {
            return;
        }

        JsonObject root = move.Root;
        bool isBasic = JsonNodeValue.Text(root[MoveFields.Category]) == MoveFields.BasicCategory;
        ShowCategory(root, context.MoveSchema);
        _frames.ShowMove(root, context.MoveSchema.FieldNames);
        _boxes.ShowMove(root, context.MoveSchema);
        _inputTitle.Visible = _input.Visible = !isBasic;
        _input.ShowMove(root, context.ButtonIds, context.MoveSchema.FieldNames);
        _properties.Show(root, context.MoveSchema, field => !_ownPanelFields.Contains(field)
            && (!isBasic || !_attackOnlyFields.Contains(field) || root.ContainsKey(field)));
        _cancels.ShowMove(root, context.MoveSchema, cancelTargets);
        ShowDerived();
    }

    private static Label Section(string title) => new() { Text = title, ThemeTypeVariation = "HeaderSmall" };

    private void ShowCategory(JsonObject root, SchemaObject moveSchema)
    {
        _category.Clear();
        List<string> choices = [.. moveSchema.Field(MoveFields.Category)?.Choices ?? []];
        string current = JsonNodeValue.Text(root[MoveFields.Category]) ?? "";
        if (!choices.Contains(current))
        {
            choices.Add(current);
        }

        foreach (string choice in choices)
        {
            _category.AddItem(choice);
        }

        _category.Select(choices.IndexOf(current));
    }

    private void ChangeCategory(string category)
    {
        MoveCategory.Set(_move!.Root, category, _context!.MoveSchema, _context.FirstButton);
        OnChanged(listChanged: true);
        // Basic and attack moves show different panels; the option button's signal is still running.
        Callable.From(() => ShowMove(_move, _context, _cancelTargets)).CallDeferred();
    }

    private void OnChanged(bool listChanged)
    {
        _move!.MarkChanged();
        ShowDerived();
        Edited?.Invoke(listChanged);
    }

    // The views that follow the JSON rather than editing it.
    private void ShowDerived()
    {
        _timeline.ShowTimeline(MoveTimeline.Of(_move!.Root));
        _json.Text = JsonDataWriter.Write(_move.Root, _move.Layout);
    }
}
#endif
