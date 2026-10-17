using Sim.State;

namespace Sim.Combat;

/// <summary>Starts a defender's pushback slide when a hit or block connects (<c>Sim.Collision.PushbackStage</c> moves it).</summary>
/// <remarks>A new hit replaces a slide still in progress, as each hit pushes the defender from where it is.</remarks>
public sealed class Pushback
{
    private readonly int _frames;

    /// <param name="frames">Frames a slide takes (<c>MatchRules.PushbackFrames</c>); 0 turns pushback off.</param>
    public Pushback(int frames)
    {
        _frames = frames;
    }

    /// <param name="direction">The way the attack faced: +1 pushes the defender right.</param>
    /// <param name="pushesAttacker">The wall passes what it stops on to the attacker (not for projectiles).</param>
    public void Start(ref FighterState defender, int distance, int direction, bool pushesAttacker)
    {
        if (_frames == 0 || distance <= 0)
        {
            return;
        }

        defender.Pushback = new PushbackSlide
        {
            DistanceLeft = distance,
            FramesLeft = _frames,
            Direction = direction >= 0 ? 1 : -1,
            PushesAttacker = pushesAttacker ? 1 : 0,
        };
    }
}
