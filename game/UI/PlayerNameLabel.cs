using FightingGame.Platform;
using Godot;

namespace FightingGame.UI;

/// <summary>Shows who is playing and on which platform, in the top-right corner.</summary>
public partial class PlayerNameLabel : CanvasLayer
{
    private const int Margin = 12;

    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Right,
        AnchorLeft = 1f,
        AnchorRight = 1f,
        OffsetRight = -Margin,
        OffsetTop = Margin,
        GrowHorizontal = Control.GrowDirection.Begin,
    };

    public override void _Ready() => AddChild(_label);

    public void Show(IPlatformServices platform) => _label.Text = $"{platform.PlatformName}: {platform.PlayerName}";
}
