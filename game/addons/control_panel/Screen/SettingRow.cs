#if TOOLS
using System;
using System.IO;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// One setting's row in a group's grid: its name, the control that edits it, the value in human terms, and a button
/// that puts it back to the saved value. The name turns the warning colour while the value is unsaved. Styled like
/// the Character tab's steppers: soft labels, numbers in the mono font.
/// </summary>
public sealed class SettingRow
{
    private const float EditorWidth = 150f;
    private const float SliderWidth = 140f;

    private readonly PanelSetting _setting;
    private readonly PanelDocument _document;
    private readonly Label _name;
    private readonly Control _editor;
    private readonly Label _readout = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        ThemeTypeVariation = MapTheme.FactHint,
    };
    private readonly Button _revert;
    private readonly Color _changed;
    private SpinBox? _spin;
    private HSlider? _slider;
    private CheckBox? _check;
    private OptionButton? _choice;

    /// <summary>Adds the row's four cells to <paramref name="grid"/>, which must have four columns.</summary>
    /// <param name="tooltip">What the setting does, shown on every cell of the row.</param>
    public SettingRow(PanelSetting setting, PanelDocument document, GridContainer grid, PanelLook look, string tooltip)
    {
        _setting = setting;
        _document = document;
        _changed = look.Changed;
        // Labels ignore the mouse by default, which would hide their tooltip.
        _name = new Label
        {
            Text = setting.Label,
            TooltipText = tooltip,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ThemeTypeVariation = MapTheme.FactLabel,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _editor = setting.Kind switch
        {
            SettingKind.Flag => CreateCheck(),
            SettingKind.Choice => CreateChoice(),
            _ => CreateNumber(look),
        };
        // Every editor keeps its own width at the start of its column, so boxes line up whether or not a slider follows.
        _editor.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        _editor.CustomMinimumSize = new Vector2(Mathf.Max(_editor.CustomMinimumSize.X, EditorWidth * look.Scale), 0f);
        SetTooltip(_editor, tooltip);
        _readout.TooltipText = tooltip;
        _readout.MouseFilter = Control.MouseFilterEnum.Stop;
        _revert = new Button
        {
            Text = "↺",
            ThemeTypeVariation = MapTheme.Ghost,
            FocusMode = Control.FocusModeEnum.None,
        };
        _revert.Pressed += () => _document.Revert(_setting);
        _revert.TooltipText = $"Back to the saved value of {setting.Label}";
        foreach (Control cell in new[] { _name, _editor, _readout, _revert })
        {
            grid.AddChild(cell);
        }
    }

    /// <summary>Dims the row when the group's switch is off; it stays editable.</summary>
    public void Dim(bool dimmed)
    {
        Color tint = dimmed ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
        foreach (Control cell in new[] { _name, _editor, _readout })
        {
            cell.Modulate = tint;
        }
    }

    /// <summary>Shows the value in the document now, without raising edits.</summary>
    public void Refresh()
    {
        bool has;
        try
        {
            has = _document.Has(_setting);
            if (has)
            {
                Show();
            }
        }
        catch (Exception exception) when (exception is FormatException or IOException or System.Text.Json.JsonException)
        {
            has = false;
        }

        _editor.Visible = has;
        _readout.Text = has ? _readout.Text : "not in the file";
        bool changed = has && _document.IsChanged(_setting);
        if (changed)
        {
            _name.AddThemeColorOverride("font_color", _changed);
        }
        else
        {
            _name.RemoveThemeColorOverride("font_color");
        }

        _revert.Modulate = changed ? Colors.White : Colors.Transparent;
        _revert.Disabled = !changed;
    }

    private void Show()
    {
        switch (_setting.Kind)
        {
            case SettingKind.Flag:
                _check!.SetPressedNoSignal(_document.Flag(_setting));
                _readout.Text = string.Empty;
                break;
            case SettingKind.Choice:
                string choice = _document.Choice(_setting);
                _choice!.Select(IndexOf(choice));
                _readout.Text = string.Empty;
                break;
            default:
                double value = _document.Number(_setting);
                _spin!.SetValueNoSignal(value);
                _slider?.SetValueNoSignal(value);
                _readout.Text = UnitReadout.Of(_setting.Unit, value);
                break;
        }
    }

    // The editor may be a box holding a spin box and a slider; every part shows the tooltip.
    private static void SetTooltip(Control control, string tooltip)
    {
        control.TooltipText = tooltip;
        foreach (Node child in control.GetChildren())
        {
            if (child is Control part)
            {
                SetTooltip(part, tooltip);
            }
        }
    }

    private Control CreateCheck()
    {
        _check = new CheckBox { Text = "On" };
        _check.Toggled += pressed => _document.SetFlag(_setting, pressed);
        return _check;
    }

    private Control CreateChoice()
    {
        _choice = new OptionButton();
        foreach (string name in _setting.Choices)
        {
            _choice.AddItem(name);
        }

        _choice.ItemSelected += index => _document.SetChoice(_setting, _setting.Choices[(int)index]);
        return _choice;
    }

    // Step stays fine-grained with a coarser arrow step: Godot snaps a value to min + n * step, which would move
    // values the loader accepts.
    private Control CreateNumber(PanelLook look)
    {
        float scale = look.Scale;
        bool whole = _setting.Kind == SettingKind.Whole;
        _spin = new SpinBox
        {
            MinValue = _setting.Min,
            MaxValue = _setting.Max,
            Step = whole ? 1 : 0.001,
            CustomArrowStep = _setting.Step,
            SelectAllOnFocus = true,
            CustomMinimumSize = new Vector2(EditorWidth * scale, 0f),
        };
        _spin.GetLineEdit().AddThemeFontOverride("font", look.Mono);
        _spin.GetLineEdit().Alignment = HorizontalAlignment.Center;
        _spin.ValueChanged += value => _document.SetNumber(_setting, value);
        if (whole)
        {
            return _spin;
        }

        // Decimal settings are feel values: a slider makes them quick to sweep while the game runs.
        _slider = new HSlider
        {
            MinValue = _setting.Min,
            MaxValue = _setting.Max,
            Step = _setting.Step,
            CustomMinimumSize = new Vector2(SliderWidth * scale, 0f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        _slider.ValueChanged += value => _document.SetNumber(_setting, value);
        var both = new HBoxContainer();
        both.AddChild(_spin);
        both.AddChild(_slider);
        return both;
    }

    private int IndexOf(string choice)
    {
        for (int i = 0; i < _setting.Choices.Count; i++)
        {
            if (_setting.Choices[i] == choice)
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
