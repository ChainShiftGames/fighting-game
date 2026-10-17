using System;
using Godot;
using Sim.Diagnostics;
using Sim.State;

namespace FightingGame.Training;

/// <summary>
/// Draws the <see cref="FrameMeter"/> above the training menu's summary line: a row of cells per fighter, coloured by
/// frame kind, invincible frames outlined, and the startup and frame advantage under them.
/// </summary>
/// <remarks>Shown while the training player's frame meter setting (a player setting) is on.</remarks>
public partial class FrameMeterBar : Node2D
{
    private const float CellWidth = 9f;
    private const float CellHeight = 14f;
    private const float CellGap = 1f;
    private const float RowGap = 3f;
    private const float BottomOffset = 96f;
    private const float TextGap = 16f;
    private const int FontSize = 16;

    private static readonly Color _invincibleOutline = new(1f, 1f, 1f);
    private static readonly Color _textColor = new(0.95f, 0.95f, 1f);

    private FrameMeter? _meter;
    private Func<bool> _isShown = () => false;

    /// <param name="isShown">The player's frame meter setting, read every displayed frame.</param>
    public void Show(FrameMeter meter, Func<bool> isShown)
    {
        _meter = meter;
        _isShown = isShown;
    }

    // Above the stage and HUD, redrawn every displayed frame like them.
    public override void _Ready() => ZIndex = 2;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (_meter is null || !_isShown())
        {
            return;
        }

        Vector2 size = GetViewportRect().Size;
        float width = FrameMeter.Columns * (CellWidth + CellGap);
        float left = (size.X - width) / 2f;
        float top = size.Y - BottomOffset - (2f * CellHeight) - RowGap;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            DrawRow(fighter, left, top + (fighter * (CellHeight + RowGap)));
        }

        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(left, top + (2f * CellHeight) + RowGap + TextGap),
            FrameMeterText.Describe(_meter),
            HorizontalAlignment.Center,
            width,
            FontSize,
            _textColor);
    }

    private void DrawRow(int fighter, float left, float top)
    {
        for (int column = 0; column < FrameMeter.Columns; column++)
        {
            var cell = new Rect2(left + (column * (CellWidth + CellGap)), top, CellWidth, CellHeight);
            if (column >= _meter!.Count)
            {
                DrawRect(cell, ColorOf(FrameKind.Free));
                continue;
            }

            FighterFrameState frame = _meter.At(fighter, column);
            DrawRect(cell, ColorOf(frame.Kind));
            if (frame.IsInvincible)
            {
                DrawRect(cell, _invincibleOutline, filled: false, width: 2f);
            }
        }
    }

    // Startup green, active red, recovery blue, stun yellow: the colours players know from Strive and SF6.
    private static Color ColorOf(FrameKind kind) => kind switch
    {
        FrameKind.Startup => new Color(0.2f, 0.75f, 0.35f),
        FrameKind.Active => new Color(0.9f, 0.25f, 0.25f),
        FrameKind.Recovery => new Color(0.25f, 0.45f, 0.95f),
        FrameKind.Movement => new Color(0.55f, 0.45f, 0.75f),
        FrameKind.Hitstun => new Color(0.95f, 0.85f, 0.2f),
        FrameKind.Blockstun => new Color(0.95f, 0.65f, 0.15f),
        FrameKind.Knockdown => new Color(0.6f, 0.6f, 0.6f),
        _ => new Color(0.18f, 0.18f, 0.2f),
    };
}
