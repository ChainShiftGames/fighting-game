using System;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>
/// One control row of the Lab's rack: a label, the control for its kind (slider with a number box, colour picker,
/// checkbox, or a slider per vector component) and a reset button while it differs from the variant's value.
/// </summary>
/// <remarks>The number box takes values past the slider's range, so nothing stops a dev from going further.</remarks>
public partial class UniformRow : VBoxContainer
{
    // Developer settings (presentation): label width and the colour of a changed setting's label.
    private const float LabelWidth = 150f;
    private const float NumberWidth = 84f;
    private static readonly Color _changedColor = new(0.78f, 0.55f, 1f);

    private readonly Label _label = new() { CustomMinimumSize = new Vector2(LabelWidth, 0f), ClipText = true };
    private readonly Button _reset = new() { Text = "↺", Flat = true, TooltipText = "Back to the variant's value" };
    private Action<LabValue?> _changed = _ => { };
    private UniformSpec _spec = null!;
    private LabValue _value;

    /// <param name="changed">Called with the new value, or null when the dev resets it.</param>
    public static UniformRow Of(string label, string tooltip, UniformSpec spec, LabValue value, bool isChanged, Action<LabValue?> changed)
    {
        UniformRow row = new() { _spec = spec, _value = value, _changed = changed };
        row._label.Text = label;
        row._label.TooltipText = tooltip;
        row.Build(isChanged);
        return row;
    }

    private void Build(bool isChanged)
    {
        HBoxContainer line = new();
        line.AddChild(_label);
        AddChild(line);
        switch (_spec.Kind)
        {
            case UniformControlKind.Colour:
                ColorPickerButton picker = new()
                {
                    Color = new Color(_value.Numbers[0], _value.Numbers[1], _value.Numbers[2], _value.Numbers.Length > 3 ? _value.Numbers[3] : 1f),
                    EditAlpha = _spec.HasAlpha,
                    CustomMinimumSize = new Vector2(72f, 26f),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                picker.ColorChanged += c => Emit(LabValue.Colour(c.R, c.G, c.B, c.A));
                line.AddChild(picker);
                break;
            case UniformControlKind.Toggle:
                CheckBox check = new() { ButtonPressed = _value.First > 0.5f, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                check.Toggled += on => Emit(LabValue.Toggle(on));
                line.AddChild(check);
                break;
            case UniformControlKind.Vector:
                line.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
                for (int i = 0; i < _value.Numbers.Length; i++)
                {
                    int component = i;
                    HBoxContainer sub = new();
                    sub.AddChild(new Label { Text = "   " + "xyzw"[i], CustomMinimumSize = new Vector2(LabelWidth, 0f) });
                    AddSlider(sub, _value.Numbers[i], number => Emit(_value.WithComponent(component, number)));
                    AddChild(sub);
                }

                break;
            default:
                AddSlider(line, _value.First, number => Emit(LabValue.Number(number)));
                break;
        }

        line.AddChild(_reset);
        _reset.Pressed += () => _changed(null);
        ShowChanged(isChanged);
    }

    private void AddSlider(HBoxContainer line, float value, Action<float> changed)
    {
        bool whole = _spec.Kind == UniformControlKind.WholeSlider;
        HSlider slider = new()
        {
            MinValue = _spec.Min,
            MaxValue = _spec.Max,
            Step = _spec.Step,
            Value = value,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        SpinBox number = new()
        {
            MinValue = _spec.Min,
            MaxValue = _spec.Max,
            Step = whole ? 1.0 : Math.Min(_spec.Step, 0.001),
            AllowGreater = true,
            AllowLesser = true,
            Value = value,
            CustomMinimumSize = new Vector2(NumberWidth, 0f),
            SelectAllOnFocus = true,
        };
        slider.ValueChanged += v =>
        {
            number.SetValueNoSignal(v);
            changed((float)v);
        };
        number.ValueChanged += v =>
        {
            slider.SetValueNoSignal(v);
            changed((float)v);
        };
        line.AddChild(slider);
        line.AddChild(number);
    }

    private void Emit(LabValue value)
    {
        _value = value;
        ShowChanged(true);
        _changed(value);
    }

    private void ShowChanged(bool isChanged)
    {
        _reset.Modulate = isChanged ? Colors.White : Colors.Transparent;
        _reset.Disabled = !isChanged;
        if (isChanged)
        {
            _label.AddThemeColorOverride("font_color", _changedColor);
        }
        else
        {
            _label.RemoveThemeColorOverride("font_color");
        }
    }
}
