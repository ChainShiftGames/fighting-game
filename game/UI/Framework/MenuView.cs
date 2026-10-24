using System;
using System.Collections.Generic;
using FightingGame.UI.Widgets;
using Godot;

namespace FightingGame.UI.Framework;

/// <summary>
/// Draws the top screen of the menu stack with the shared theme: a backdrop, the title, the body text and one widget
/// per item, fading in when the screen changes (<see cref="MenuTransition"/>). Widgets are rebuilt only when the
/// kinds of items change, so text can update every frame.
/// </summary>
public partial class MenuView : CanvasLayer
{
    /// <summary>The shared menu theme: fonts, sizes, colours and the focus highlight.</summary>
    public const string ThemePath = "res://Assets/UI/menu_theme.tres";

    // Above the match and the training menu, below the options menu.
    private const int MenuLayer = 6;
    private const float ItemWidth = 420f;

    // Over a match the column sits in the lower half, under the KO / winner banner.
    private const float OverMatchTop = 0.45f;

    private readonly Panel _backdrop = new() { AnchorRight = 1f, AnchorBottom = 1f };
    private readonly CenterContainer _centre = new() { AnchorRight = 1f, AnchorBottom = 1f };
    private readonly Label _title = new()
    {
        ThemeTypeVariation = "MenuTitle",
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    private readonly Label _body = new() { ThemeTypeVariation = "MenuBody", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _items = new() { ThemeTypeVariation = "MenuItems" };
    private readonly List<IMenuWidget> _widgets = [];
    private MenuTextFormatter? _text;
    private MenuTransition? _transition;
    private IMenuScreen? _shown;

    /// <summary>The mouse moved over the item at this index.</summary>
    public event Action<int>? Pointed;

    /// <summary>The item at this index was clicked.</summary>
    public event Action<int>? Clicked;

    public void Bind(MenuTextFormatter text, MenuTransition transition)
    {
        _text = text;
        _transition = transition;
    }

    public override void _Ready()
    {
        Layer = MenuLayer;
        Visible = false;

        // Menu text is translated by the formatter; Godot translating it again would turn names into keys' texts.
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        var root = new Control { AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (ResourceLoader.Exists(ThemePath))
        {
            root.Theme = GD.Load<Theme>(ThemePath);
        }

        AddChild(root);
        root.AddChild(_backdrop);
        root.AddChild(_centre);
        var column = new VBoxContainer { ThemeTypeVariation = "MenuColumn" };
        column.AddChild(_title);
        column.AddChild(_body);
        column.AddChild(_items);
        _centre.AddChild(column);
    }

    /// <summary>Shows the screen as it is now, or hides the menus when it is null; call once per displayed frame.</summary>
    public void Show(IMenuScreen? screen, MenuFocus? focus, long nowMilliseconds)
    {
        if (screen is null || _text is null)
        {
            Visible = false;
            _shown = null;
            return;
        }

        if (!ReferenceEquals(screen, _shown))
        {
            _shown = screen;
            _transition!.Start(nowMilliseconds);
        }

        Visible = true;
        _backdrop.ThemeTypeVariation = screen.IsOverMatch ? "MenuOverlayBackdrop" : "MenuScreenBackdrop";
        _centre.AnchorTop = screen.IsOverMatch ? OverMatchTop : 0f;
        _centre.Modulate = new Color(1f, 1f, 1f, _transition!.Opacity(nowMilliseconds));
        _title.Text = _text.Format(screen.Title);
        _title.Visible = !screen.Title.IsEmpty;
        _body.Text = _text.FormatLines(screen.Body);
        _body.Visible = _body.Text.Length > 0;
        ShowItems(screen.Items, focus?.Settle(screen.Items) ?? -1);
    }

    private void ShowItems(IReadOnlyList<MenuItem> items, int focused)
    {
        if (!FitsWidgets(items))
        {
            Rebuild(items);
        }

        for (int i = 0; i < items.Count; i++)
        {
            _widgets[i].Show(items[i], i == focused, _text!);
        }
    }

    private bool FitsWidgets(IReadOnlyList<MenuItem> items)
    {
        if (items.Count != _widgets.Count)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (!_widgets[i].Draws(items[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void Rebuild(IReadOnlyList<MenuItem> items)
    {
        foreach (IMenuWidget widget in _widgets)
        {
            _items.RemoveChild(widget.Control);
            widget.Control.QueueFree();
        }

        _widgets.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            IMenuWidget widget = MenuWidgets.For(items[i]);
            widget.Control.CustomMinimumSize = new Vector2(ItemWidth, 0f);
            widget.Listen(() => Pointed?.Invoke(index), () => Clicked?.Invoke(index));
            _widgets.Add(widget);
            _items.AddChild(widget.Control);
        }
    }
}
