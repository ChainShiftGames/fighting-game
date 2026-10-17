using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Netcode.Bots;

/// <summary>
/// The bot's state machine: from what it has seen, picks approach, attack, block or punish, and turns that into this
/// frame's input word.
/// </summary>
/// <remarks>
/// Deterministic: its only randomness is its own seeded <see cref="RandomState"/> (not the match's), so the same seed
/// and the same observations always give the same inputs. Its state is not game state and is never rolled back: its
/// words are local inputs, final once given, like a human's button presses.
/// </remarks>
public sealed class BotBrain
{
    // Up-forward held through the prejump, so the jump goes forward whichever frame the logic reads it on.
    private const int JumpHoldFrames = 4;

    // Forward held after the dash input, so a running dash keeps running for a moment.
    private const int DashHoldFrames = 10;

    private const int Percent = 100;

    private readonly MoveRepertoire _moves;
    private readonly int _dashMotion;
    private readonly BotSettings _settings;
    private readonly InputPlan _plan = new();
    private RandomState _random;
    private bool _isInDanger;
    private bool _guardsDanger;
    private int _framesUntilNextAttack;

    /// <param name="moves">The bot's attacks against this opponent.</param>
    /// <param name="dashMotion">The dash motion from the movement inputs in data (66).</param>
    /// <param name="settings">How it plays; already validated.</param>
    /// <param name="seed">Seeds its choices.</param>
    public BotBrain(MoveRepertoire moves, int dashMotion, BotSettings settings, uint seed)
    {
        _moves = moves;
        _dashMotion = dashMotion;
        _settings = settings;
        _random = RandomState.FromSeed(seed);
    }

    /// <summary>What the bot last decided; it doesn't decide while playing out a plan.</summary>
    public BotIntent Intent { get; private set; }

    /// <summary>This frame's input word, from what the bot has now reacted to.</summary>
    public InputWord Next(in BotObservation seen)
    {
        if (_framesUntilNextAttack > 0)
        {
            _framesUntilNextAttack--;
        }

        if (!seen.IsFighting)
        {
            _plan.Clear();
            _isInDanger = false;
            Intent = BotIntent.Wait;
            return InputWord.Neutral;
        }

        if (_plan.TryTake(out InputWord planned))
        {
            return planned;
        }

        Intent = Choose(in seen);
        return Act(Intent, in seen);
    }

    private BotIntent Choose(in BotObservation seen)
    {
        FighterView self = seen.Self;
        bool guards = Guards(in seen);
        bool isAttackComing = seen.Opponent.IsThreatening
            || seen.IncomingProjectileDistance != BotObservation.NoProjectile;
        if (self.Pose == FighterPose.Blockstun || (guards && isAttackComing))
        {
            return BotIntent.Block;
        }

        bool canAttack = _framesUntilNextAttack == 0;
        if (self.IsAirborne)
        {
            return canAttack && self.Pose == FighterPose.Jumping && Roll(_settings.AttackChancePercent)
                ? BotIntent.Attack
                : BotIntent.Wait;
        }

        if (!self.IsFreeOnGround)
        {
            return BotIntent.Wait;
        }

        if (canAttack && seen.Opponent.IsRecovering && _moves.TryPickFastest(seen.Distance, out _))
        {
            return BotIntent.Punish;
        }

        if (seen.Distance > _moves.CloseRange)
        {
            return BotIntent.Approach;
        }

        if (canAttack && Roll(_settings.AttackChancePercent))
        {
            return BotIntent.Attack;
        }

        // Like a player in reach of the opponent: hold back between attacks rather than wait for one to be seen,
        // since a fast attack lands before anyone could react to it.
        return guards ? BotIntent.Block : BotIntent.Wait;
    }

    // In danger: the opponent can attack and is in reach, or a projectile is coming. Whether to guard is rolled once
    // each time the danger starts, not per frame, so the bot doesn't flicker between guarding and not.
    private bool Guards(in BotObservation seen)
    {
        bool isInDanger = (seen.Opponent.IsFreeOnGround || seen.Opponent.IsThreatening || seen.Opponent.IsAirborne)
            && seen.Distance <= _moves.ThreatRange;
        isInDanger |= seen.IncomingProjectileDistance <= _moves.ThreatRange;
        if (isInDanger && !_isInDanger)
        {
            _guardsDanger = Roll(_settings.BlockChancePercent);
        }

        _isInDanger = isInDanger;
        bool canGuard = seen.Self.IsFreeOnGround || seen.Self.Pose == FighterPose.Jumping;
        return isInDanger && _guardsDanger && canGuard;
    }

    private InputWord Act(BotIntent intent, in BotObservation seen) => intent switch
    {
        BotIntent.Block => Block(in seen),
        BotIntent.Punish => _moves.TryPickFastest(seen.Distance, out BotMove punish)
            ? StartAttack(in punish, in seen)
            : InputWord.Neutral,
        BotIntent.Attack => _moves.TryPickInReach(seen.Distance, StanceOf(seen.Self), ref _random, out BotMove attack)
            ? StartAttack(in attack, in seen)
            : InputWord.Neutral,
        BotIntent.Approach => Approach(in seen),
        _ => InputWord.Neutral,
    };

    // Back is away from the opponent; down-back against a low, standing against everything else.
    private static InputWord Block(in BotObservation seen)
    {
        ushort back = seen.OpponentSide > 0 ? InputWord.Left : InputWord.Right;
        bool isLow = seen.Opponent.IsThreatening && seen.Opponent.AttackGuard == Guard.Low;
        return new InputWord(isLow ? (ushort)(back | InputWord.Down) : back);
    }

    // After an attack, the bot waits for its reaction time before attacking again: it has to see what happened.
    private InputWord StartAttack(in BotMove move, in BotObservation seen)
    {
        _plan.Motion(move.Motion, move.ButtonMask, seen.Self.Facing);
        _framesUntilNextAttack = _settings.ReactionDelayFrames;
        return TakePlanned();
    }

    private InputWord Approach(in BotObservation seen)
    {
        int facing = seen.Self.Facing;
        if (Roll(_settings.ProjectileChancePercent) && _moves.TryPickProjectile(ref _random, out BotMove projectile))
        {
            return StartAttack(in projectile, in seen);
        }

        ushort forward = InputPlan.NumpadBits(6, facing);
        if (Roll(_settings.DashChancePercent))
        {
            _plan.Motion(_dashMotion, 0, facing);
            _plan.Hold(forward, DashHoldFrames);
            return TakePlanned();
        }

        if (Roll(_settings.JumpChancePercent))
        {
            _plan.Hold(InputPlan.NumpadBits(9, facing), JumpHoldFrames);
            return TakePlanned();
        }

        return new InputWord(forward);
    }

    private InputWord TakePlanned()
    {
        _plan.TryTake(out InputWord first);
        return first;
    }

    private static MoveStance StanceOf(FighterView self) => self.IsAirborne ? MoveStance.Air : MoveStance.Ground;

    private bool Roll(int percent) => _random.NextBelow(Percent) < percent;
}
