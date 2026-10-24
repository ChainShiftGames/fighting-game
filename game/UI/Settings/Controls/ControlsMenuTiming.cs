namespace FightingGame.UI.Settings.Controls;

/// <summary>How long "press a control" waits before it gives up (developer setting); <see cref="Default"/> sets it.</summary>
public sealed record ControlsMenuTiming(long CaptureMilliseconds)
{
    public static ControlsMenuTiming Default { get; } = new(CaptureMilliseconds: 6000);
}
