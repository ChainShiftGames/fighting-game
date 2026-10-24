using System;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>
/// What every focusable widget shares: a themed row with the item's label on the left (its description, if any,
/// under it), the focus highlight, and mouse pointing and clicking. Widgets with a value put it in
/// <see cref="Value"/> on the right.
/// </summary>
public abstract partial class MenuRow : PanelContainer, IMenuWidget
{
    private const string RowStyle = "MenuItem";
    private const string FocusedRowStyle = "MenuItemFocused";
    private const string LabelStyle = "MenuItemLabel";
    private const string DisabledLabelStyle = "MenuItemLabelDisabled";
    private const string DescriptionStyle = "MenuItemDescription";

    private readonly Label _label = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly Label _description = new() { ThemeTypeVariation = DescriptionStyle, Visible = false };
    private Action? _pointed;
    private Action? _clicked;

    protected MenuRow()
    {
        var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        line.AddChild(_label);
        line.AddChild(Value);
        var lines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        lines.AddChild(line);
        lines.AddChild(_description);
        AddChild(lines);
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += () => _pointed?.Invoke();
        GuiInput += OnGuiInput;
    }

    public Control Control => this;

    /// <summary>Holds the widget's value on the right of the row; empty for a plain button.</summary>
    protected HBoxContainer Value { get; } = new() { MouseFilter = MouseFilterEnum.Ignore };

    public abstract bool Draws(MenuItem item);

    public void Show(MenuItem item, bool isFocused, MenuTextFormatter text)
    {
        ThemeTypeVariation = isFocused ? FocusedRowStyle : RowStyle;
        _label.ThemeTypeVariation = item.Enabled ? LabelStyle : DisabledLabelStyle;
        _label.Text = text.Format(item.Label);
        _description.Text = text.Format(item.Description);
        _description.Visible = !item.Description.IsEmpty;
        ShowValue(item, text);
    }

    public void Listen(Action pointed, Action clicked)
    {
        _pointed = pointed;
        _clicked = clicked;
    }

    /// <summary>Fills <see cref="Value"/> for the item.</summary>
    protected abstract void ShowValue(MenuItem item, MenuTextFormatter text);

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            _clicked?.Invoke();
        }
    }
}
