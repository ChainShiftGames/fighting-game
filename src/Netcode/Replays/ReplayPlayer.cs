namespace Netcode.Replays;

/// <summary>Who one player was in a recorded match: their character and its colour.</summary>
/// <param name="CharacterId">The character's id in data, e.g. <c>testman</c>.</param>
/// <param name="Colour">The character's colour (palette index); 0 until there is a colour select.</param>
public readonly record struct ReplayPlayer(string CharacterId, byte Colour);
