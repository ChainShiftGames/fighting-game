using Netcode.Drivers;
using Sim.Input;
using Sim.Input.Motions;

namespace Netcode.Bots;

/// <summary>
/// A CPU player: looks at the match the way a player does, reacts after its reaction time and presses buttons like a
/// player. Plugs into any match driver as an <see cref="IInputSource"/> (versus CPU, soak tests, bot matches).
/// </summary>
/// <remarks>
/// It sees only the narrow <see cref="BotObservation"/> view, never game internals. Seeded and free of clocks and
/// shared randomness, so the same seed and the same observations always give the same inputs; its inputs are local
/// inputs, so it works under rollback like a human player. Allocates nothing per frame.
/// </remarks>
public sealed class CpuBot : IInputSource
{
    private readonly IObservationSource _eyes;
    private readonly ReactionDelay _reaction;
    private readonly BotBrain _brain;

    /// <param name="eyes">What the bot's player would see.</param>
    /// <param name="moves">Its character's attacks against this opponent.</param>
    /// <param name="movement">The movement inputs from data, for dashing.</param>
    /// <param name="settings">Difficulty and style.</param>
    /// <param name="seed">Seeds its choices.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">A setting is outside its range.</exception>
    public CpuBot(
        IObservationSource eyes, MoveRepertoire moves, MovementInputs movement, BotSettings settings, uint seed)
    {
        settings.Validate();
        _eyes = eyes;
        _reaction = new ReactionDelay(settings.ReactionDelayFrames);
        _brain = new BotBrain(moves, movement.DashMotion, settings, seed);
    }

    /// <summary>What the bot decided last: approach, attack, block, punish or wait.</summary>
    public BotIntent Intent => _brain.Intent;

    public InputWord Next()
    {
        BotObservation seen = _reaction.Push(_eyes.Observe());
        return _brain.Next(in seen);
    }
}
