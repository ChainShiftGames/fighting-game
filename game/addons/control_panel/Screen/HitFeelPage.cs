#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FightingGame.Addons.CharacterEditor.Screen.Drawer;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;
using Sim.Math;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The Hit feel page: every hit of the picked character's moves in one table (damage, hitstop, hitstun, blockstun,
/// pushback), editable in place, with each move's frame advantage, and a bar that shifts a whole column at once.
/// </summary>
/// <remarks>Pushback is shown and typed in pixels; the file keeps subpixels.</remarks>
[Tool]
public partial class HitFeelPage : VBoxContainer
{
    private const int MoveColumn = 0;
    private const int CategoryColumn = 1;
    private const int FirstValueColumn = 2;
    private const string AllCategories = "All moves";

    private readonly SectionTitle _heading = new();
    private string _title = string.Empty;
    private readonly Label _description = new() { ThemeTypeVariation = MapTheme.MutedLabel };
    private readonly CharacterPicker _picker = new();
    private readonly OptionButton _filter = new();
    private readonly Tree _table = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HideRoot = true };
    private readonly BulkEditBar _bulk = new();
    private readonly List<(TreeItem Item, HitRow Row)> _shown = [];
    private IReadOnlyList<HitRow> _rows = [];
    private PanelDocument? _document;
    private PanelLook? _look;
    private SettingTooltips? _tooltips;
    private string? _character;
    private bool _filling;

    private static int OnHitColumn => FirstValueColumn + HitColumn.All.Count;

    private static int OnBlockColumn => OnHitColumn + 1;

    public HitFeelPage()
    {
        AddChild(_heading);
        AddChild(_description);
        var top = new HBoxContainer();
        top.AddChild(_picker);
        top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        top.AddChild(new Label
        {
            Text = "SHOW",
            ThemeTypeVariation = MapTheme.Section,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Show only one category of moves; the change-all bar then applies to those.",
            MouseFilter = MouseFilterEnum.Stop,
        });
        top.AddChild(_filter);
        AddChild(top);
        var bulkCard = new PanelContainer { ThemeTypeVariation = MapTheme.Card };
        bulkCard.AddChild(_bulk);
        AddChild(bulkCard);
        AddChild(_table);
        _picker.Picked += id =>
        {
            _character = id;
            ShowRows();
        };
        _filter.ItemSelected += _ => ShowTable();
        _table.ItemEdited += OnCellEdited;
        _bulk.Applied += ApplyBulk;
    }

    public void Build(PanelDocument document, PanelLook look, SettingTooltips tooltips)
    {
        _document = document;
        _look = look;
        _tooltips = tooltips;
        _filter.TooltipText = "Show only one category of moves; the change-all bar then applies to those.";
        _table.Columns = OnBlockColumn + 1;
        _table.ColumnTitlesVisible = true;
        _table.SetColumnTitle(MoveColumn, "Move");
        _table.SetColumnTitle(CategoryColumn, "Category");
        _table.SetColumnTitleTooltipText(MoveColumn, "The move's ID; #n is the hit number of a multi-hit move.");
        _table.SetColumnTitleTooltipText(CategoryColumn, "normal, special, super or throw.");
        for (int i = 0; i < HitColumn.All.Count; i++)
        {
            HitColumn column = HitColumn.All[i];
            string title = column.Unit == SettingUnit.Subpixels ? $"{column.Label} (px)" : column.Label;
            _table.SetColumnTitle(FirstValueColumn + i, title);
            _table.SetColumnTitleTooltipText(FirstValueColumn + i, column.Hint);
        }

        _table.SetColumnTitle(OnHitColumn, "On hit");
        _table.SetColumnTitle(OnBlockColumn, "On block");
        _table.SetColumnTitleTooltipText(OnHitColumn, "Frame advantage after the move's last hit lands on its first frame.");
        _table.SetColumnTitleTooltipText(OnBlockColumn, "Frame advantage after the move's last hit is blocked.");
        _bulk.Build(look);
    }

    /// <param name="description">One line under the page's heading.</param>
    public void SetHeading(string title, string description)
    {
        _title = title;
        _heading.Show(title);
        _description.Text = description;
    }

    /// <summary>Lists the characters on disk again and shows the picked one's hits.</summary>
    public void Fill(string dataRoot)
    {
        _picker.Fill(dataRoot, _character);
        _character = _picker.Current;
        ShowRows();
    }

    /// <summary>Shows the document's values in the table's cells.</summary>
    public void Refresh()
    {
        _filling = true;
        foreach ((TreeItem item, HitRow row) in _shown)
        {
            for (int i = 0; i < HitColumn.All.Count; i++)
            {
                ShowCell(item, FirstValueColumn + i, HitColumn.All[i], row);
            }

            (int? onHit, int? onBlock) = row.IsLastHit ? MoveAdvantage.Of(_document!, row.File) : (null, null);
            ShowAdvantage(item, OnHitColumn, row.IsLastHit ? onHit : null, row.IsLastHit);
            ShowAdvantage(item, OnBlockColumn, row.IsLastHit ? onBlock : null, row.IsLastHit);
        }

        _filling = false;
    }

    private void ShowRows()
    {
        try
        {
            _rows = _character is null ? [] : HitFeelRows.For(_document!, _character);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            _rows = [];
            GD.PushWarning($"Control panel: no hit table for {_character}: {exception.Message}");
        }

        string kept = _filter.Selected >= 0 ? _filter.GetItemText(_filter.Selected) : AllCategories;
        _filter.Clear();
        _filter.AddItem(AllCategories);
        var categories = new List<string>();
        foreach (HitRow row in _rows)
        {
            if (!categories.Contains(row.Category))
            {
                categories.Add(row.Category);
                _filter.AddItem(row.Category);
            }
        }

        _filter.Select(Math.Max(0, categories.IndexOf(kept) + 1));
        ShowTable();
    }

    private void ShowTable()
    {
        _table.Clear();
        _shown.Clear();
        TreeItem root = _table.CreateItem();
        string category = _filter.Selected > 0 ? _filter.GetItemText(_filter.Selected) : string.Empty;
        foreach (HitRow row in _rows)
        {
            if (category.Length > 0 && row.Category != category)
            {
                continue;
            }

            TreeItem item = _table.CreateItem(root);
            item.SetText(MoveColumn, row.Label);
            item.SetText(CategoryColumn, row.Category);
            item.SetTooltipText(MoveColumn, $"{row.File}: hit {row.HitIndex + 1} of {row.HitCount}");
            item.SetTooltipText(CategoryColumn, "normal, special, super or throw.");
            item.SetTooltipText(OnHitColumn, _table.GetColumnTitleTooltipText(OnHitColumn));
            item.SetTooltipText(OnBlockColumn, _table.GetColumnTitleTooltipText(OnBlockColumn));
            for (int i = 0; i < HitColumn.All.Count; i++)
            {
                item.SetTooltipText(FirstValueColumn + i, _tooltips!.For(HitColumn.All[i].For(row)));
            }
            _shown.Add((item, row));
        }

        _bulk.ShowCount(_shown.Count);
        _heading.Show(_title, $"{_shown.Count} of {_rows.Count} hits shown");
        Refresh();
    }

    // Plus frames read good, minus frames bad, as on the Character tab's move facts.
    private void ShowAdvantage(TreeItem item, int cell, int? frames, bool shown)
    {
        item.SetText(cell, shown ? MoveAdvantage.Text(frames) : string.Empty);
        item.SetCustomFont(cell, _look!.Mono);
        item.SetTextAlignment(cell, HorizontalAlignment.Center);
        item.SetCustomColor(cell, frames switch
        {
            > 0 => MapPalette.Good,
            < 0 => MapPalette.Bad,
            _ => MapPalette.Muted,
        });
    }

    private void ShowCell(TreeItem item, int cell, HitColumn column, HitRow row)
    {
        item.SetCustomFont(cell, _look!.Mono);
        PanelSetting setting = column.For(row);
        if (!_document!.Has(setting))
        {
            item.SetCellMode(cell, TreeItem.TreeCellMode.String);
            item.SetText(cell, "—");
            item.SetEditable(cell, false);
            return;
        }

        double scale = ScaleOf(column);
        item.SetCellMode(cell, TreeItem.TreeCellMode.Range);
        item.SetRangeConfig(cell, setting.Min / scale, setting.Max / scale, 1);
        item.SetRange(cell, _document.Number(setting) / scale);
        item.SetEditable(cell, true);
        item.SetCustomColor(cell, _document.IsChanged(setting) ? _look.Changed : MapPalette.Text);
    }

    private void OnCellEdited()
    {
        TreeItem? edited = _table.GetEdited();
        int cell = _table.GetEditedColumn() - FirstValueColumn;
        if (_filling || edited is null || cell < 0 || cell >= HitColumn.All.Count)
        {
            return;
        }

        foreach ((TreeItem item, HitRow row) in _shown)
        {
            if (item == edited)
            {
                HitColumn column = HitColumn.All[cell];
                _document!.SetNumber(column.For(row), edited.GetRange(FirstValueColumn + cell) * ScaleOf(column));
                return;
            }
        }
    }

    private void ApplyBulk(int columnIndex, BulkEditBar.Change change, double amount)
    {
        HitColumn column = HitColumn.All[columnIndex];
        var rows = new List<HitRow>(_shown.Count);
        foreach ((TreeItem _, HitRow row) in _shown)
        {
            rows.Add(row);
        }

        if (change == BulkEditBar.Change.Scale)
        {
            HitColumnEdit.Scale(_document!, rows, column, amount);
        }
        else
        {
            HitColumnEdit.Add(_document!, rows, column, amount * ScaleOf(column));
        }
    }

    private static double ScaleOf(HitColumn column) => column.Unit == SettingUnit.Subpixels ? Subpixels.PerPixel : 1;
}
#endif
