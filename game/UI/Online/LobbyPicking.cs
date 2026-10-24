using FightingGame.Online;
using FightingGame.UI.CharacterSelect;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>Online character select through the lobby: picks travel as lobby member data (ADR-0042).</summary>
public sealed class LobbyPicking : IOnlinePicking
{
    private readonly LobbyFlow _lobby;

    public LobbyPicking(LobbyFlow lobby)
    {
        _lobby = lobby;
    }

    /// <summary>The host plays P1.</summary>
    public int LocalSide => _lobby.IsHost ? 0 : 1;

    // A pick this build can't read stays "waiting"; the match start then says why.
    public FighterPick? OpponentPick => OnlinePickText.TryRead(_lobby.OpponentPick);

    public bool HasSent => _lobby.LocalPick.Length > 0;

    public void Send(FighterPick pick) => _lobby.Pick(OnlinePickText.Write(pick));

    public void Leave() => _lobby.Leave();
}
