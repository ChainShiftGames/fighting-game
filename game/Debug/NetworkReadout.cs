using System.Globalization;
using Netcode.Transport;

namespace FightingGame.Debug;

/// <summary>
/// The developer overlay's network line during an online match: this PC's simulated conditions (debug panel) and the
/// packets dropped on arrival as malformed or oversized (P2-03). Empty until an online match starts.
/// </summary>
/// <remarks>Engine-free, so its text is unit-tested without Godot (ADR-0016).</remarks>
public sealed class NetworkReadout
{
    private readonly LiveNetworkSimulation _simulation;
    private DroppedPackets? _dropped;

    /// <param name="simulation">The conditions the debug panel sets for this PC's packets.</param>
    public NetworkReadout(LiveNetworkSimulation simulation) => _simulation = simulation;

    /// <summary>Reads the drops of the transport the online match about to start uses.</summary>
    public void Watch(DroppedPackets dropped) => _dropped = dropped;

    public string Describe() =>
        _dropped is { } dropped ? Describe(_simulation.Current, dropped.Malformed, dropped.Oversized) : string.Empty;

    public static string Describe(NetworkSimulation simulation, int malformed, int oversized)
    {
        string conditions = simulation.IsNone
            ? "off"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{simulation.PingMilliseconds} ms ±{simulation.JitterMilliseconds} ms, " +
                $"{simulation.PacketLossPercent} % loss on this PC's sends");
        return string.Create(
            CultureInfo.InvariantCulture,
            $"network sim (F1): {conditions} | dropped on arrival: {malformed} malformed, {oversized} oversized");
    }
}
