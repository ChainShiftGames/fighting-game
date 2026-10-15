using System.Globalization;
using Godot;

namespace FightingGame.Debug;

/// <summary>Developer overlay (developer setting, F3): logic rate, frame rate, vsync, input latency and the match.</summary>
public partial class DeveloperOverlay : CanvasLayer
{
    private const int Margin = 12;

    private readonly Label _label = new() { Position = new Vector2(Margin, Margin) };
    private string _timing = string.Empty;

    public override void _Ready() => AddChild(_label);

    public void Refresh(double logicFramesPerSecond, bool vsync, double latencyAverageMs, double latencyMaxMs)
    {
        string vsyncText = vsync ? "on" : "off";
        _timing = string.Create(
            CultureInfo.InvariantCulture,
            $"logic {logicFramesPerSecond:F1} Hz | render {Engine.GetFramesPerSecond():F0} fps | vsync {vsyncText} (F2)\n" +
            $"input to draw: avg {latencyAverageMs:F1} ms, max {latencyMaxMs:F1} ms");
    }

    public void ShowMatch(string description, int frame) =>
        _label.Text = string.Create(CultureInfo.InvariantCulture, $"{_timing}\n{description} | frame {frame}");
}
