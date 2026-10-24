using Godot;

namespace FightingGame.UI.Results;

/// <summary>
/// Beside a results entry, on one player's side: a triangle where their cursor is, a check mark once they want the
/// rematch. Drawn as shapes, so it doesn't depend on the font having those glyphs.
/// </summary>
public partial class ResultsMarker : Control
{
    private const float LineWidth = 4f;

    private ResultsMarkerShape _shape;
    private Color _colour = Colors.White;
    private bool _pointsLeft;

    /// <param name="pointsLeft">The cursor triangle points left (on P2's side, towards the entry).</param>
    public void Show(ResultsMarkerShape shape, Color colour, bool pointsLeft)
    {
        if (shape == _shape && colour == _colour && pointsLeft == _pointsLeft)
        {
            return;
        }

        _shape = shape;
        _colour = colour;
        _pointsLeft = pointsLeft;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 size = Size;
        float unit = Mathf.Min(size.X, size.Y) * 0.5f;
        Vector2 centre = size * 0.5f;
        switch (_shape)
        {
            case ResultsMarkerShape.Cursor:
                float tip = _pointsLeft ? -unit * 0.6f : unit * 0.6f;
                DrawColoredPolygon(
                    [
                        centre + new Vector2(tip, 0f),
                        centre + new Vector2(-tip, -unit * 0.6f),
                        centre + new Vector2(-tip, unit * 0.6f),
                    ],
                    _colour);
                break;
            case ResultsMarkerShape.Tick:
                DrawPolyline(
                    [
                        centre + new Vector2(-unit * 0.6f, 0f),
                        centre + new Vector2(-unit * 0.15f, unit * 0.45f),
                        centre + new Vector2(unit * 0.65f, -unit * 0.5f),
                    ],
                    _colour,
                    LineWidth,
                    antialiased: true);
                break;
        }
    }
}
