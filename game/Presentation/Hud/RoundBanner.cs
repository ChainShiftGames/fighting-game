using Godot;
using Sim.State;

namespace FightingGame.Presentation.Hud;

/// <summary>
/// The big text across the middle of the screen when a round ends (KO, time-out) and when the match is won
/// (<see cref="RoundBannerText"/>). Placeholder look: HUD lettering, larger.
/// </summary>
/// <remarks>
/// Read from the state each displayed frame, like the timer, so a KO that a rollback takes back takes its banner with
/// it (ARCHITECTURE.md §9).
/// </remarks>
public partial class RoundBanner : Node2D
{
    // Font sizes in reference pixels (HudLayout).
    private const float HeadlineSize = 120f;
    private const float DetailSize = 30f;
    private const float DetailGap = 1.4f;

    private static readonly Color _text = new(1f, 1f, 1f);
    private static readonly Color _glow = new(1f, 0.45f, 0.35f, 0.55f);
    private static readonly Color _shade = new(0.1f, 0.05f, 0.08f, 0.9f);

    private readonly Font _font =
        ResourceLoader.Exists(HudLettering.FontPath) ? GD.Load<Font>(HudLettering.FontPath) : ThemeDB.FallbackFont;

    private HudLayout _layout;
    private string _headline = string.Empty;
    private string _detail = string.Empty;

    public void Show(HudLayout layout, in RoundState round)
    {
        string headline = RoundBannerText.Headline(in round);
        string detail = RoundBannerText.Detail(in round);
        if (headline == _headline && detail == _detail && layout.Scale == _layout.Scale)
        {
            return;
        }

        _layout = layout;
        _headline = headline;
        _detail = detail;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_headline.Length == 0)
        {
            return;
        }

        Vector2 baseline = _layout.Banner;
        Centred(_headline, baseline, _layout.FontSize(HeadlineSize));
        if (_detail.Length > 0)
        {
            int size = _layout.FontSize(DetailSize);
            Centred(_detail, baseline + new Vector2(0f, size * DetailGap), size);
        }
    }

    private void Centred(string text, Vector2 baselineCentre, int size)
    {
        float width = _font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        Vector2 baseline = baselineCentre - new Vector2(width / 2f, 0f);
        DrawStringOutline(_font, baseline, text, HorizontalAlignment.Left, -1, size, size / 5, _glow);
        DrawStringOutline(_font, baseline, text, HorizontalAlignment.Left, -1, size, Mathf.Max(2, size / 12), _shade);
        DrawString(_font, baseline, text, HorizontalAlignment.Left, -1, size, _text);
    }
}
