using Godot;

namespace FightingGame.UI.CharacterSelect;

/// <summary>How character select draws: each player's colour (cursor, tag, preview frame) and its panels.</summary>
public static class SelectStyles
{
    private static readonly Color _player1 = new(0.93f, 0.33f, 0.29f);
    private static readonly Color _player2 = new(0.3f, 0.6f, 0.95f);

    public static Color Of(int side) => side == 0 ? _player1 : _player2;

    /// <summary>A flat panel with a border; transparent parts are left out.</summary>
    public static StyleBoxFlat Box(Color background, Color border, int borderWidth, float margin)
    {
        var box = new StyleBoxFlat { BgColor = background, BorderColor = border };
        box.SetBorderWidthAll(borderWidth);
        box.SetCornerRadiusAll(4);
        box.SetContentMarginAll(margin);
        return box;
    }
}
