using FightingGame.Presentation.World.Projectiles;

namespace FightingGame.VfxLab;

/// <summary>How the Lab shows the variants: one set of choices for the whole grid (developer settings, presentation).</summary>
public sealed class LabViewOptions
{
    /// <summary>A round id, <see cref="LabSession.AllRounds"/> or <see cref="LabSession.ForkRound"/>.</summary>
    public string Round { get; set; } = LabSession.AllRounds;

    public bool PicksOnly { get; set; }

    public bool ShowHidden { get; set; }

    /// <summary>The one variant shown alone, or null for the grid.</summary>
    public string? Solo { get; set; }

    public int Columns { get; set; } = 3;

    public LabBackdrop Backdrop { get; set; } = LabBackdrop.Day;

    public LabFramingKind Framing { get; set; } = LabFramingKind.Match;

    public float Zoom { get; set; } = 1f;

    public LabPhaseLoop Loop { get; set; } = LabPhaseLoop.Whole;

    public ImpactKind Impact { get; set; } = ImpactKind.Hit;

    /// <summary>Cast to the left, to check the mirrored look.</summary>
    public bool Mirror { get; set; }

    /// <summary>Drains colour from the views, to check the effect reads by value alone.</summary>
    public bool Greyscale { get; set; }

    /// <summary>Darkens the backdrop as a super flash does.</summary>
    public bool SuperFlash { get; set; }

    public bool StandIns { get; set; } = true;

    public bool Hitbox { get; set; }

    /// <summary>The style's palette wins over every variant's own colours (tweaks still win).</summary>
    public bool PaletteLock { get; set; }
}
