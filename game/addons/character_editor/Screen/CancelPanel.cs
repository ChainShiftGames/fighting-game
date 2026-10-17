#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// A move's cancel windows: frames, what they cancel on and into, add and remove. Each window's own fields come from
/// the schema (<see cref="SchemaForm"/>); its <c>into</c> list is ticked from the character's attacks.
/// </summary>
[Tool]
public partial class CancelPanel : VBoxContainer
{
    private readonly VBoxContainer _windows = new();
    private readonly Button _add = new() { Text = "Add cancel window" };
    private JsonObject _move = [];
    private SchemaObject _moveSchema = SchemaObject.Empty;
    private IReadOnlyList<string> _targets = [];

    public CancelPanel()
    {
        AddChild(_windows);
        AddChild(_add);
        _add.Pressed += () =>
        {
            MoveCancels.Add(_move, _moveSchema.FieldNames);
            Changed?.Invoke();
            Callable.From(Rebuild).CallDeferred();
        };
    }

    /// <summary>A window changed, or one was added or removed.</summary>
    public event Action? Changed;

    /// <param name="targets">Move ids a window can cancel into: the character's attacks.</param>
    public void ShowMove(JsonObject move, SchemaObject moveSchema, IReadOnlyList<string> targets)
    {
        _move = move;
        _moveSchema = moveSchema;
        _targets = targets;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in _windows.GetChildren())
        {
            _windows.RemoveChild(child);
            child.QueueFree();
        }

        SchemaObject windowSchema = _moveSchema.Field(MoveFields.Cancels)?.Items ?? SchemaObject.Empty;
        IReadOnlyList<JsonObject> windows = MoveCancels.Windows(_move);
        for (int i = 0; i < windows.Count; i++)
        {
            AddWindow(i + 1, windows[i], windowSchema);
        }
    }

    private void AddWindow(int number, JsonObject window, SchemaObject windowSchema)
    {
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = $"Cancel {number}", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var remove = new Button { Text = "Remove" };
        remove.Pressed += () =>
        {
            MoveCancels.Remove(_move, window);
            Changed?.Invoke();
            Callable.From(Rebuild).CallDeferred();
        };
        header.AddChild(remove);
        _windows.AddChild(header);

        var form = new SchemaForm();
        form.Show(window, windowSchema, field => field != MoveFields.Into);
        form.Changed += () => Changed?.Invoke();
        _windows.AddChild(form);
        _windows.AddChild(IntoTargets(window, windowSchema));
        _windows.AddChild(new HSeparator());
    }

    private HBoxContainer IntoTargets(JsonObject window, SchemaObject windowSchema)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = MoveFields.Into,
            TooltipText = windowSchema.Field(MoveFields.Into)?.Description ?? "",
            MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = new Vector2(SchemaForm.LabelWidth, 0f),
        });
        var flow = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        // Targets the character doesn't have are listed too, so they can be unticked (validation reports them).
        List<string> shown = [.. _targets];
        foreach (string target in JsonStringList.Read(window, MoveFields.Into))
        {
            if (!shown.Contains(target))
            {
                shown.Add(target);
            }
        }

        foreach (string target in shown)
        {
            var check = new CheckBox
            {
                Text = target,
                ButtonPressed = JsonStringList.Contains(window, MoveFields.Into, target),
            };
            check.Toggled += on =>
            {
                JsonStringList.Set(window, MoveFields.Into, target, on, keepWhenEmpty: false, windowSchema.FieldNames);
                Changed?.Invoke();
            };
            flow.AddChild(check);
        }

        row.AddChild(flow);
        return row;
    }
}
#endif
