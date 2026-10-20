using System;

namespace FightingGame.Presentation;

/// <summary>Eases the camera's view toward its target once per logic frame: smooth, the same at any display rate.</summary>
/// <remarks>
/// Steps by the match's frame number, never by elapsed time (ARCHITECTURE.md §9): showing a frame again (a fast
/// display, a pause) changes nothing and a frame step eases one step. A rollback that corrects the shown frame eases on
/// from what was shown. A frame earlier than the last (a replay seek) cuts straight to the target. However far the
/// easing lags, the view keeps covering what it must show (<see cref="VerticalFraming.MustShow"/>).
/// </remarks>
public sealed class VerticalFollow
{
    // Developer settings (presentation): the share of the way to its target the view's top and height move each logic
    // frame, quick while they grow (rising, zooming out: a slower follow lets a jumper's head reach the screen edge)
    // and gentle while they shrink (settling back after a landing), and the longest gap eased frame by frame; after a
    // longer one the view has settled anyway.
    private const int FollowPercent = 60;
    private const int SettlePercent = 20;
    private const int LongestEasedGap = 60;

    private VerticalSpan _view;
    private int _frame;
    private bool _following;

    /// <summary>The view for match frame <paramref name="frame"/>, eased toward the fighters' target view.</summary>
    /// <param name="ground">The round-start view (<see cref="StageCamera.GroundView"/>).</param>
    /// <param name="bodies">Each fighter's height on that frame (<see cref="FighterBoxes.Height"/>).</param>
    public VerticalSpan Follow(int frame, VerticalSpan ground, ReadOnlySpan<VerticalSpan> bodies)
    {
        VerticalSpan target = VerticalFraming.Target(ground, bodies);
        if (!_following || frame < _frame)
        {
            _view = target;
        }
        else
        {
            int steps = Math.Min(frame - _frame, LongestEasedGap);
            for (int step = 0; step < steps; step++)
            {
                int top = Ease(_view.Top, target.Top);
                _view = new VerticalSpan(top - Ease(_view.Height, target.Height), top);
            }
        }

        _view = _view.Covering(VerticalFraming.MustShow(target, bodies));
        _frame = frame;
        _following = true;
        return _view;
    }

    // Whole subpixels, so it lands exactly on the target instead of creeping toward it forever.
    private static int Ease(int from, int to)
    {
        int step = (to - from) * (to > from ? FollowPercent : SettlePercent) / 100;
        return step == 0 ? to : from + step;
    }
}
