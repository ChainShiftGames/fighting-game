using System;
using System.Collections.Generic;
using System.Linq;
using Sim.Events;
using Sim.State;
using Xunit;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

/// <summary>P1 and P2 are treated alike: swapping and mirroring the players mirrors every entity result.</summary>
public sealed class EntityMirrorTests
{
    private const int Frames = 400;

    [Fact]
    public void SwappingAndMirroringThePlayersMirrorsEntitiesFightersAndEvents()
    {
        EntityStages originalStages = new(entitiesPerPlayer: 3);
        EntityStages swappedStages = new(entitiesPerPlayer: 3);
        MatchState original = default;
        original.Fighters[0] = At(-120_000, 1);
        original.Fighters[1] = At(90_000, -1);
        MatchState swapped = Mirror(original);
        int events = 0;

        for (int frame = 0; frame < Frames; frame++)
        {
            for (int role = 0; role < MatchState.PlayerCount; role++)
            {
                EntityScript.Play(ref original, originalStages.Requests, frame, role, fighter: role);
                EntityScript.Play(ref swapped, swappedStages.Requests, frame, role, fighter: 1 - role);
            }

            originalStages.Tick(ref original);
            swappedStages.Tick(ref swapped);
            events += AssertMirrored(in original, in swapped);
        }

        Assert.True(events > 20, $"Only {events} entity events happened; the script doesn't exercise the rules.");
    }

    private static MatchState Mirror(MatchState state)
    {
        MatchState mirrored = state;
        mirrored.Fighters[0] = MirrorFighter(state.Fighters[1]);
        mirrored.Fighters[1] = MirrorFighter(state.Fighters[0]);
        return mirrored;
    }

    private static FighterState MirrorFighter(FighterState fighter)
    {
        fighter.Position = fighter.Position.MirrorX();
        fighter.Velocity = fighter.Velocity.MirrorX();
        fighter.Facing = -fighter.Facing;
        return fighter;
    }

    private static EntityState MirrorEntity(EntityState entity)
    {
        if (entity.IsActive == 0)
        {
            return entity;
        }

        entity.Owner = 1 - entity.Owner;
        entity.Position = entity.Position.MirrorX();
        entity.Velocity = entity.Velocity.MirrorX();
        entity.Facing = -entity.Facing;
        return entity;
    }

    // Returns how many events the frame had, to show the script keeps the rules busy.
    private static int AssertMirrored(in MatchState original, in MatchState swapped)
    {
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            int other = 1 - player;
            Assert.Equal(MirrorFighter(original.Fighters[player]), swapped.Fighters[other]);
            for (int i = 0; i < EntityPool.MaxPerPlayer; i++)
            {
                Assert.Equal(
                    MirrorEntity(original.Entities.Slots[EntityPool.FirstSlotOf(player) + i]),
                    swapped.Entities.Slots[EntityPool.FirstSlotOf(other) + i]);
            }
        }

        // Events of both owners interleave in slot order, so compare them as sets.
        int frame = original.Frame - 1;
        List<(FrameEventKind, int, int, int, int)> expected = original.Events.OfFrame(frame).ToArray()
            .Select(e => (e.Kind, 1 - e.Fighter, -e.Position.X, e.Position.Y, e.Detail)).Order().ToList();
        List<(FrameEventKind, int, int, int, int)> actual = swapped.Events.OfFrame(frame).ToArray()
            .Select(e => (e.Kind, e.Fighter, e.Position.X, e.Position.Y, e.Detail)).Order().ToList();
        Assert.Equal(expected, actual);
        return expected.Count;
    }
}
