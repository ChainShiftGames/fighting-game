using Godot;

namespace FightingGame.UI;

/// <summary>
/// The player-facing ping during an online match, in the bottom-left corner, coloured by how playable it is. Hidden
/// without a reading (local matches, before the first frame). No settings.
/// </summary>
public partial class PingIndicator : CanvasLayer
{
    private const int Margin = 12;

    private static readonly Color _good = new(0.45f, 0.85f, 0.45f);
    private static readonly Color _fair = new(0.95f, 0.8f, 0.3f);
    private static readonly Color _poor = new(0.95f, 0.4f, 0.35f);

    private readonly Label _label = new()
    {
        VerticalAlignment = VerticalAlignment.Bottom,
        AnchorTop = 1f,
        AnchorBottom = 1f,
        OffsetLeft = Margin,
        OffsetBottom = -Margin,
        GrowVertical = Control.GrowDirection.Begin,
    };

    private int? _shown;

    public override void _Ready() => AddChild(_label);

    /// <param name="pingMilliseconds">The latest ping; null hides the indicator.</param>
    public void Show(int? pingMilliseconds)
    {
        if (pingMilliseconds == _shown)
        {
            return;
        }

        _shown = pingMilliseconds;
        _label.Visible = pingMilliseconds is not null;
        if (pingMilliseconds is { } ping)
        {
            var reading = new PingReading(ping);
            _label.Text = reading.Text;
            _label.AddThemeColorOverride("font_color", ColourOf(reading.Quality));
        }
    }

    private static Color ColourOf(PingQuality quality) => quality switch
    {
        PingQuality.Good => _good,
        PingQuality.Fair => _fair,
        _ => _poor,
    };
}
