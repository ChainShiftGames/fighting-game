using Sim.Characters;
using Sim.Math;
using Sim.State;

namespace Sim.Fighters.Movement;

/// <summary>Moves one fighter by one of its own frames: velocity, gravity, takeoff and landing.</summary>
/// <remarks>
/// Velocity comes from, in order: a jump starting this frame (<see cref="FighterState.Takeoff"/>), the current move's
/// velocity data for this frame, then on the ground the action (walking) and in the air the momentum the fighter
/// already has. Gravity pulls on airborne frames that move data doesn't cover. Landing happens when an airborne
/// fighter reaches the ground (y = 0) moving down: it stops, and its air actions are refilled. All values are
/// character data, so both fighters follow the same rules. Walls and pushboxes come after this step (P1-10).
/// </remarks>
public sealed class FighterMovement
{
    public void Advance(ref FighterState fighter, CharacterData character)
    {
        CharacterStats stats = character.Stats;
        bool hasMoveVelocity = MoveVelocity.TryFind(
            character, in character.Moves[fighter.Move], fighter.MoveFrame, out VelocityData moveVelocity);
        if (fighter.Takeoff != Takeoff.None)
        {
            fighter.Velocity = TakeoffVelocity.For(fighter.Takeoff, fighter.TakeoffDirection, fighter.Facing, in stats);
            fighter.IsAirborne = 1;
            hasMoveVelocity = false;
        }
        else if (hasMoveVelocity)
        {
            fighter.Velocity = new IntVector2(moveVelocity.X * fighter.Facing, moveVelocity.Y);
        }
        else if (fighter.IsAirborne == 0)
        {
            fighter.Velocity = new IntVector2(GroundVelocity.For(fighter.Action, fighter.Facing, in stats), 0);
        }

        fighter.Position += fighter.Velocity;
        if (fighter.IsAirborne == 0)
        {
            KeepOnGround(ref fighter);
        }
        else if (fighter.Position.Y <= 0 && fighter.Velocity.Y <= 0)
        {
            Land(ref fighter);
        }
        else if (!hasMoveVelocity)
        {
            fighter.Velocity = new IntVector2(fighter.Velocity.X, fighter.Velocity.Y - stats.Gravity);
        }
    }

    // A grounded fighter whose move data lifts it leaves the ground; anything pushing it down stops at the floor.
    private static void KeepOnGround(ref FighterState fighter)
    {
        if (fighter.Position.Y > 0)
        {
            fighter.IsAirborne = 1;
            return;
        }

        fighter.Position = new IntVector2(fighter.Position.X, 0);
        fighter.Velocity = new IntVector2(fighter.Velocity.X, 0);
    }

    private static void Land(ref FighterState fighter)
    {
        fighter.Position = new IntVector2(fighter.Position.X, 0);
        fighter.Velocity = IntVector2.Zero;
        fighter.IsAirborne = 0;
        fighter.AirActionsUsed = 0;
    }
}
