#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Edits a JSON object through the fields its schema lists: a spin box, text box, check box or choice per field,
/// nested objects indented, lists of objects as one nested form per item (hits, velocity ranges, projectiles) with
/// Add item/Remove item, maps (move roles) as one row per entry with Add entry/Remove, and Add/Remove for optional
/// fields. Anything the form can't edit (hand-edited values of the
/// wrong type, fields the schema doesn't know) is shown read-only, never hidden.
/// </summary>
/// <remarks>Changes are made to the object in place; <see cref="Changed"/> tells the owner to mark its file.</remarks>
[Tool]
public partial class SchemaForm : VBoxContainer
{
    /// <summary>Width of the field names, so other panels' rows line up with the form's.</summary>
    public const float LabelWidth = 170f;
    private const float NestedIndent = 18f;
    private const int MaxShownJson = 120;

    private JsonObject _target = [];
    private SchemaObject _schema = SchemaObject.Empty;
    private Func<string, bool> _shows = _ => true;

    // Set when this form edits a map's entries: its schema is then the entries, read again on every rebuild.
    private SchemaField? _mapValue;

    /// <summary>A value in the object changed, or a field was added or removed.</summary>
    public event Action? Changed;

    /// <param name="shows">Which fields this form shows; the owner edits the others its own way.</param>
    public void Show(JsonObject target, SchemaObject schema, Func<string, bool>? shows = null)
    {
        _target = target;
        _schema = schema;
        _shows = shows ?? (_ => true);
        _mapValue = null;
        Rebuild();
    }

    /// <summary>Edits the entries of a map field (<see cref="SchemaFieldKind.Map"/>), each holding a <paramref name="value"/>.</summary>
    public void ShowMap(JsonObject map, SchemaField value)
    {
        _target = map;
        _shows = _ => true;
        _mapValue = value;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        if (_mapValue is not null)
        {
            _schema = JsonMap.EntriesOf(_target, _mapValue);
        }

        foreach (SchemaField field in _schema.Fields)
        {
            if (_shows(field.Name))
            {
                AddField(field);
            }
        }

        foreach (KeyValuePair<string, JsonNode?> entry in _target)
        {
            if (_shows(entry.Key) && _schema.Field(entry.Key) is null)
            {
                AddRow(entry.Key, "Not in the schema: validation reports it.", ReadOnly(entry.Value), removable: true);
            }
        }
    }

    // Rebuilding frees the control whose signal is running, so it waits until the signal is done.
    private void RebuildLater() => Callable.From(Rebuild).CallDeferred();

    private void AddField(SchemaField field)
    {
        if (!_target.ContainsKey(field.Name))
        {
            var add = new Button { Text = field.IsRequired ? "Add (required)" : "Add" };
            add.Pressed += () =>
            {
                Set(field.Name, SchemaDefaults.ValueFor(field));
                RebuildLater();
            };
            AddRow(field.Name, field.Description, add, removable: false);
            return;
        }

        JsonNode? value = _target[field.Name];
        if (field.Kind == SchemaFieldKind.Object && value is JsonObject nested)
        {
            AddRow(field.Name, field.Description, new Control(), removable: !field.IsRequired);
            var form = new SchemaForm();
            form.Show(nested, field.Object ?? SchemaObject.Empty);
            form.Changed += () => Changed?.Invoke();
            var indent = new MarginContainer();
            indent.AddThemeConstantOverride("margin_left", (int)NestedIndent);
            indent.AddChild(form);
            AddChild(indent);
            return;
        }

        if (field.Kind == SchemaFieldKind.Map && field.Value is { } entryValue && value is JsonObject map)
        {
            AddMap(field, entryValue, map);
            return;
        }

        if (field.Items is { } itemSchema && value is JsonArray list && IsObjectList(list))
        {
            AddList(field, itemSchema, list);
            return;
        }

        AddRow(field.Name, field.Description, Input(field, value) ?? ReadOnly(value), removable: !field.IsRequired);
    }

    private static bool IsObjectList(JsonArray list)
    {
        foreach (JsonNode? item in list)
        {
            if (item is not JsonObject)
            {
                return false;
            }
        }

        return true;
    }

    private void AddList(SchemaField field, SchemaObject itemSchema, JsonArray list)
    {
        var add = new Button { Text = "Add item", TooltipText = $"Add a {field.Name} item with its required fields" };
        add.Pressed += () =>
        {
            list.Add(SchemaDefaults.ObjectFor(itemSchema));
            Changed?.Invoke();
            RebuildLater();
        };
        AddRow(field.Name, field.Description, add, removable: !field.IsRequired);
        for (int index = 0; index < list.Count; index++)
        {
            AddListItem(field.Name, itemSchema, list, index);
        }
    }

    private void AddMap(SchemaField field, SchemaField entryValue, JsonObject map)
    {
        var adder = new HBoxContainer();
        var key = new LineEdit { PlaceholderText = "new key", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var add = new Button { Text = "Add entry", Disabled = true, TooltipText = JsonMap.AddProblem(map, "") };
        key.TextChanged += text =>
        {
            string? problem = JsonMap.AddProblem(map, text.Trim());
            add.Disabled = problem is not null;
            add.TooltipText = problem ?? $"Add '{text.Trim()}' to {field.Name}";
        };
        add.Pressed += () =>
        {
            JsonMap.Add(map, key.Text.Trim(), entryValue);
            Changed?.Invoke();
            RebuildLater();
        };
        adder.AddChild(key);
        adder.AddChild(add);
        AddRow(field.Name, field.Description, adder, removable: !field.IsRequired);

        var form = new SchemaForm();
        form.ShowMap(map, entryValue);
        form.Changed += () => Changed?.Invoke();
        var indent = new MarginContainer();
        indent.AddThemeConstantOverride("margin_left", (int)NestedIndent);
        indent.AddChild(form);
        AddChild(indent);
    }

    private void AddListItem(string name, SchemaObject itemSchema, JsonArray list, int index)
    {
        var item = new VBoxContainer();
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = $"{name}[{index}]", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var remove = new Button { Text = "Remove item" };
        remove.Pressed += () =>
        {
            list.RemoveAt(index);
            Changed?.Invoke();
            RebuildLater();
        };
        header.AddChild(remove);
        item.AddChild(header);
        var form = new SchemaForm();
        form.Show((JsonObject)list[index]!, itemSchema);
        form.Changed += () => Changed?.Invoke();
        item.AddChild(form);
        var indent = new MarginContainer();
        indent.AddThemeConstantOverride("margin_left", (int)NestedIndent);
        indent.AddChild(item);
        AddChild(indent);
    }

    // Null when the value isn't of the field's kind (a hand edit): it is shown read-only instead.
    private Control? Input(SchemaField field, JsonNode? value)
    {
        switch (field.Kind)
        {
            case SchemaFieldKind.Integer when JsonNodeValue.Int(value) is int number:
                var spin = new SpinBox
                {
                    MinValue = field.Minimum ?? int.MinValue,
                    MaxValue = field.Maximum ?? int.MaxValue,
                    Step = 1,
                    Rounded = true,
                    Value = number,
                };
                spin.ValueChanged += newValue => Set(field.Name, (int)newValue);
                return spin;
            case SchemaFieldKind.Text when JsonNodeValue.Text(value) is string text:
                var line = new LineEdit { Text = text };
                line.TextChanged += newText => Set(field.Name, newText);
                return line;
            case SchemaFieldKind.Boolean when JsonNodeValue.Flag(value) is bool flag:
                var check = new CheckBox { ButtonPressed = flag };
                check.Toggled += on => Set(field.Name, on);
                return check;
            case SchemaFieldKind.Choice when JsonNodeValue.Text(value) is string choice:
                return ChoiceInput(field, choice);
            case SchemaFieldKind.ChoiceList when value is JsonArray:
                return ChoiceListInput(field);
            default:
                return null;
        }
    }

    private OptionButton ChoiceInput(SchemaField field, string current)
    {
        var options = new OptionButton();
        List<string> items = [.. field.Choices];
        if (!items.Contains(current))
        {
            // Not an allowed value (validation reports it), but the form still shows what the file says.
            items.Add(current);
        }

        foreach (string item in items)
        {
            options.AddItem(item);
        }

        options.Select(items.IndexOf(current));
        options.ItemSelected += index => Set(field.Name, options.GetItemText((int)index));
        return options;
    }

    private HFlowContainer ChoiceListInput(SchemaField field)
    {
        var flow = new HFlowContainer();
        foreach (string choice in field.Choices)
        {
            var check = new CheckBox
            {
                Text = choice,
                ButtonPressed = JsonStringList.Contains(_target, field.Name, choice),
            };
            check.Toggled += on =>
            {
                JsonStringList.Set(_target, field.Name, choice, on, field.IsRequired, _schema.FieldNames);
                Changed?.Invoke();
            };
            flow.AddChild(check);
        }

        return flow;
    }

    private static Label ReadOnly(JsonNode? value)
    {
        string json = value?.ToJsonString() ?? "null";
        return new Label
        {
            Text = json.Length > MaxShownJson ? json[..MaxShownJson] + "…" : json,
            TooltipText = json,
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
    }

    private void AddRow(string name, string description, Control input, bool removable)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = name,
            TooltipText = description,
            MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = new Vector2(LabelWidth, 0f),
        });
        input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(input);
        if (removable)
        {
            var remove = new Button { Text = "Remove", TooltipText = $"Remove {name} from the file" };
            remove.Pressed += () =>
            {
                _target.Remove(name);
                Changed?.Invoke();
                RebuildLater();
            };
            row.AddChild(remove);
        }

        AddChild(row);
    }

    private void Set(string name, JsonNode? value)
    {
        JsonFieldOrder.Set(_target, name, value, _schema.FieldNames);
        Changed?.Invoke();
    }
}
#endif
