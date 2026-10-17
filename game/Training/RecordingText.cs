using System.Globalization;
using FightingGame.Loop;

namespace FightingGame.Training;

/// <summary>How the training menu shows a recording's length: seconds with one decimal, or "empty".</summary>
public static class RecordingText
{
    public static string Length(int frames) => frames == 0
        ? "empty"
        : $"{(frames / (double)LoopSettings.Default.FramesPerSecond).ToString("0.0", CultureInfo.InvariantCulture)} s";
}
