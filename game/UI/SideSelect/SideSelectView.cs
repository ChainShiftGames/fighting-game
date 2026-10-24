using System.Collections.Generic;
using System.Globalization;
using FightingGame.Input;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.SideSelect;

/// <summary>
/// Draws <see cref="SideSelectScreen"/> with the shared menu theme: three columns (player 1, not playing, player 2),
/// each device as a <see cref="DeviceToken"/> sliding between them, and the controls line. Only reads the sides.
/// </summary>
/// <remarks>The menus show it in place of <see cref="MenuView"/> while side select is on top.</remarks>
public partial class SideSelectView : CanvasLayer
{
    // The same layer as MenuView: above the match and the training menu, below the options menu.
    private const int MenuLayer = 6;
    private const float ColumnWidth = 330f;
    private const float ColumnGap = 28f;
    private const float BoardHeight = 400f;
    private const float HeaderHeight = 58f;
    private const int HeaderFontSize = 34;
    private const float TokenInset = 14f;
    private const float TokenHeight = 66f;
    private const float TokenGap = 10f;

    private static readonly Color _column = new(1f, 1f, 1f, 0.05f);
    private static readonly Color _middleBorder = new(1f, 1f, 1f, 0.2f);

    private readonly Label _title = new() { ThemeTypeVariation = "MenuTitle", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Control _board = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Label[] _headers = new Label[3];
    private readonly Label[] _footers = new Label[3];
    private readonly Dictionary<InputDeviceId, DeviceToken> _tokens = [];
    private readonly List<InputDeviceId> _gone = [];
    private readonly Label _hint = new() { ThemeTypeVariation = "MenuItemDescription", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Control _root = new()
    {
        AnchorRight = 1f,
        AnchorBottom = 1f,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private SideSelectSettings _settings = SideSelectSettings.Default;
    private MenuTextFormatter? _text;
    private MenuTransition? _transition;
    private SideSelectScreen? _shown;

    public void Bind(MenuTextFormatter text, MenuTransition transition, SideSelectSettings settings)
    {
        _text = text;
        _transition = transition;
        _settings = settings;
    }

    public override void _Ready()
    {
        Layer = MenuLayer;
        Visible = false;
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        if (ResourceLoader.Exists(MenuView.ThemePath))
        {
            _root.Theme = GD.Load<Theme>(MenuView.ThemePath);
        }

        AddChild(_root);
        _root.AddChild(new Panel { AnchorRight = 1f, AnchorBottom = 1f, ThemeTypeVariation = "MenuScreenBackdrop" });
        var centre = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        _root.AddChild(centre);
        var column = new VBoxContainer { ThemeTypeVariation = "MenuColumn" };
        centre.AddChild(column);
        column.AddChild(_title);
        _board.CustomMinimumSize = new Vector2((3f * ColumnWidth) + (2f * ColumnGap), BoardHeight);
        column.AddChild(_board);
        column.AddChild(_hint);
        for (int index = 0; index < 3; index++)
        {
            AddColumn(index);
        }
    }

    /// <summary>Shows the screen as it is now, or hides when it is null; call once per displayed frame.</summary>
    public void Show(SideSelectScreen? screen, long nowMilliseconds)
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
        _root.Modulate = new Color(1f, 1f, 1f, _transition!.Opacity(nowMilliseconds));
        DeviceSides sides = screen.Sides;
        _title.Text = _text.Format(screen.Title);
        ShowColumns(sides);
        ShowTokens(sides, nowMilliseconds);
        _hint.Text = _text.Format(MenuText.Key(Hint(sides)));
    }

    private static string Hint(DeviceSides sides) =>
        sides.IsAllReady ? "MENU_SIDES_ALL_READY"
        : sides.Devices.Count < sides.Rules.PlayerCount ? "MENU_SIDES_NEED_CONTROLLER"
        : "MENU_SIDES_HINT";

    private void AddColumn(int index)
    {
        var column = (SideColumn)index;
        int side = SideSelectColumns.SideOf(column);
        Color border = side < 0 ? _middleBorder : SelectStyles.Of(side);
        var panel = new Panel
        {
            Position = new Vector2(index * (ColumnWidth + ColumnGap), 0f),
            Size = new Vector2(ColumnWidth, BoardHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", SelectStyles.Box(_column, border, 2, 0f));
        _board.AddChild(panel);

        var header = _headers[index] = new Label
        {
            ThemeTypeVariation = "MenuTitle",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(ColumnWidth, HeaderHeight),
        };
        header.AddThemeFontSizeOverride("font_size", HeaderFontSize);
        if (side >= 0)
        {
            header.AddThemeColorOverride("font_color", border);
        }

        panel.AddChild(header);
        var footer = _footers[index] = new Label
        {
            ThemeTypeVariation = "MenuItemDescription",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Position = new Vector2(TokenInset, HeaderHeight + TokenHeight + TokenGap),
            Size = new Vector2(ColumnWidth - (2f * TokenInset), TokenHeight),
        };
        panel.AddChild(footer);
    }

    private void ShowColumns(DeviceSides sides)
    {
        for (int index = 0; index < 3; index++)
        {
            int side = SideSelectColumns.SideOf((SideColumn)index);
            _headers[index].Text = side < 0
                ? _text!.Format(MenuText.Key("MENU_SIDES_NOT_PLAYING"))
                : _text!.Format(MenuText.Key("MENU_SELECT_PLAYER", (side + 1).ToString(CultureInfo.InvariantCulture)));
            _footers[index].Text = side < 0 ? string.Empty : _text.Format(Footer(sides, side));
        }
    }

    // Under the side's device: what to do there next; nothing once it is ready (its token says so).
    private static MenuText Footer(DeviceSides sides, int side) =>
        sides.Rules.RoleOf(side) == SideRole.Dummy ? MenuText.Key("MENU_SIDES_DUMMY")
        : sides.On(side) is not { } device ? MenuText.Key("MENU_SIDES_FREE")
        : device.IsReady ? MenuText.Empty
        : MenuText.Key("MENU_SIDES_CONFIRM");

    private void ShowTokens(DeviceSides sides, long nowMilliseconds)
    {
        RemoveGone(sides);
        int middleRow = 0;
        foreach (SideSelectDevice device in sides.Devices)
        {
            if (!_tokens.TryGetValue(device.Id, out DeviceToken? token))
            {
                token = new DeviceToken { Size = new Vector2(ColumnWidth - (2f * TokenInset), TokenHeight) };
                _tokens.Add(device.Id, token);
                _board.AddChild(token);
            }

            int row = device.Column == SideColumn.NotPlaying ? middleRow++ : 0;
            var target = new Vector2(
                ((int)device.Column * (ColumnWidth + ColumnGap)) + TokenInset,
                HeaderHeight + (row * (TokenHeight + TokenGap)));
            string name = Name(sides, device);
            string status = device.IsReady ? _text!.Format(MenuText.Key("MENU_SIDES_READY")) : string.Empty;
            token.Show(device.Icon, name, status, device.Side, device.IsReady);
            token.MoveTo(target, nowMilliseconds);
            token.Refused(device.Refusals, nowMilliseconds);
            token.Animate(_settings, nowMilliseconds);
        }
    }

    private new string Name(DeviceSides sides, SideSelectDevice device)
    {
        string name = device.Icon == DeviceIcon.Keyboard
            ? _text!.Format(MenuText.Key("MENU_SIDES_KEYBOARD"))
            : device.Name;
        int number = sides.NumberOf(device);
        return number == 0
            ? name
            : _text!.Format(MenuText.Key("MENU_SIDES_NUMBERED", name, number.ToString(CultureInfo.InvariantCulture)));
    }

    private void RemoveGone(DeviceSides sides)
    {
        _gone.Clear();
        foreach (InputDeviceId id in _tokens.Keys)
        {
            if (!IsOnScreen(sides, id))
            {
                _gone.Add(id);
            }
        }

        foreach (InputDeviceId id in _gone)
        {
            _tokens[id].QueueFree();
            _tokens.Remove(id);
        }
    }

    private static bool IsOnScreen(DeviceSides sides, InputDeviceId id)
    {
        foreach (SideSelectDevice device in sides.Devices)
        {
            if (device.Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
