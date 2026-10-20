using System;

namespace FightingGame.Presentation.Hud;

/// <summary>
/// The pale strip behind a health bar that shows the damage just taken: it holds while the fighter is in hitstun and
/// then drains to the health, by logic frames so it runs at the same speed at any display rate.
/// </summary>
public sealed class DamageTrail
{
    // Developer setting (presentation): share of the bar drained per logic frame, a full bar in about a second.
    private const float DrainPerFrame = 0.018f;

    private float _trail = 1f;
    private int _lastFrame;

    /// <summary>Share of the bar the trail reaches, 0 to 1; never below the health.</summary>
    public float Fraction => _trail;

    /// <param name="frame">The logic frame shown.</param>
    /// <param name="health">The health shown, 0 to 1.</param>
    /// <param name="isHeld">True while the damage is still coming, e.g. the fighter is in hitstun.</param>
    public void Follow(int frame, float health, bool isHeld)
    {
        // A rollback or a new match shows an earlier frame: start over from what it shows.
        int elapsed = frame - _lastFrame;
        _lastFrame = frame;
        if (elapsed < 0 || health >= _trail)
        {
            _trail = health;
            return;
        }

        if (!isHeld)
        {
            _trail = Math.Max(health, _trail - (DrainPerFrame * elapsed));
        }
    }
}
