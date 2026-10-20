#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Screen.Drawer;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// A scrolling page of cards: a heading, then the setting groups (and any extra cards, e.g. a readout) spread over as
/// many columns as fit, each column a stack of cards, packed so the columns end as evenly as possible.
/// </summary>
/// <remarks>
/// Cards are measured after the first layout and repacked then, and again when the width changes the column count,
/// so the packing uses their real heights (wrapped hints included).
/// </remarks>
[Tool]
public partial class SettingsPage : ScrollContainer
{
    private const float CardWidth = 420f;
    private const float Spacing = 14f;
    private const int MaxColumns = 4;

    private readonly VBoxContainer _page = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly SectionTitle _heading = new();
    private readonly Label _description = new()
    {
        ThemeTypeVariation = MapTheme.MutedLabel,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private readonly HBoxContainer _columns = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly List<SettingGroupView> _views = [];
    private readonly List<Control> _cards = [];
    private PanelDocument? _document;
    private PanelLook? _look;
    private SettingTooltips? _tooltips;
    private string _title = string.Empty;
    private int _columnCount;
    private bool _layoutQueued;

    public SettingsPage()
    {
        HorizontalScrollMode = ScrollMode.Disabled;
        _page.AddChild(_heading);
        _page.AddChild(_description);
        _page.AddChild(_columns);
        AddChild(_page);
        Resized += OnResized;
    }

    /// <summary>A control shown between the heading and the cards, e.g. a character picker.</summary>
    public void AddTop(Control control)
    {
        _page.AddChild(control);
        _page.MoveChild(control, _columns.GetIndex());
    }

    public void Build(PanelDocument document, PanelLook look, SettingTooltips tooltips)
    {
        _document = document;
        _look = look;
        _tooltips = tooltips;
        _page.AddThemeConstantOverride("separation", look.Px(10f));
        _columns.AddThemeConstantOverride("separation", look.Px(Spacing));
    }

    /// <param name="description">One line under the page's heading.</param>
    public void SetHeading(string title, string description)
    {
        _title = title;
        _heading.Show(title);
        _description.Text = description;
    }

    /// <summary>Replaces the cards shown, e.g. for another character.</summary>
    /// <param name="extraCards">Cards placed before the groups, e.g. a readout; they keep their own content.</param>
    public void Show(IReadOnlyList<SettingGroup> groups, IReadOnlyList<Control>? extraCards = null)
    {
        foreach (Control card in _cards)
        {
            card.GetParent()?.RemoveChild(card);
            if (card is SettingGroupView)
            {
                card.QueueFree();
            }
        }

        _views.Clear();
        _cards.Clear();
        _cards.AddRange(extraCards ?? []);
        foreach (SettingGroup group in groups)
        {
            var view = new SettingGroupView();
            view.Build(group, _document!, _look!, _tooltips!);
            _views.Add(view);
            _cards.Add(view);
        }

        _columnCount = 0;
        Layout();
        Refresh();
        QueueLayout();
    }

    public void Refresh()
    {
        int switches = 0;
        int on = 0;
        foreach (SettingGroupView view in _views)
        {
            view.Refresh();
            switches += view.HasSwitch ? 1 : 0;
            on += view.IsOn ? 1 : 0;
        }

        _heading.Show(_title, switches > 0 ? $"{on} of {switches} on" : string.Empty);
    }

    private void OnResized()
    {
        if (ColumnCount() != _columnCount)
        {
            QueueLayout();
        }
    }

    // After layout the cards have their real heights: pack again with those.
    private void QueueLayout()
    {
        if (_layoutQueued)
        {
            return;
        }

        _layoutQueued = true;
        Callable.From(() =>
        {
            _layoutQueued = false;
            Layout();
        }).CallDeferred();
    }

    private void Layout()
    {
        int count = ColumnCount();
        var heights = new float[_cards.Count];
        for (int i = 0; i < _cards.Count; i++)
        {
            heights[i] = Height(_cards[i]);
        }

        List<int>[] packed = ColumnPacking.Pack(heights, count);
        foreach (Node column in _columns.GetChildren())
        {
            foreach (Node card in column.GetChildren())
            {
                column.RemoveChild(card);
            }

            _columns.RemoveChild(column);
            column.QueueFree();
        }

        foreach (List<int> cards in packed)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = 1f,
            };
            column.AddThemeConstantOverride("separation", _look?.Px(Spacing) ?? (int)Spacing);
            foreach (int card in cards)
            {
                column.AddChild(_cards[card]);
            }

            _columns.AddChild(column);
        }

        _columnCount = count;
    }

    private int ColumnCount()
    {
        float scale = _look?.Scale ?? 1f;
        float width = Size.X > 0f ? Size.X : CardWidth * scale;
        return ColumnPacking.ColumnsFor(width, CardWidth * scale, Spacing * scale, MaxColumns);
    }

    // Before the first layout a card has no size yet: its row count is a good stand-in.
    private float Height(Control card)
    {
        float measured = card.IsInsideTree() ? card.GetCombinedMinimumSize().Y : 0f;
        return measured > 0f ? measured : card is SettingGroupView view ? view.EstimatedRows * 33f : 150f;
    }
}
#endif
