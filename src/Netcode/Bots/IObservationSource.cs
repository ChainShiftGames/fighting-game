namespace Netcode.Bots;

/// <summary>Shows a bot the match as its player would see it right now.</summary>
public interface IObservationSource
{
    /// <summary>The newest frame the bot's player could see; called once per input the bot gives.</summary>
    BotObservation Observe();
}
