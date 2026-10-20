#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>
/// The whole character on one canvas: moves as cards in columns, arrows for what can chain into what, the selected
/// move's arrows lit. Click a card to edit it, right-click for its menu; drag empty space (or right/middle-drag)
/// to pan, the wheel zooms, the corner buttons zoom and fit.
/// </summary>
[Tool]
public partial class MoveMapCanvas : Control
{
    private const float MinZoom = 0.35f;
    private const float MaxZoom = 2.2f;
    private const float ZoomStep = 1.12f;

    private static readonly Dictionary<string, string> _groupLabels = new(StringComparer.Ordinal)
    {
        ["special"] = "Any special move",
        ["super"] = "Any super",
        ["throw"] = "Any throw",
    };

    private readonly PopupMenu _menu = new();
    private MapPlacement? _placement;
    private Dictionary<string, MapCard> _cards = [];
    private IReadOnlyList<ChainLink> _links = [];
    private List<MapEdges.Edge> _edges = [];
    private HashSet<string> _related = [];
    private string? _selected;
    private string? _hovered;
    private string? _menuMove;
    private Vector2 _offset;
    private float _zoom = 1f;
    private bool _panning;
    private bool _fitPending = true;

    public MoveMapCanvas()
    {
        ClipContents = true;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseDefaultCursorShape = CursorShape.Arrow;
        AddChild(_menu);
        AddChild(new MapLegend());
        AddChild(ZoomButtons());
        _menu.AddItem("New move after this one…", (int)MapMoveAction.NewAfter);
        _menu.AddItem("Duplicate…", (int)MapMoveAction.Duplicate);
        _menu.AddSeparator();
        _menu.AddItem("Check this input earlier (raise priority)", (int)MapMoveAction.RaisePriority);
        _menu.AddItem("Check this input later (lower priority)", (int)MapMoveAction.LowerPriority);
        _menu.AddSeparator();
        _menu.AddItem("Delete…", (int)MapMoveAction.Delete);
        _menu.IdPressed += id => MoveAction?.Invoke(_menuMove!, (MapMoveAction)(int)id);
        Resized += () =>
        {
            if (_fitPending)
            {
                Fit();
            }
        };
    }

    /// <summary>A move's card was clicked.</summary>
    public event Action<string>? MoveSelected;

    /// <summary>An item of a card's menu was picked.</summary>
    public event Action<string, MapMoveAction>? MoveAction;

    /// <param name="refit">Fit the view to the map, e.g. for a new character or lens.</param>
    public void ShowMap(MapPlacement placement, Dictionary<string, MapCard> cards, IReadOnlyList<ChainLink> links,
        string? selected, bool refit)
    {
        _placement = placement;
        _cards = cards;
        _links = links;
        Select(selected);
        if (refit)
        {
            _fitPending = true;
            Fit();
        }
    }

    /// <summary>Lights the move's arrows; null for none.</summary>
    public void Select(string? selected)
    {
        _selected = selected;
        _edges = _placement is null ? [] : MapEdges.Of(_placement, _links, selected);
        _related = Related(selected);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), MapPalette.Background);
        MapLook look = MapLook.Of(this);
        var painter = new MapPainter(this, look with { Scale = look.Scale * _zoom }, _offset);
        painter.Grid(Size);
        if (_placement is null)
        {
            return;
        }

        painter.Headers(_placement);
        foreach ((string category, Rect2 box) in _placement.Groups)
        {
            painter.Group(box, _groupLabels.GetValueOrDefault(category, category));
        }

        // Lit arrows last, so they cross over the others.
        foreach (MapEdges.Edge edge in _edges)
        {
            if (!edge.Hot)
            {
                painter.Edge(edge);
            }
        }

        foreach (MapEdges.Edge edge in _edges)
        {
            if (edge.Hot)
            {
                painter.Edge(edge);
            }
        }

        foreach ((string entry, Rect2 card) in _placement.Cards)
        {
            if (_cards.TryGetValue(entry, out MapCard? content))
            {
                bool dim = _selected is not null && entry != _selected && !_related.Contains(entry);
                painter.Card(card, content, entry == _selected, dim, entry == _hovered);
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel:
                Zoom(wheel.Position, wheel.ButtonIndex == MouseButton.WheelUp ? ZoomStep : 1f / ZoomStep);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                if (CardAt(click.Position) is string clicked)
                {
                    if (clicked != MoveMapLayout.JumpEntry)
                    {
                        MoveSelected?.Invoke(clicked);
                    }
                }
                else
                {
                    _panning = true;
                }

                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } right
                when CardAt(right.Position) is string card && card != MoveMapLayout.JumpEntry:
                _menuMove = card;
                _menu.Position = (Vector2I)(GetScreenTransform() * right.Position);
                _menu.Popup();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right or MouseButton.Middle } button:
                _panning = button.Pressed;
                AcceptEvent();
                break;
            case InputEventMouseMotion motion:
                if (_panning)
                {
                    _offset += motion.Relative;
                    _fitPending = false;
                    QueueRedraw();
                }

                Hover(CardAt(motion.Position));
                break;
        }
    }

    public override string _GetTooltip(Vector2 atPosition) =>
        CardAt(atPosition) is string entry && _cards.TryGetValue(entry, out MapCard? card) ? card.Tooltip : "";

    private Control ZoomButtons()
    {
        var row = new HBoxContainer();
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.KeepSize, 14);
        row.GrowHorizontal = GrowDirection.Begin;
        row.GrowVertical = GrowDirection.Begin;
        row.AddThemeConstantOverride("separation", 4);
        foreach ((string text, string tip, Action action) in new (string, string, Action)[]
        {
            ("−", "Zoom out", () => Zoom(Size / 2f, 1f / ZoomStep)),
            ("+", "Zoom in", () => Zoom(Size / 2f, ZoomStep)),
            ("⤢", "Fit the whole map", Fit),
        })
        {
            var button = new Button
            {
                Text = text,
                TooltipText = tip,
                ThemeTypeVariation = MapTheme.Ghost,
                CustomMinimumSize = new Vector2(30f, 30f),
                FocusMode = FocusModeEnum.None,
            };
            button.Pressed += action;
            row.AddChild(button);
        }

        return row;
    }

    private void Fit()
    {
        if (_placement is null || Size.X < 1f || Size.Y < 1f)
        {
            return;
        }

        float scale = MapLook.Of(this).Scale;
        Rect2 bounds = _placement.Bounds;
        _zoom = Mathf.Clamp(Mathf.Min(Size.X / (bounds.Size.X * scale), Size.Y / (bounds.Size.Y * scale)), MinZoom, 1f);
        Vector2 drawn = bounds.Size * scale * _zoom;
        _offset = new Vector2(Mathf.Max(0f, (Size.X - drawn.X) / 2f), 0f) - (bounds.Position * scale * _zoom);
        QueueRedraw();
    }

    private void Zoom(Vector2 around, float factor)
    {
        float zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
        _offset = around + ((_offset - around) * (zoom / _zoom));
        _zoom = zoom;
        _fitPending = false;
        QueueRedraw();
    }

    private void Hover(string? entry)
    {
        if (entry != _hovered)
        {
            _hovered = entry;
            MouseDefaultCursorShape = entry is null || entry == MoveMapLayout.JumpEntry
                ? CursorShape.Arrow
                : CursorShape.PointingHand;
            QueueRedraw();
        }
    }

    private string? CardAt(Vector2 position)
    {
        if (_placement is null)
        {
            return null;
        }

        float scale = MapLook.Of(this).Scale * _zoom;
        Vector2 design = (position - _offset) / scale;
        foreach ((string entry, Rect2 card) in _placement.Cards)
        {
            if (card.HasPoint(design))
            {
                return entry;
            }
        }

        return null;
    }

    // The cards joined to the selected one by an arrow, either way; the rest are dimmed.
    private HashSet<string> Related(string? selected)
    {
        HashSet<string> related = new(StringComparer.Ordinal);
        if (selected is null || _placement is null)
        {
            return related;
        }

        _placement.Categories.TryGetValue(selected, out string? selectedCategory);
        foreach (ChainLink link in _links)
        {
            if (link.From == selected)
            {
                related.UnionWith(MapEdges.Targets(_placement, link));
                if (link.Kind == ChainTargetKind.Category)
                {
                    foreach (MapGroup group in _placement.Layout.Groups)
                    {
                        if (group.Category == link.Target)
                        {
                            related.UnionWith(group.Moves);
                        }
                    }
                }
            }
            else if ((link.Kind == ChainTargetKind.Move && link.Target == selected)
                || (link.Kind == ChainTargetKind.Category && link.Target == selectedCategory))
            {
                related.Add(link.From);
            }
        }

        return related;
    }
}
#endif
