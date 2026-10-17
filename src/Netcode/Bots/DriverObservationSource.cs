using Netcode.Drivers;
using Sim.Characters;
using Sim.State;

namespace Netcode.Bots;

/// <summary>Shows a bot the state a match driver would draw for its player: the newest one, predicted or not.</summary>
/// <remarks>
/// Online, the newest state may rest on predicted inputs and change on a rollback, exactly as a human's screen does.
/// The bot's inputs are local inputs, sent to the other PC like a human's, so reacting to a prediction never
/// desyncs; it only means the bot, like a human, sometimes reacts to something that didn't happen.
/// </remarks>
public sealed class DriverObservationSource : IObservationSource
{
    private readonly IMatchDriver<MatchState> _driver;
    private readonly int _player;
    private readonly CharacterData _self;
    private readonly CharacterData _opponent;

    /// <param name="driver">The driver running the match the bot plays in.</param>
    /// <param name="player">The bot's port: 0 = P1, 1 = P2.</param>
    /// <param name="self">The bot's character.</param>
    /// <param name="opponent">The opponent's character.</param>
    public DriverObservationSource(
        IMatchDriver<MatchState> driver, int player, CharacterData self, CharacterData opponent)
    {
        _driver = driver;
        _player = player;
        _self = self;
        _opponent = opponent;
    }

    public BotObservation Observe() => MatchObserver.Observe(in _driver.State, _player, _self, _opponent);
}
