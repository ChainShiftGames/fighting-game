using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>What one Lab view shows: the world pixel at its middle and how many world pixels tall it is.</summary>
public readonly record struct LabShot(float CentreX, float CentreY, float VisibleHeight)
{
    // Developer settings (presentation): the match camera shows 228 world pixels of height (as in a match at 16:9),
    // the floor sits 12 % of the height above the bottom, and the cast keeps this margin from the sides.
    public const float MatchHeight = 228f;
    private const float FloorShare = 0.12f;
    private const float SideMargin = 40f;
    private const float CloseUpZoom = 2.4f;
    private const float ExtremeZoom = 5f;

    /// <param name="zoom">Extra zoom on top of the framing (1 = none).</param>
    /// <param name="aspect">The view's width over its height.</param>
    public static LabShot Of(LabTiming timing, LabMoment moment, LabFramingKind framing, float zoom, float aspect)
    {
        zoom = System.MathF.Max(zoom, 0.05f);
        if (framing == LabFramingKind.Match)
        {
            float left = -timing.FighterWidth / 2f;
            float right = LabLayout.TargetX(timing) + (timing.FighterWidth / 2f);
            float height = System.MathF.Max(MatchHeight, (right - left + (2f * SideMargin)) / System.MathF.Max(aspect, 0.1f)) / zoom;
            return new LabShot((left + right) / 2f, (height / 2f) - (height * FloorShare), height);
        }

        (float x, float y) = LabLayout.EffectPoint(timing, moment);
        float close = MatchHeight / (framing == LabFramingKind.CloseUp ? CloseUpZoom : ExtremeZoom) / zoom;
        return new LabShot(x, y, close);
    }
}
