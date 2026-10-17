using Sim.Characters;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Entities;

/// <summary>The spawn part of the movement step: moves spawn their projectiles on their data's frames.</summary>
/// <remarks>
/// A projectile spawns when its owner advances this frame and is on the projectile's spawn frame of its current move,
/// so a fighter in hitstop or slowed down still spawns it exactly once, on the frame it reaches that move frame.
/// Spawning happens after the fighters have moved, from where the owner stands then, and before hit detection, so a
/// projectile can hit on its spawn frame, and it is out even if its owner gets hit later in that frame. The owners'
/// pools are separate, so P1 spawning first can't affect P2 (<see cref="ProjectileSpawner"/>).
/// </remarks>
public sealed class MoveSpawnStage : ITickStage
{
    private readonly OwnerCharacters _characters;
    private readonly ProjectileSpawner _spawner;

    public MoveSpawnStage(OwnerCharacters characters, ProjectileSpawner spawner)
    {
        _characters = characters;
        _spawner = spawner;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        for (int owner = 0; owner < MatchState.PlayerCount; owner++)
        {
            if (activity.Advances(owner))
            {
                SpawnFromMove(ref state, owner);
            }
        }
    }

    private void SpawnFromMove(ref MatchState state, int owner)
    {
        CharacterData character = _characters.Of(owner);
        MoveData move = character.Moves[state.Fighters[owner].Move];
        int moveFrame = state.Fighters[owner].MoveFrame;
        int end = move.ProjectileStart + move.ProjectileCount;
        for (int projectile = move.ProjectileStart; projectile < end; projectile++)
        {
            if (character.Projectiles[projectile].SpawnFrame == moveFrame)
            {
                _spawner.TrySpawn(ref state, owner, projectile, state.Fighters[owner].IsShortcutMove != 0);
            }
        }
    }
}
