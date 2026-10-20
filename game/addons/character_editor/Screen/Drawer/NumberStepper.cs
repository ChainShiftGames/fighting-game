#if TOOLS
using System;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// One number with a plain name above it, − and + beside it and a hint below ("Wind-up · until it can hit"). The
/// file can hold a finer unit than the one shown: speeds are stored in subpixels and shown in pixels.
/// </summary>
[Tool]
public partial class NumberStepper : VBoxContainer
{
    private readonly Label _title = new() { ThemeTypeVariation = MapTheme.FactLabel };
    private readonly Label _hint = new() { ThemeTypeVariation = MapTheme.FactHint, AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly LineEdit _field = new()
    {
        Alignment = HorizontalAlignment.Center,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SelectAllOnFocus = true,
    };

    private int _value;

    public NumberStepper()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 3);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 3);
        row.AddChild(StepButton("−", -1));
        row.AddChild(_field);
        row.AddChild(StepButton("+", 1));
        AddChild(_title);
        AddChild(row);
        AddChild(_hint);
        _field.TextSubmitted += _ => ApplyText();
        _field.FocusExited += ApplyText;
    }

    /// <summary>The user set a new value, in the file's unit.</summary>
    public event Action<int>? ValueChanged;

    public int Minimum { get; set; }

    public int Maximum { get; set; } = 999;

    /// <summary>How much − and + change the value, in the file's unit.</summary>
    public int Step { get; set; } = 1;

    /// <summary>File units per shown unit: 1000 to show subpixels as pixels.</summary>
    public int Divisor { get; set; } = 1;

    public int Value => _value;

    /// <param name="titleColor">A phase's colour for its stepper; null for the usual label colour.</param>
    public void Show(string title, string hint, int value, Color? titleColor = null)
    {
        _title.Text = title;
        _hint.Text = hint;
        _hint.Visible = hint.Length > 0;
        if (titleColor is Color color)
        {
            _title.AddThemeColorOverride("font_color", color);
        }
        else
        {
            _title.RemoveThemeColorOverride("font_color");
        }

        _value = value;
        _field.Text = Format(value);
    }

    public override void _Ready()
    {
        MapLook look = MapLook.Of(this);
        _field.AddThemeFontOverride("font", look.Mono);
        _field.AddThemeFontSizeOverride("font_size", look.FontSize(15f));
    }

    private Button StepButton(string text, int direction)
    {
        var button = new Button
        {
            Text = text,
            ThemeTypeVariation = MapTheme.Ghost,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(26f, 0f),
        };
        button.Pressed += () => Set(_value + (direction * Step));
        return button;
    }

    private void ApplyText()
    {
        string text = _field.Text.Trim().Replace(',', '.');
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal shown))
        {
            Set((int)Math.Round(shown * Divisor));
        }
        else
        {
            _field.Text = Format(_value);
        }
    }

    private void Set(int value)
    {
        int clamped = Math.Clamp(value, Minimum, Maximum);
        _field.Text = Format(clamped);
        if (clamped != _value)
        {
            _value = clamped;
            ValueChanged?.Invoke(clamped);
        }
    }

    private string Format(int value) => Divisor == 1
        ? value.ToString(CultureInfo.InvariantCulture)
        : (value / (decimal)Divisor).ToString("0.###", CultureInfo.InvariantCulture);
}
#endif
