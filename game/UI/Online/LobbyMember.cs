namespace FightingGame.UI.Online;

/// <summary>One player in the lobby as the online screens list them: their side, name and whether they are ready.</summary>
/// <param name="Side">0 for P1 (the lobby's host), 1 for P2.</param>
public readonly record struct LobbyMember(int Side, string Name, bool IsYou, bool IsReady);
