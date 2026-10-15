using System;
using Sim.Input;

namespace Sim.Toy;

/// <summary>Spike A toy logic: two squares walk left/right and jump. Advances a <see cref="ToyState"/> one frame.</summary>
public sealed class ToySimulation
{
    private readonly ToyRules _rules;

    public ToySimulation(ToyRules rules)
    {
        _rules = rules;
    }

    public ToyState CreateInitialState()
    {
        var state = new ToyState();
        state.Fighters[0].PositionX = -_rules.StartDistance / 2;
        state.Fighters[1].PositionX = _rules.StartDistance / 2;
        return state;
    }

    public void Tick(ref ToyState state, InputWord player1, InputWord player2)
    {
        StepFighter(ref state.Fighters[0], SocdResolver.Resolve(player1));
        StepFighter(ref state.Fighters[1], SocdResolver.Resolve(player2));
        state.Frame++;
    }

    private void StepFighter(ref ToyFighter fighter, InputWord input)
    {
        bool grounded = fighter.PositionY == 0;
        if (grounded)
        {
            fighter.PositionX += WalkDirection(input) * _rules.WalkSpeed;
            if (input.IsHeld(InputWord.Up))
            {
                fighter.VelocityY = _rules.JumpVelocity;
            }
        }

        fighter.PositionY += fighter.VelocityY;
        fighter.VelocityY -= _rules.Gravity;
        if (fighter.PositionY <= 0)
        {
            fighter.PositionY = 0;
            fighter.VelocityY = 0;
        }

        fighter.PositionX = Math.Clamp(fighter.PositionX, -_rules.StageHalfWidth, _rules.StageHalfWidth);
    }

    private static int WalkDirection(InputWord input)
    {
        if (input.IsHeld(InputWord.Right))
        {
            return 1;
        }

        return input.IsHeld(InputWord.Left) ? -1 : 0;
    }
}
