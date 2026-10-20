#if TOOLS
using System;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The Hit feel page's "change a whole column" bar: pick a column, scale it by a percentage or add an amount, and
/// apply it to every hit the table shows.
/// </summary>
[Tool]
public partial class BulkEditBar : HBoxContainer
{
    private const double DefaultPercent = 100;
    private const double DefaultAmount = 1;
    private const double MaxPercent = 1000;
    private const double MaxAmount = 1000;

    private readonly OptionButton _column = new();
    private readonly OptionButton _change = new();
    private readonly SpinBox _amount = new() { Step = 1, SelectAllOnFocus = true };
    private readonly Button _apply = new();

    /// <summary>How the bar changes the column.</summary>
    public enum Change
    {
        /// <summary>Multiply by amount / 100.</summary>
        Scale,

        /// <summary>Add the amount (pixels for pushback).</summary>
        Add,
    }

    /// <summary>Raised with the column's index in <see cref="HitColumn.All"/>, the change and the amount.</summary>
    public event Action<int, Change, double>? Applied;

    public void Build(PanelLook look)
    {
        AddChild(new Label
        {
            Text = "CHANGE ALL SHOWN",
            ThemeTypeVariation = MapTheme.Section,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Change one column for every hit the table shows, in one step.",
            MouseFilter = MouseFilterEnum.Stop,
        });
        _column.TooltipText = "The column to change.";
        _change.TooltipText = "× percent multiplies every value (120 = 20 % more); + add adds to it (negative takes away).";
        _amount.TooltipText = "How much. Pushback is added in pixels. Results are rounded and kept in range.";
        _apply.TooltipText = "Changes the shown hits now; nothing is saved until Save (or Apply live).";
        foreach (HitColumn column in HitColumn.All)
        {
            _column.AddItem(column.Label);
        }

        _change.AddItem("× percent");
        _change.AddItem("+ add");
        _change.ItemSelected += _ => ResetAmount();
        _amount.CustomMinimumSize = new Vector2(look.Px(90f), 0f);
        _amount.GetLineEdit().AddThemeFontOverride("font", look.Mono);
        _apply.ThemeTypeVariation = MapTheme.Ghost;
        _apply.Pressed += () => Applied?.Invoke(_column.Selected, (Change)_change.Selected, _amount.Value);
        foreach (Control control in new Control[] { _column, _change, _amount, _apply })
        {
            AddChild(control);
        }

        _column.Select(1);
        _change.Select(0);
        ResetAmount();
    }

    public void ShowCount(int hits)
    {
        _apply.Text = $"Apply to {hits} hits";
        _apply.Disabled = hits == 0;
    }

    private void ResetAmount()
    {
        bool scale = _change.Selected == (int)Change.Scale;
        _amount.MinValue = scale ? 0 : -MaxAmount;
        _amount.MaxValue = scale ? MaxPercent : MaxAmount;
        _amount.Value = scale ? DefaultPercent : DefaultAmount;
        _amount.Suffix = scale ? "%" : string.Empty;
    }
}
#endif
