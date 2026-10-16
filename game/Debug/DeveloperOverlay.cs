using System.Globalization;
using Godot;

namespace FightingGame.Debug;

/// <summary>
/// Developer overlay (developer setting, F3): logic rate, frame rate, vsync, input latency, the match and, online,
/// its connection, then the local input words and joypads.
/// </summary>
public partial class DeveloperOverlay : CanvasLayer
{
    private const int Margin = 12;

    private readonly Label _label = new() { Position = new Vector2(Margin, Margin) };
    private string _timing = string.Empty;
    private string _inputs = string.Empty;

    public override void _Ready() => AddChild(_label);

    public void Refresh(double logicFramesPerSecond, bool vsync, double latencyAverageMs, double latencyMaxMs)
    {
        string vsyncText = vsync ? "on" : "off";
        _timing = string.Create(
            CultureInfo.InvariantCulture,
            $"logic {logicFramesPerSecond:F1} Hz | render {Engine.GetFramesPerSecond():F0} fps | vsync {vsyncText} (F2)\n" +
            $"input to draw: avg {latencyAverageMs:F1} ms, max {latencyMaxMs:F1} ms");
    }

    /// <summary>Shown below the match readings from the next <see cref="ShowMatch"/> on.</summary>
    public void ShowInputs(string inputs) => _inputs = inputs;

    /// <param name="connection">Online connection readings, one per line; empty for local matches.</param>
    public void ShowMatch(string description, int frame, string connection)
    {
        string match = string.Create(CultureInfo.InvariantCulture, $"{_timing}\n{description} | frame {frame}");
        string withConnection = connection.Length == 0 ? match : $"{match}\n{connection}";
        _label.Text = _inputs.Length == 0 ? withConnection : $"{withConnection}\n{_inputs}";
    }
}
