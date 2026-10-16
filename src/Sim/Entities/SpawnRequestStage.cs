using System;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Entities;

/// <summary>Applies the modules' spawn requests of this frame (ADR-0011 core request), then clears them.</summary>
/// <remarks>
/// Runs once per frame after the end-of-frame hooks (first in the round and timer step), so a request from any hook
/// spawns at the end of the frame it was made in: the entity first moves and can first hit on the next frame. Order:
/// module slot by slot, each owner in turn, through the same pool rules as moves (<see cref="ProjectileSpawner"/>).
/// A projectile the owner's character doesn't have is a module bug and throws, naming the slot, rather than being
/// skipped. Requests never outlive the tick, so snapshots don't need them.
/// </remarks>
public sealed class SpawnRequestStage : ITickStage
{
    private readonly OwnerCharacters _characters;
    private readonly ProjectileSpawner _spawner;
    private readonly RequestTable<SpawnRequest> _requests;

    public SpawnRequestStage(OwnerCharacters characters, ProjectileSpawner spawner, CoreRequests requests)
    {
        _characters = characters;
        _spawner = spawner;
        _requests = requests.Spawns;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            for (int owner = 0; owner < MatchState.PlayerCount; owner++)
            {
                if (_requests.TryGet(CoreRequests.FighterKey(slot, owner), out SpawnRequest request))
                {
                    Validate(slot, owner, in request);
                    _spawner.TrySpawn(ref state, owner, request.Projectile);
                }
            }
        }

        _requests.Clear();
    }

    private void Validate(int slot, int owner, in SpawnRequest request)
    {
        int projectiles = _characters.Of(owner).Projectiles.Length;
        if (request.Projectile < 0 || request.Projectile >= projectiles)
        {
            throw new InvalidOperationException(
                $"The module in slot {slot} requested projectile {request.Projectile} for player {owner + 1}, whose " +
                $"character has {projectiles}; the core can't spawn it.");
        }
    }
}
