using FightingGame.Diagnostics;
using FightingGame.Input;
using Netcode.Drivers;
using Sim.Toy;

namespace FightingGame.Online;

/// <summary>
/// A started toy match: its driver, the players on this PC, a one-line description for the overlay, and for online
/// matches the connection meter.
/// </summary>
public sealed record ToyMatch(
    IMatchDriver<ToyState> Driver, PlayerInputReader[] LocalPlayers, string Description, ConnectionMeter? Connection)
{
    public bool IsOnline => Connection is not null;
}
