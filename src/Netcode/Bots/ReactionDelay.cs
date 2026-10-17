namespace Netcode.Bots;

/// <summary>Holds back what a bot sees by a fixed number of frames: its reaction time.</summary>
/// <remarks>
/// A ring of the newest observations, allocated once. Until enough frames have been seen, the oldest one stands in:
/// at round start nothing has happened yet that the bot could miss.
/// </remarks>
public sealed class ReactionDelay
{
    private readonly BotObservation[] _seen;
    private int _newest = -1;
    private int _count;

    /// <param name="delayFrames">How many frames late the bot sees things; 0 sees each observation at once.</param>
    public ReactionDelay(int delayFrames)
    {
        _seen = new BotObservation[delayFrames + 1];
    }

    /// <summary>Takes in the newest observation and returns the one the bot has now reacted to.</summary>
    public BotObservation Push(in BotObservation newest)
    {
        _newest = (_newest + 1) % _seen.Length;
        _seen[_newest] = newest;
        if (_count < _seen.Length)
        {
            _count++;
        }

        int delayed = (_newest - (_count - 1) + _seen.Length) % _seen.Length;
        return _seen[delayed];
    }
}
