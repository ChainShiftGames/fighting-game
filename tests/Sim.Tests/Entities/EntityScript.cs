using Sim.Fighters.States;
using Sim.Input;
using Sim.Modules;
using Sim.State;
using static Sim.Tests.Entities.ProjectileCharacter;

namespace Sim.Tests.Entities;

/// <summary>
/// A busy, deterministic stand-in for the move state machines in entity tests: both fighters keep casting
/// projectiles, block now and then, and a module requests spawns, so every entity rule runs.
/// </summary>
/// <remarks>
/// The script is written per role (0 or 1), not per port, so a test can play role 0 as P2 to check the mirror.
/// </remarks>
internal static class EntityScript
{
    private const int ModuleSlot = 0;

    /// <summary>Prepares <paramref name="frame"/> for the fighter playing <paramref name="role"/>.</summary>
    public static void Play(ref MatchState state, CoreRequests requests, int frame, int role, int fighter)
    {
        ref FighterState self = ref state.Fighters[fighter];
        int castEvery = role == 0 ? 40 : 55;
        if (frame % castEvery == role * 7)
        {
            Cast(ref self, role == 0 ? Fireball : Sturdy);
        }
        else if (self.Action == FighterAction.Attack)
        {
            self.MoveFrame++;
        }

        // Back is away from the opponent, which faces the other way.
        bool holdsBack = role == 1 && frame % 60 >= 20 && frame % 60 < 30;
        int back = self.Facing > 0 ? InputWord.Left : InputWord.Right;
        state.Inputs[fighter].Record(new InputWord((ushort)(holdsBack ? back : 0)));

        if (frame % 33 == 5 + role)
        {
            int projectile = role == 0 ? SturdyProjectile : FireballProjectile;
            var request = new SpawnRequest { Projectile = projectile };
            requests.Spawns.Set(CoreRequests.FighterKey(ModuleSlot, fighter), request);
        }
    }

    private static void Cast(ref FighterState fighter, int move)
    {
        fighter.Action = FighterAction.Attack;
        fighter.Move = move;
        fighter.MoveFrame = 1;
        fighter.StunFramesLeft = 0;
    }
}
