using FightingGame.Input;
using Netcode.Drivers;
using Sim.Toy;

namespace FightingGame.Online;

/// <summary>A started toy match: its driver, the players on this PC, and a one-line description for the overlay.</summary>
public sealed record ToyMatch(IMatchDriver<ToyState> Driver, PlayerInputReader[] LocalPlayers, string Description, bool IsOnline);
