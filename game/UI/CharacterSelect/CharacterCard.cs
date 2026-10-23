using Godot;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// One slot of the grid: the character's name on a card until there is a portrait. Each player's cursor is a frame
/// around it, player 1 outside and player 2 inside, so both show on the same card in a mirror.
/// </summary>
public partial class CharacterCard : PanelContainer
{
    private const float CardWidth = 150f;
    private const float CardHeight = 96f;
    private const int CursorWidth = 4;

    private static readonly Color _card = new(1f, 1f, 1f, 0.07f);
    private static readonly Color _none = new(0f, 0f, 0f, 0f);

    private readonly PanelContainer _inner = new();
    private readonly Label _name = new()
    {
        ThemeTypeVariation = "MenuItemLabel",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private readonly StyleBoxFlat[] _frames =
    [
        SelectStyles.Box(_none, SelectStyles.Of(0), CursorWidth, 2f),
        SelectStyles.Box(_card, SelectStyles.Of(1), CursorWidth, 6f),
    ];

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(CardWidth, CardHeight);
        AddThemeStyleboxOverride("panel", _frames[0]);
        _inner.AddThemeStyleboxOverride("panel", _frames[1]);
        _inner.SizeFlagsVertical = SizeFlags.ExpandFill;
        _inner.AddChild(_name);
        AddChild(_inner);
    }

    public void Show(string name, bool player1Here, bool player2Here)
    {
        _name.Text = name;
        _frames[0].BorderColor = player1Here ? SelectStyles.Of(0) : _none;
        _frames[1].BorderColor = player2Here ? SelectStyles.Of(1) : _none;
    }
}
