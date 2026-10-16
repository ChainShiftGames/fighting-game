using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Tests.Matches;

/// <summary>
/// Seeded random input words that play like a (bad) fighter: held directions, button presses, jumps, dashes and
/// motions (236, 623, 214, 236236), so a match exercises moves, specials, projectiles, hits and rounds.
/// </summary>
/// <remarks>
/// Its own xorshift generator, not <see cref="System.Random"/>, so the same seed gives the same inputs on every
/// .NET version: golden replays are generated from it. Motions don't know the fighter's facing, so each is played
/// to a random side and works about half the time. Button bits are the default set's (a, b, c, d, special, dash on
/// bits 4–9); the input step drops any the button set doesn't define.
/// </remarks>
internal sealed class FightingInputs : IInputSource
{
    private const ushort Up = InputWord.Up;
    private const ushort Down = InputWord.Down;
    private const ushort Forward = InputWord.Right;
    private const ushort Back = InputWord.Left;
    private const int FirstButtonBit = InputWord.FirstButtonBit;
    private const int AttackButtons = 4;
    private const int AllButtons = 6;

    private static readonly ushort[] _directions =
        [0, Up, Down, Forward, Back, Up | Forward, Up | Back, Down | Forward, Down | Back];

    private static readonly ushort[][] _motions =
    [
        [Down, Down | Forward, Forward],
        [Forward, Down, Down | Forward],
        [Down, Down | Back, Back],
        [Down, Down | Forward, Forward, Down, Down | Forward, Forward],
    ];

    private readonly ushort[] _planned = new ushort[64];
    private uint _state;
    private int _count;
    private int _next;

    public FightingInputs(uint seed)
    {
        _state = seed == 0 ? 1u : seed;
    }

    public InputWord Next()
    {
        if (_next == _count)
        {
            Plan();
        }

        return new InputWord(_planned[_next++]);
    }

    private void Plan()
    {
        _count = 0;
        _next = 0;
        int kind = Below(100);
        if (kind < 35)
        {
            Hold(_directions[Below(_directions.Length)], 4 + Below(27));
        }
        else if (kind < 70)
        {
            ushort direction = _directions[Below(_directions.Length)];
            Hold((ushort)(direction | Button(AttackButtons)), 1 + Below(3));
            Hold(direction, 1 + Below(6));
        }
        else if (kind < 90)
        {
            PlayMotion(_motions[Below(_motions.Length)], Below(2) == 0);
        }
        else
        {
            Hold(Forward, 1);
            Hold(0, 1);
            Hold(Forward, 6 + Below(20));
        }
    }

    // Each step of the motion for a frame or two, the button with the last one.
    private void PlayMotion(ushort[] motion, bool isMirrored)
    {
        for (int step = 0; step < motion.Length; step++)
        {
            ushort direction = isMirrored ? Mirror(motion[step]) : motion[step];
            bool isLast = step == motion.Length - 1;
            Hold(isLast ? (ushort)(direction | Button(AllButtons)) : direction, 1 + Below(2));
        }

        Hold(0, 2 + Below(10));
    }

    private void Hold(ushort bits, int frames)
    {
        for (int frame = 0; frame < frames && _count < _planned.Length; frame++)
        {
            _planned[_count++] = bits;
        }
    }

    private ushort Button(int buttons) => (ushort)(1 << (FirstButtonBit + Below(buttons)));

    private static ushort Mirror(ushort direction)
    {
        ushort vertical = (ushort)(direction & (Up | Down));
        ushort forward = (direction & Forward) != 0 ? Back : (ushort)0;
        ushort back = (direction & Back) != 0 ? Forward : (ushort)0;
        return (ushort)(vertical | forward | back);
    }

    private int Below(int bound) => (int)(NextBits() % (uint)bound);

    // xorshift32 (Marsaglia).
    private uint NextBits()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return _state;
    }
}
