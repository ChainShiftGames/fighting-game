namespace FightingGame.UI.SideSelect;

/// <summary>How side select is paced and animated.</summary>
/// <remarks>Developer settings (ARCHITECTURE.md §6); <see cref="Default"/> is the one place they are set.</remarks>
/// <param name="AllReadyMilliseconds">How long "all ready" shows before character select, so a last back can undo it.</param>
/// <param name="SlideMilliseconds">How long an icon takes to slide to its new column; only the look waits.</param>
/// <param name="ShakeMilliseconds">How long an icon shakes when its move is refused (a taken side).</param>
public sealed record SideSelectSettings(long AllReadyMilliseconds, long SlideMilliseconds, long ShakeMilliseconds)
{
    public static SideSelectSettings Default { get; } =
        new(AllReadyMilliseconds: 400, SlideMilliseconds: 120, ShakeMilliseconds: 220);
}
