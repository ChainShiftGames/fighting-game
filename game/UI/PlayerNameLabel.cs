using FightingGame.Platform;
using Godot;

namespace FightingGame.UI;

/// <summary>Shows who is playing and on which platform, in the bottom-right corner, clear of the match HUD.</summary>
public partial class PlayerNameLabel : CanvasLayer
{
    private const int Margin = 12;

    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        AnchorLeft = 1f,
        AnchorRight = 1f,
        AnchorTop = 1f,
        AnchorBottom = 1f,
        OffsetRight = -Margin,
        OffsetBottom = -Margin,
        GrowHorizontal = Control.GrowDirection.Begin,
        GrowVertical = Control.GrowDirection.Begin,
    };

    public override void _Ready() => AddChild(_label);

    public void Show(IPlatformServices platform) => _label.Text = $"{platform.PlatformName}: {platform.PlayerName}";
}
