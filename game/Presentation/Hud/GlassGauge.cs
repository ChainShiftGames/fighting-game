using Godot;

namespace FightingGame.Presentation.Hud;

/// <summary>A gauge on frosted glass: the glass track, an optional damage trail and the glowing fill.</summary>
public partial class GlassGauge : Node2D
{
    private readonly GaugeLook _look;
    private readonly GlassPanel _track = new();
    private readonly GaugeFill _trail;
    private readonly GaugeFill _fill;

    public GlassGauge(GaugeLook look)
    {
        _look = look;
        _trail = new GaugeFill(look.Trail, look.Trail, look.Trail, Colors.Transparent);
        _fill = new GaugeFill(look.Start, look.Middle, look.End, look.Glow);
        AddChild(_track);
        AddChild(_trail);
        AddChild(_fill);
    }

    /// <param name="track">The glass, in screen pixels.</param>
    /// <param name="fill">Share filled, 0 to 1.</param>
    /// <param name="trail">Share the damage trail reaches, 0 to 1; ignored without a trail colour.</param>
    /// <param name="fromRight">Fills from the right end, e.g. P1's health, which drains from the outer (left) end.</param>
    /// <param name="isMirrored">P2's side: the gradient runs right to left.</param>
    /// <param name="scale">Screen pixels per reference pixel.</param>
    public void Show(Rect2 track, float fill, float trail, bool fromRight, bool isMirrored, float scale)
    {
        _track.Place(track, cornerShare: 0.5f);
        Rect2 inner = track.Grow(-_look.Inset * scale);
        _trail.Visible = _look.Trail.A > 0f;
        if (_trail.Visible)
        {
            _trail.Place(Part(inner, trail, fromRight), 0f, isMirrored);
        }

        _fill.Place(Part(inner, fill, fromRight), _look.GlowSize * scale, isMirrored);
    }

    private static Rect2 Part(Rect2 inner, float share, bool fromRight)
    {
        float width = inner.Size.X * Mathf.Clamp(share, 0f, 1f);
        float left = fromRight ? inner.End.X - width : inner.Position.X;
        return new Rect2(left, inner.Position.Y, width, inner.Size.Y);
    }
}
