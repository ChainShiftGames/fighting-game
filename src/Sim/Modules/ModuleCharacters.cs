using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.State;

namespace Sim.Modules;

/// <summary>The match's characters as modules see them (ADR-0023): read-only data, and each module's roles as moves.</summary>
/// <remarks>
/// Built once by <see cref="ModuleLineup"/>: role names are strings, so they are resolved to move indexes here, at
/// match start, and a hook's lookup is an array read.
/// </remarks>
internal sealed class ModuleCharacters
{
    private readonly CharacterData[] _characters;

    // Per slot: role i of fighter f is at [slot][f * roleCount + i].
    private readonly int[][] _roleMoves;

    /// <exception cref="ArgumentException">A character doesn't name a role a module in the lineup needs.</exception>
    public ModuleCharacters(IMechanicModule?[] modulesBySlot, CharacterData player1, CharacterData player2)
    {
        _characters = [player1, player2];
        _roleMoves = new int[modulesBySlot.Length][];
        for (int slot = 0; slot < modulesBySlot.Length; slot++)
        {
            _roleMoves[slot] = modulesBySlot[slot] is { } module ? ResolveRoles(module) : [];
        }
    }

    public CharacterData CharacterOf(int fighter) => _characters[fighter];

    /// <exception cref="ArgumentOutOfRangeException">The module in the slot has no role at that index.</exception>
    public int RoleMove(int slot, int fighter, int role)
    {
        int[] moves = _roleMoves[slot];
        int roleCount = moves.Length / MatchState.PlayerCount;
        ArgumentOutOfRangeException.ThrowIfNegative(role);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(role, roleCount);
        return moves[(fighter * roleCount) + role];
    }

    private int[] ResolveRoles(IMechanicModule module)
    {
        IReadOnlyList<string> roles = module.MoveRoles;
        int[] moves = new int[roles.Count * MatchState.PlayerCount];
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            for (int role = 0; role < roles.Count; role++)
            {
                int move = _characters[fighter].MoveForRole(roles[role]);
                if (move == CharacterData.NoMove)
                {
                    throw new ArgumentException(
                        $"Character '{_characters[fighter].Id}' names no '{roles[role]}' move, which module " +
                        $"'{module.Name}' needs (moveRoles in its character.json).");
                }

                moves[(fighter * roles.Count) + role] = move;
            }
        }

        return moves;
    }
}
