using System;
using Sim.Characters;
using Sim.State;

namespace Netcode.Bots;

/// <summary>
/// The attacks a bot's character has and how far each reaches against this opponent, read once from character data
/// at match start, the way a player knows their character's moves and the opponent's.
/// </summary>
/// <remarks>Built once; the per-frame queries walk a fixed array and allocate nothing.</remarks>
public sealed class MoveRepertoire
{
    private readonly BotMove[] _moves;

    private MoveRepertoire(BotMove[] moves, int closeRange, int threatRange)
    {
        _moves = moves;
        CloseRange = closeRange;
        ThreatRange = threatRange;
    }

    /// <summary>The longest reach of the bot's ground attacks, projectiles aside: inside it, the bot can hit.</summary>
    public int CloseRange { get; }

    /// <summary>The longest reach of the opponent's ground attacks, projectiles aside: inside it, it can hit.</summary>
    public int ThreatRange { get; }

    public ReadOnlySpan<BotMove> Moves => _moves;

    public static MoveRepertoire Build(CharacterData self, CharacterData opponent)
    {
        BotMove[] moves = AttacksOf(self, BodyDepthOf(opponent));
        return new MoveRepertoire(
            moves, LongestMeleeReach(moves), LongestMeleeReach(AttacksOf(opponent, BodyDepthOf(self))));
    }

    /// <summary>A random attack for <paramref name="stance"/> that reaches <paramref name="distance"/>.</summary>
    public bool TryPickInReach(int distance, MoveStance stance, ref RandomState random, out BotMove move)
    {
        int candidates = 0;
        foreach (BotMove candidate in _moves)
        {
            candidates += Fits(in candidate, distance, stance) ? 1 : 0;
        }

        int pick = random.NextBelow(candidates);
        foreach (BotMove candidate in _moves)
        {
            if (Fits(in candidate, distance, stance) && pick-- == 0)
            {
                move = candidate;
                return true;
            }
        }

        move = default;
        return false;
    }

    /// <summary>The fastest ground attack that reaches <paramref name="distance"/>: the one to punish with.</summary>
    /// <remarks>
    /// Throws and projectiles aside: a punish is a fast hit in reach. On a tie the earlier move in the character file
    /// wins.
    /// </remarks>
    public bool TryPickFastest(int distance, out BotMove move)
    {
        move = default;
        bool found = false;
        foreach (BotMove candidate in _moves)
        {
            bool isHit = candidate.Category != MoveCategory.Throw && !candidate.IsProjectile;
            bool isFaster = !found || candidate.StartupFrames < move.StartupFrames;
            if (isHit && isFaster && Fits(in candidate, distance, MoveStance.Ground))
            {
                move = candidate;
                found = true;
            }
        }

        return found;
    }

    /// <summary>A random ground projectile, if the character has any.</summary>
    public bool TryPickProjectile(ref RandomState random, out BotMove move)
    {
        int candidates = 0;
        foreach (BotMove candidate in _moves)
        {
            candidates += IsGroundProjectile(in candidate) ? 1 : 0;
        }

        int pick = random.NextBelow(candidates);
        foreach (BotMove candidate in _moves)
        {
            if (IsGroundProjectile(in candidate) && pick-- == 0)
            {
                move = candidate;
                return true;
            }
        }

        move = default;
        return false;
    }

    private static bool Fits(in BotMove move, int distance, MoveStance stance) =>
        move.Stance == stance && move.Reach >= distance;

    private static bool IsGroundProjectile(in BotMove move) => move.IsProjectile && move.Stance == MoveStance.Ground;

    private static BotMove[] AttacksOf(CharacterData character, int targetDepth)
    {
        int count = 0;
        BotMove[] found = new BotMove[character.Moves.Length];
        foreach (MoveData move in character.Moves)
        {
            int reach = ReachOf(character, in move, targetDepth);
            int startup = StartupOf(character, in move);
            if (move.Category == MoveCategory.Basic || move.ButtonMask == 0 || reach == 0 || startup == 0)
            {
                continue;
            }

            found[count++] = new BotMove(move.Category, move.Motion, move.ButtonMask, move.Stance, startup, reach);
        }

        return found.AsSpan(0, count).ToArray();
    }

    // Origin to the far end of its hit or throw boxes, plus the target's body in front of its origin.
    private static int ReachOf(CharacterData character, in MoveData move, int targetDepth)
    {
        if (!character.ProjectilesOf(in move).IsEmpty)
        {
            return BotMove.FullScreen;
        }

        int reach = 0;
        foreach (BoxData box in character.BoxesOf(in move))
        {
            if (box.Kind is BoxKind.Hit or BoxKind.Throw)
            {
                reach = Math.Max(reach, box.Area.MaxX + targetDepth);
            }
        }

        return reach;
    }

    private static int StartupOf(CharacterData character, in MoveData move)
    {
        ReadOnlySpan<ProjectileData> projectiles = character.ProjectilesOf(in move);
        return projectiles.IsEmpty ? move.FirstActiveFrame : projectiles[0].SpawnFrame;
    }

    // How far the character's standing hurtbox reaches in front of its origin.
    private static int BodyDepthOf(CharacterData character)
    {
        int idle = character.BasicStateMove(BasicState.Idle);
        if (idle == CharacterData.NoMove)
        {
            return 0;
        }

        int depth = 0;
        foreach (BoxData box in character.BoxesOf(in character.Moves[idle]))
        {
            if (box.Kind == BoxKind.Hurt)
            {
                depth = Math.Max(depth, box.Area.MaxX);
            }
        }

        return depth;
    }

    private static int LongestMeleeReach(ReadOnlySpan<BotMove> moves)
    {
        int longest = 0;
        foreach (BotMove move in moves)
        {
            if (!move.IsProjectile && move.Stance == MoveStance.Ground)
            {
                longest = Math.Max(longest, move.Reach);
            }
        }

        return longest;
    }
}
