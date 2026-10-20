using System.Globalization;
using Godot;
using Sim.State;

namespace FightingGame.Presentation.Hud;

/// <summary>The HUD's text and small marks over the glass: names, round wins, the timer, meter numbers and the combo counter.</summary>
public partial class HudLettering : Node2D
{
    // The Aether Glass face (SIL OFL, licence beside it); Godot's default font stands in if it is missing.
    internal const string FontPath = "res://Assets/UI/Fonts/Marcellus-Regular.ttf";

    // Font sizes in reference pixels (HudLayout).
    private const float NameSize = 19f;
    private const float TimerSize = 49f;
    private const float RoundSize = 14f;
    private const float LabelSize = 13f;
    private const float MeterNumberSize = 33f;
    private const float ComboSize = 69f;
    private const float ComboLabelSize = 17f;
    private const float PipSize = 13f;
    private const float PipGap = 8f;
    private const float NamePipGap = 12f;
    private const int MinComboHits = 2;

    // Below this screen size, letters get tracking and a lighter rim, or the thin face blurs into its outline.
    private const int SmallTextSize = 18;
    private const int SmallTracking = 2;

    private static readonly Color _text = new(1f, 1f, 1f);
    private static readonly Color _glow = new(0.56f, 0.96f, 0.87f, 0.45f);
    private static readonly Color _shade = new(0.06f, 0.24f, 0.27f, 0.85f);
    private static readonly Color _pipWon = new(0.73f, 1f, 0.94f);

    private readonly string[] _names;
    private readonly int _roundsToWin;
    private readonly Font _font;
    private readonly FontVariation _smallFont;
    private readonly HudPlayerValues[] _values = new HudPlayerValues[MatchState.PlayerCount];
    private HudLayout _layout;
    private string _timer = "";
    private string _round = "";
    private bool _showsMeter;

    /// <param name="names">P1's and P2's display name.</param>
    public HudLettering(string[] names, int roundsToWin)
    {
        _names = names;
        _roundsToWin = roundsToWin;
        _font = ResourceLoader.Exists(FontPath) ? GD.Load<Font>(FontPath) : ThemeDB.FallbackFont;
        _smallFont = new FontVariation { BaseFont = _font, SpacingGlyph = SmallTracking };
    }

    public void Show(HudLayout layout, HudPlayerValues player1, HudPlayerValues player2, string timer, string round, bool showsMeter)
    {
        _layout = layout;
        _values[0] = player1;
        _values[1] = player2;
        _timer = timer;
        _round = round;
        _showsMeter = showsMeter;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Rect2 timer = _layout.Timer;
        Centred(_timer, timer.GetCenter() + new Vector2(0f, _layout.FontSize(TimerSize) * 0.25f), TimerSize);
        Centred(_round, new Vector2(timer.GetCenter().X, timer.End.Y - (timer.Size.Y * 0.17f)), RoundSize);
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            DrawNameRow(player);
            if (_showsMeter)
            {
                DrawMeterText(player);
            }

            DrawCombo(player);
        }
    }

    private void DrawNameRow(int player)
    {
        Rect2 row = _layout.NameRow(player);
        bool isLeft = player == 0;
        int size = _layout.FontSize(NameSize);
        string name = _names[player].ToUpperInvariant();
        float nameWidth = FontFor(size).GetStringSize(name, HorizontalAlignment.Left, -1, size).X;
        float baseline = row.Position.Y + (size * 0.85f);
        float nameLeft = isLeft ? row.Position.X : row.End.X - nameWidth;
        Lettered(name, new Vector2(nameLeft, baseline), size);

        float pip = PipSize * _layout.Scale;
        float step = (PipSize + PipGap) * _layout.Scale;
        float gap = NamePipGap * _layout.Scale;
        float firstCentre = isLeft ? nameLeft + nameWidth + gap + (pip / 2f) : nameLeft - gap - (pip / 2f);
        float direction = isLeft ? 1f : -1f;
        for (int win = 0; win < _roundsToWin; win++)
        {
            var centre = new Vector2(firstCentre + (direction * win * step), row.Position.Y + (size * 0.5f));
            DrawPip(centre, pip / 2f, win < _values[player].Wins);
        }
    }

    private void DrawPip(Vector2 centre, float radius, bool isWon)
    {
        if (isWon)
        {
            DrawCircle(centre, radius * 1.6f, _glow with { A = 0.3f });
            DrawCircle(centre, radius, _pipWon);
        }

        DrawArc(centre, radius, 0f, Mathf.Tau, 24, _shade, 3f * _layout.Scale);
        DrawArc(centre, radius, 0f, Mathf.Tau, 24, _text, 1.8f * _layout.Scale);
    }

    private void DrawMeterText(int player)
    {
        Rect2 label = _layout.MeterLabel(player);
        bool isLeft = player == 0;
        int labelSize = _layout.FontSize(LabelSize);
        Aligned("TENSION", label, labelSize, isLeft);

        Rect2 number = _layout.MeterNumber(player);
        string percent = (_values[player].Meter / 100).ToString(CultureInfo.InvariantCulture);
        Aligned(percent, number, _layout.FontSize(MeterNumberSize), !isLeft);
    }

    private void DrawCombo(int player)
    {
        int hits = _values[player].ComboHits;
        if (hits < MinComboHits)
        {
            return;
        }

        Rect2 anchor = _layout.Combo(player);
        bool isLeft = player == 0;
        int size = _layout.FontSize(ComboSize);
        Aligned(hits.ToString(CultureInfo.InvariantCulture), new Rect2(anchor.Position, anchor.Size.X, size), size, isLeft);
        int labelSize = _layout.FontSize(ComboLabelSize);
        var label = new Rect2(anchor.Position + new Vector2(0f, size * 1.05f), anchor.Size.X, labelSize);
        Aligned("HITS", label, labelSize, isLeft);
    }

    // Text with its baseline at the bottom of the box, against the box's left or right edge.
    private void Aligned(string text, Rect2 box, int size, bool isLeft)
    {
        float width = FontFor(size).GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float left = isLeft ? box.Position.X : box.End.X - width;
        Lettered(text, new Vector2(left, box.End.Y), size);
    }

    private void Centred(string text, Vector2 baselineCentre, float referenceSize)
    {
        int size = _layout.FontSize(referenceSize);
        float width = FontFor(size).GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        Lettered(text, baselineCentre - new Vector2(width / 2f, 0f), size);
    }

    // White lettering with a dark rim for contrast on the pale stage and an Aether glow around it.
    // Small text keeps only the rim: the glow would swallow its thin strokes.
    private void Lettered(string text, Vector2 baseline, int size)
    {
        Font font = FontFor(size);
        bool isSmall = size < SmallTextSize;
        if (!isSmall)
        {
            DrawStringOutline(font, baseline, text, HorizontalAlignment.Left, -1, size, size / 4, _glow);
        }

        int rim = isSmall ? 2 : Mathf.Max(2, size / 9);
        DrawStringOutline(font, baseline, text, HorizontalAlignment.Left, -1, size, rim, _shade);
        DrawString(font, baseline, text, HorizontalAlignment.Left, -1, size, _text);
    }

    private Font FontFor(int size) => size < SmallTextSize ? _smallFont : _font;
}
