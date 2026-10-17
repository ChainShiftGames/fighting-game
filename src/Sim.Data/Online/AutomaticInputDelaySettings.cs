namespace Sim.Data.Online;

/// <summary>
/// How the automatic online input delay follows Backdash's ping (ADR-0005 §4), from <c>netcode/online.json</c>.
/// Developer settings.
/// </summary>
/// <param name="MinimumFrames">Delay at the lowest ping.</param>
/// <param name="MaximumFrames">Highest automatic delay; also the first round's, before any ping is known.</param>
/// <param name="PingStepMilliseconds">Each full step of this much averaged ping adds one frame above the minimum.</param>
/// <param name="PingSampleIntervalFrames">Logic frames between two ping readings.</param>
/// <param name="PingSamples">Readings averaged (median), so one reading never sets the delay (ADR-0014 problem 4).</param>
public sealed record AutomaticInputDelaySettings(
    int MinimumFrames,
    int MaximumFrames,
    int PingStepMilliseconds,
    int PingSampleIntervalFrames,
    int PingSamples);
