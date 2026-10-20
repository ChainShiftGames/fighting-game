using System;
using System.Collections.Generic;
using FightingGame.Presentation.Events;
using Sim.Characters;
using Sim.Events;
using Sim.Math;

namespace FightingGame.Presentation.World.Sparks;

/// <summary>
/// Works out how each hit spark looks from its hit: strength from the hitstop, direction from the launch, style
/// from the strength and the move's category, and anything the character's art file sets for that hit.
/// </summary>
/// <remarks>
/// So most hits need no art settings at all. Built once at match start (art settings by move index, no string
/// lookups per frame); engine-free so it can be tested.
/// </remarks>
public sealed class HitSparkLooks
{
    // Developer settings (presentation): the hitstop of the lightest and heaviest hits (weight 0 and 1), what a counter
    // hit adds, what a block keeps, the weight a hit tears from without being told, and a throw tech's weight (it has
    // no hit of its own).
    private const int LightHitstop = 8;
    private const int HeavyHitstop = 20;
    private const float CounterWeight = 0.3f;
    private const float BlockShare = 0.6f;
    private const float TearFromWeight = 0.5f;
    private const float TechWeight = 0.3f;

    private readonly CharacterData[] _characters;

    // Per player, per move index, per hit of that move: the art file's settings, or null.
    private readonly HitSparkSettings?[][][] _settings;

    /// <param name="characters">P1's and P2's character.</param>
    /// <param name="sparks">Per player, the art file's sparks by move ID (a list per hit); null for none.</param>
    public HitSparkLooks(
        CharacterData[] characters, IReadOnlyDictionary<string, IReadOnlyList<HitSparkSettings>>?[] sparks)
    {
        _characters = characters;
        _settings = new HitSparkSettings?[characters.Length][][];
        for (int player = 0; player < characters.Length; player++)
        {
            _settings[player] = ByMove(characters[player], player < sparks.Length ? sparks[player] : null);
        }
    }

    public HitSparkLook Of(in LiveSpark spark)
    {
        FrameEvent contact = spark.Event;
        int facing = spark.Facing < 0 ? -1 : 1;
        HitSparkTone tone = ToneOf(contact.Kind, spark.Counter);
        float seed = SeedOf(contact.Id);
        if (!TryFindHit(in contact, out HitData hit, out MoveCategory category, out HitSparkSettings? settings))
        {
            return new HitSparkLook(
                HitSparkStyle.Burst, tone, TechWeight, OnScreen(0f, facing), contact.Position, seed, 1f);
        }

        float weight = WeightOf(hit.Hitstop, tone);
        HitSparkStyle style = settings?.Style ?? DefaultStyle(weight, category, tone);
        float angle = settings?.Angle is float degrees ? degrees * MathF.PI / 180f : LaunchAngle(in hit);
        var offset = new IntVector2(
            (int)MathF.Round((settings?.OffsetX ?? 0f) * facing * Subpixels.PerPixel),
            (int)MathF.Round((settings?.OffsetY ?? 0f) * Subpixels.PerPixel));
        return new HitSparkLook(
            style, tone, weight, OnScreen(angle, facing), contact.Position + offset, seed, settings?.Scale ?? 1f);
    }

    private static HitSparkSettings?[][] ByMove(
        CharacterData character, IReadOnlyDictionary<string, IReadOnlyList<HitSparkSettings>>? sparks)
    {
        var byMove = new HitSparkSettings?[character.Moves.Length][];
        for (int move = 0; move < byMove.Length; move++)
        {
            byMove[move] = new HitSparkSettings?[character.Moves[move].HitCount];
            if (sparks is null || !sparks.TryGetValue(character.MoveIds[move], out IReadOnlyList<HitSparkSettings>? hits))
            {
                continue;
            }

            for (int hit = 0; hit < byMove[move].Length && hit < hits.Count; hit++)
            {
                byMove[move][hit] = hits[hit];
            }
        }

        return byMove;
    }

    private bool TryFindHit(
        in FrameEvent contact, out HitData hit, out MoveCategory category, out HitSparkSettings? settings)
    {
        hit = default;
        category = MoveCategory.None;
        settings = null;
        if (contact.Fighter < 0 || contact.Fighter >= _characters.Length)
        {
            return false;
        }

        CharacterData character = _characters[contact.Fighter];
        switch (contact.Kind)
        {
            case FrameEventKind.Hit or FrameEventKind.Block or FrameEventKind.Clash:
                return TryFindMoveHit(
                    contact.Fighter,
                    HitEventDetail.MoveOf(contact.Detail),
                    HitEventDetail.HitOf(contact.Detail),
                    out hit,
                    out category,
                    out settings);
            case FrameEventKind.ThrowHit:
                return TryFindMoveHit(contact.Fighter, contact.Detail, 0, out hit, out category, out settings);
            case FrameEventKind.ProjectileHit or FrameEventKind.ProjectileBlock or FrameEventKind.ProjectileClash
                when contact.Detail >= 0 && contact.Detail < character.Projectiles.Length:
                hit = character.Projectiles[contact.Detail].Hit;
                category = MoveCategory.Special;
                return true;
            default:
                return false;
        }
    }

    private bool TryFindMoveHit(
        int player, int move, int hitIndex, out HitData hit, out MoveCategory category, out HitSparkSettings? settings)
    {
        CharacterData character = _characters[player];
        hit = default;
        category = MoveCategory.None;
        settings = null;
        if (move < 0 || move >= character.Moves.Length)
        {
            return false;
        }

        ReadOnlySpan<HitData> hits = character.HitsOf(in character.Moves[move]);
        if (hitIndex < 0 || hitIndex >= hits.Length)
        {
            return false;
        }

        hit = hits[hitIndex];
        category = character.Moves[move].Category;
        settings = _settings[player][move][hitIndex];
        return true;
    }

    private static float WeightOf(int hitstop, HitSparkTone tone)
    {
        float weight = Math.Clamp((float)(hitstop - LightHitstop) / (HeavyHitstop - LightHitstop), 0f, 1f);
        return tone switch
        {
            HitSparkTone.Counter => weight + CounterWeight,
            HitSparkTone.Block => weight * BlockShare,
            _ => weight,
        };
    }

    // Blocks and throws burst; specials, supers and heavy hits tear.
    private static HitSparkStyle DefaultStyle(float weight, MoveCategory category, HitSparkTone tone)
    {
        if (tone == HitSparkTone.Block || category == MoveCategory.Throw)
        {
            return HitSparkStyle.Burst;
        }

        bool special = (category & (MoveCategory.Special | MoveCategory.Super)) != 0;
        return special || weight >= TearFromWeight ? HitSparkStyle.Tear : HitSparkStyle.Burst;
    }

    // A launch sends the defender the way the attack travels; without one, the attack goes straight forward.
    private static float LaunchAngle(in HitData hit) =>
        hit.HasLaunch == 1 && hit.Launch != default ? MathF.Atan2(hit.Launch.Y, hit.Launch.X) : 0f;

    // Facing left mirrors the angle about the vertical.
    private static float OnScreen(float angle, int facing) => facing > 0 ? angle : MathF.PI - angle;

    private static HitSparkTone ToneOf(FrameEventKind kind, FrameEventKind counter) => kind switch
    {
        FrameEventKind.Block or FrameEventKind.ProjectileBlock => HitSparkTone.Block,
        FrameEventKind.Clash or FrameEventKind.ProjectileClash or FrameEventKind.ThrowTech => HitSparkTone.Clash,
        _ when counter != FrameEventKind.None => HitSparkTone.Counter,
        _ => HitSparkTone.Hit,
    };

    // A hash of the event's id, so a spark varies from hit to hit but looks the same every time it is shown.
    private static float SeedOf(FrameEventId id)
    {
        uint hash = (uint)id.Frame * 2_654_435_761u;
        hash ^= ((uint)id.Kind * 40_503u) + ((uint)id.Fighter * 97u) + (uint)id.Occurrence;
        hash ^= hash >> 15;
        hash *= 2_246_822_519u;
        hash ^= hash >> 13;
        return (hash & 0xFFFF) / 65_536f;
    }
}
