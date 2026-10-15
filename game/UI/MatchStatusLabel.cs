using Godot;

namespace FightingGame.UI;

/// <summary>Centred message about the match itself: waiting for the other player, or a desync. Empty while playing.</summary>
public partial class MatchStatusLabel : CanvasLayer
{
    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    public override void _Ready() => AddChild(_label);

    public void Show(string message) => _label.Text = message;
}
