using System.Globalization;

namespace FightingGame.UI;

/// <summary>One ping for the player-facing ping indicator: its text and how playable it is. No player settings.</summary>
/// <param name="Milliseconds">Round trip to the other PC, as the netcode measures it.</param>
public readonly record struct PingReading(int Milliseconds)
{
    // Developer settings. Rollback hides up to ~100 ms well (ADR-0005's target); past ~150 ms rollbacks get visible.
    private const int MaxGoodMilliseconds = 100;
    private const int MaxFairMilliseconds = 150;

    /// <summary>Most bars a connection shows (<see cref="Bars"/>).</summary>
    public const int MaxBars = 3;

    /// <summary>Connection bars: 3 good, 2 fair, 1 poor (the order of <see cref="PingQuality"/>).</summary>
    public int Bars => MaxBars - (int)Quality;

    public PingQuality Quality => Milliseconds switch
    {
        <= MaxGoodMilliseconds => PingQuality.Good,
        <= MaxFairMilliseconds => PingQuality.Fair,
        _ => PingQuality.Poor,
    };

    public string Text => string.Create(CultureInfo.InvariantCulture, $"Ping {Milliseconds} ms");
}
