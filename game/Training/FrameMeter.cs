using System;
using Sim.Diagnostics;
using Sim.State;

namespace FightingGame.Training;

/// <summary>
/// The frame meter's contents (Strive/SF6 style): one column per frame for each fighter while something happens, the
/// training player's startup and the frame advantage once both fighters can act again.
/// </summary>
/// <remarks>
/// A stretch starts on the first frame either fighter is busy and clears the previous one; it ends on the first frame
/// both are free again. Frames frozen for both (hitstop, super freeze) are left out by the caller, as the games do.
/// The newest <see cref="Columns"/> frames are kept. Godot-free, so it is tested without the engine.
/// </remarks>
public sealed class FrameMeter
{
    /// <summary>Frames the bar shows; older frames scroll off its left end.</summary>
    public const int Columns = 80;

    private const int Player = 0;
    private const int Opponent = 1;

    private readonly FighterFrameState[][] _columns =
        [new FighterFrameState[Columns], new FighterFrameState[Columns]];

    private readonly bool[] _wasBusy = new bool[MatchState.PlayerCount];
    private readonly int[] _freeAt = new int[MatchState.PlayerCount];
    private bool _isRunning;
    private int _frame;
    private int _playerStartup;

    /// <summary>Columns filled so far in the current stretch, at most <see cref="Columns"/>.</summary>
    public int Count { get; private set; }

    /// <summary>The training player's first attack's startup (its first active frame); null before it is active.</summary>
    public int? Startup { get; private set; }

    /// <summary>
    /// The training player's frame advantage once both fighters were busy and are free again: positive when the
    /// player acts first. Null while the stretch runs or when only one fighter was busy (a whiff).
    /// </summary>
    public int? Advantage { get; private set; }

    /// <summary>A column, oldest first: <paramref name="column"/> in 0..<see cref="Count"/>.</summary>
    public FighterFrameState At(int fighter, int column) => _columns[fighter][column];

    /// <summary>Adds one frame that wasn't frozen for both fighters.</summary>
    public void Record(FighterFrameState player, FighterFrameState opponent)
    {
        bool bothFree = player.Kind == FrameKind.Free && opponent.Kind == FrameKind.Free;
        if (!_isRunning)
        {
            if (bothFree)
            {
                return;
            }

            Begin();
        }

        Append(player, opponent);
        Track(Player, player);
        Track(Opponent, opponent);
        TrackStartup(player);
        if (bothFree)
        {
            End();
        }

        _frame++;
    }

    /// <summary>Empties the meter, e.g. when training restarts or a savestate is loaded.</summary>
    public void Clear()
    {
        _isRunning = false;
        Count = 0;
        Startup = null;
        Advantage = null;
    }

    private void Begin()
    {
        Clear();
        _isRunning = true;
        _frame = 0;
        _playerStartup = 0;
        Array.Clear(_wasBusy);
        Array.Clear(_freeAt);
    }

    private void Append(FighterFrameState player, FighterFrameState opponent)
    {
        if (Count == Columns)
        {
            Array.Copy(_columns[Player], 1, _columns[Player], 0, Columns - 1);
            Array.Copy(_columns[Opponent], 1, _columns[Opponent], 0, Columns - 1);
            Count--;
        }

        _columns[Player][Count] = player;
        _columns[Opponent][Count] = opponent;
        Count++;
    }

    // Remembers the frame each fighter became free after being busy: the frames between them are the advantage.
    private void Track(int fighter, FighterFrameState state)
    {
        if (state.Kind != FrameKind.Free)
        {
            _wasBusy[fighter] = true;
            _freeAt[fighter] = -1;
        }
        else if (_wasBusy[fighter] && _freeAt[fighter] < 0)
        {
            _freeAt[fighter] = _frame;
        }
    }

    private void TrackStartup(FighterFrameState player)
    {
        if (Startup is not null)
        {
            return;
        }

        if (player.Kind == FrameKind.Startup)
        {
            _playerStartup++;
        }
        else if (player.Kind == FrameKind.Active)
        {
            Startup = _playerStartup + 1;
        }
    }

    private void End()
    {
        _isRunning = false;
        Advantage = _wasBusy[Player] && _wasBusy[Opponent] ? _freeAt[Opponent] - _freeAt[Player] : null;
    }
}
