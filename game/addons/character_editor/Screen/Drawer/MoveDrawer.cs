#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// The panel on the right that edits the selected move: a preview of the frame with its boxes and a point-blank
/// opponent, the move's plain name and input, and four tabs (Overview, Timing, Hit areas, Chains). It can show the
/// character file instead (stats and states).
/// </summary>
[Tool]
public partial class MoveDrawer : PanelContainer
{
    private const float PreviewHeight = 236f;
    private const int HitAreasTab = 2;
    private static readonly string[] _tabNames = ["Overview", "Timing", "Hit areas", "Chains"];

    private readonly Control _preview = new() { ClipContents = true };
    private readonly Label _title = new() { ThemeTypeVariation = MapTheme.Heading, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
    private readonly InputLine _subtitle = new();
    private readonly UnderlineTabs _tabs = new();
    private readonly OverviewPage _overview = new();
    private readonly TimingPage _timing = new();
    private readonly HitAreasPage _hitAreas = new();
    private readonly ChainsPage _chains = new();
    private readonly CharacterPanel _character = new();
    private readonly Control[] _pages;
    private DrawerMove? _move;

    public MoveDrawer()
    {
        _pages = [_overview, _timing, _hitAreas, _chains];
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        BoxCanvas canvas = _hitAreas.Canvas;
        canvas.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _preview.AddChild(canvas);
        column.AddChild(_preview);
        column.AddChild(Body());
        AddChild(column);

        _tabs.SetTabs(_tabNames);
        _tabs.TabChosen += ShowTab;
        canvas.Clicked += () => _tabs.Open(HitAreasTab);
        canvas.Resized += canvas.ResetView;
        _hitAreas.FrameShown += frame =>
        {
            ShowOpponent();
            _timing.MarkFrame(frame);
            FrameShown?.Invoke(frame);
        };
        _timing.FrameClicked += _hitAreas.SelectFrame;
        foreach (IDrawerPage page in new IDrawerPage[] { _overview, _timing, _hitAreas, _chains })
        {
            IDrawerPage source = page;
            page.Changed += rangesMoved => OnPageChanged(source, rangesMoved);
        }

        _character.Visible = false;
        _character.Edited += () => Edited?.Invoke(false);
        ShowTab(0);
    }

    /// <summary>The move's or the character's file changed; <c>true</c> when the map's cards may have changed too.</summary>
    public event Action<bool>? Edited;

    /// <summary>The preview shows another frame.</summary>
    public event Action<int>? FrameShown;

    public int Frame => _hitAreas.Frame;

    public override void _Ready() => _preview.CustomMinimumSize = new Vector2(0f, MapLook.Of(this).Px(PreviewHeight));

    /// <summary>Loads the character's model into the preview.</summary>
    public void ShowCharacter(string? characterId, IReadOnlyList<string> moveIds) =>
        _hitAreas.ShowCharacter(characterId, moveIds);

    /// <summary>Edits the move; nothing when null.</summary>
    public void ShowMove(DrawerMove? move)
    {
        _move = move;
        _character.Visible = false;
        _tabs.Visible = move is not null;
        _preview.Visible = move is not null;
        if (move is null)
        {
            _title.Text = "No move selected";
            _subtitle.ShowLine("Click a card on the map.", "", [], "");
            ShowTab(-1);
            return;
        }

        foreach (IDrawerPage page in new IDrawerPage[] { _overview, _timing, _hitAreas, _chains })
        {
            page.ShowMove(move);
        }

        ShowHeader(move);
        ShowOpponent();
        ShowTab(_tabs.Current);
    }

    /// <summary>Edits the character file instead of a move.</summary>
    public void ShowCharacterFile(CharacterDocument document, EditingContext context)
    {
        _move = null;
        _tabs.Visible = _preview.Visible = false;
        ShowTab(-1);
        _character.Visible = true;
        _title.Text = JsonNodeValue.Text(document.Character.Root["displayName"]) ?? document.Id;
        _subtitle.ShowLine("Character file: stats, states and roles", "", [], "");
        _character.ShowCharacter(document.Character, context);
    }

    /// <summary>Shows the frame in the preview, e.g. one clicked on the frame strip.</summary>
    public void SelectFrame(int frame) => _hitAreas.SelectFrame(frame);

    private Control Body()
    {
        var margin = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        foreach ((string side, int pixels) in new[] { ("left", 18), ("right", 18), ("top", 16), ("bottom", 8) })
        {
            margin.AddThemeConstantOverride("margin_" + side, pixels);
        }

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 14);
        var header = new VBoxContainer();
        header.AddThemeConstantOverride("separation", 2);
        header.AddChild(_title);
        header.AddChild(_subtitle);
        body.AddChild(header);
        body.AddChild(_tabs);
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var pages = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (Control page in _pages)
        {
            page.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            pages.AddChild(page);
        }

        pages.AddChild(_character);
        scroll.AddChild(pages);
        body.AddChild(scroll);
        margin.AddChild(body);
        return margin;
    }

    private void ShowTab(int index)
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].Visible = i == index;
        }

        _hitAreas.Canvas.Editable = index == HitAreasTab;
        if (_move is not null && index >= 0)
        {
            // Another tab may have changed what this one shows (the settings form edits projectiles too).
            ((IDrawerPage)_pages[index]).ShowMove(_move);
        }
    }

    private void OnPageChanged(IDrawerPage source, bool rangesMoved)
    {
        DrawerMove move = _move!;
        move.File.MarkChanged();
        if (rangesMoved)
        {
            foreach (IDrawerPage page in new IDrawerPage[] { _overview, _timing, _hitAreas, _chains })
            {
                if (page != source || source == _overview)
                {
                    page.ShowMove(move);
                }
            }
        }
        else
        {
            _overview.ShowFacts(move);
        }

        ShowHeader(move);
        ShowOpponent();
        Edited?.Invoke(true);
    }

    private void ShowHeader(DrawerMove move)
    {
        _title.Text = move.Name;
        if (move.IsBasic)
        {
            _subtitle.ShowLine("Plays on its own", "", [], $"· {move.Id}");
            return;
        }

        string what = move.Category switch
        {
            "normal" => JsonNodeValue.Text(move.Root[MoveFields.UsableIn]) == "air" ? "air attack" : "ground attack",
            "special" => "special move",
            "throw" => "throw",
            _ => move.Category,
        };
        _subtitle.ShowLine("Press", MoveGlyphs.Arrows(MoveInput.Motion(move.Root)),
            MoveGlyphs.Keycaps(MoveInput.Buttons(move.Root)), $"· {what}");
    }

    private void ShowOpponent()
    {
        BoxCanvas canvas = _hitAreas.Canvas;
        canvas.Opponent = _move is null ? [] : PointBlankOpponent.Of(_move.Root, _hitAreas.Frame, Idle(_move));
        canvas.QueueRedraw();
    }

    private static System.Text.Json.Nodes.JsonObject? Idle(DrawerMove move) =>
        JsonNodeValue.Text(move.Document.Character.Root["states"]?["idle"]) is string idle
            ? move.Document.Move(idle)?.Root
            : null;
}
#endif
