using FightingGame.Loop;
using Godot;
using Netcode.Drivers;
using Sim.Characters;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// The match HUD: health bars with names, rounds won, the round and the timer, read from the game state. Each
/// value shows whatever the logic has; the timer and round wins only change once P1-13 runs rounds.
/// </summary>
public partial class MatchHud : Node2D
{
    private const float Margin = 24f;
    private const float BarHeight = 18f;
    private const float CentreWidth = 120f;
    private const float NameGap = 18f;
    private const float WinPipRadius = 5f;
    private const float WinPipSpacing = 14f;
    private const int NameFontSize = 16;
    private const int TimerFontSize = 32;
    private const int RoundFontSize = 14;

    private static readonly Color _barBackground = new(0.2f, 0.05f, 0.05f);
    private static readonly Color _healthColor = new(0.95f, 0.8f, 0.2f);
    private static readonly Color _frameColor = new(0.9f, 0.9f, 0.95f);
    private static readonly Color _textColor = new(0.95f, 0.95f, 1f);

    private IMatchDriver<MatchState>? _driver;
    private CharacterData[] _characters = [];

    public void Show(IMatchDriver<MatchState> driver, CharacterData[] characters)
    {
        _driver = driver;
        _characters = characters;
    }

    // Drawn on top of the stage; redrawn every displayed frame, like the view under it.
    public override void _Ready() => ZIndex = 1;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (_driver is null)
        {
            return;
        }

        ref readonly MatchState state = ref _driver.State;
        float width = GetViewportRect().Size.X;
        float barWidth = (width - CentreWidth - (2f * Margin)) / 2f;
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            DrawPlayer(player, in state, barWidth, width);
        }

        Font font = ThemeDB.FallbackFont;
        float centreX = (width - CentreWidth) / 2f;
        string timer = HudText.Timer(state.Round.TimerFramesLeft, LoopSettings.Default.FramesPerSecond);
        DrawString(font, new Vector2(centreX, Margin + BarHeight), timer, HorizontalAlignment.Center, CentreWidth, TimerFontSize, _textColor);
        DrawString(font, new Vector2(centreX, Margin + BarHeight + NameGap), HudText.Round(state.Round.Round), HorizontalAlignment.Center, CentreWidth, RoundFontSize, _textColor);
    }

    // Health stays next to the timer; damage eats each bar from its outer edge.
    private void DrawPlayer(int player, in MatchState state, float barWidth, float screenWidth)
    {
        bool isLeft = player == 0;
        float left = isLeft ? Margin : screenWidth - Margin - barWidth;
        var bar = new Rect2(left, Margin, barWidth, BarHeight);
        float fill = HudText.HealthFraction(state.Fighters[player].Health, _characters[player].Stats.Health);
        float fillWidth = barWidth * fill;
        float fillLeft = isLeft ? bar.End.X - fillWidth : bar.Position.X;
        DrawRect(bar, _barBackground);
        DrawRect(new Rect2(fillLeft, Margin, fillWidth, BarHeight), _healthColor);
        DrawRect(bar, _frameColor, filled: false, width: 2f);

        Font font = ThemeDB.FallbackFont;
        HorizontalAlignment alignment = isLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        DrawString(font, new Vector2(left, Margin + BarHeight + NameGap), _characters[player].DisplayName, alignment, barWidth, NameFontSize, _textColor);
        DrawWins(state.Round.Wins[player], isLeft ? bar.End.X : bar.Position.X, isLeft ? -1f : 1f);
    }

    // One pip per round won, from the inner end of the bar outwards.
    private void DrawWins(int wins, float innerEdge, float direction)
    {
        float y = Margin + BarHeight + NameGap + WinPipRadius;
        for (int win = 0; win < wins; win++)
        {
            float x = innerEdge + (direction * (WinPipRadius + (win * WinPipSpacing)));
            DrawCircle(new Vector2(x, y), WinPipRadius, _healthColor);
        }
    }
}
