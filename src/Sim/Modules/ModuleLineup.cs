using System;
using Sim.Characters;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>The match's enabled modules in hook order; the tick pipeline calls its hooks at fixed points.</summary>
/// <remarks>
/// Built once at match start from the <see cref="MatchRules"/> lineup, which is fixed for the match, followed by each
/// fighter's character mechanic (ADR-0023), P1 then P2. Within a hook point, modules run in slot order (the module
/// order from data, then the mechanics). Running hooks allocates nothing.
/// </remarks>
public sealed class ModuleLineup
{
    private readonly ModuleConfig[] _configs = new ModuleConfig[ModuleStates.MaxModules];
    private readonly CoreRequests _requests;
    private readonly HookList<IAfterInputHook> _afterInput;
    private readonly HookList<IBeforeStateMachinesHook> _beforeStateMachines;
    private readonly HookList<IAfterStateMachinesHook> _afterStateMachines;
    private readonly HookList<IOnHitHook> _onHit;
    private readonly HookList<IOnBlockHook> _onBlock;
    private readonly HookList<IOnThrowHook> _onThrow;
    private readonly HookList<IEndOfFrameHook> _endOfFrame;
    private readonly HookList<IAirGuardHook> _airGuard;
    private readonly MeterAccess _meters;
    private readonly HookList<IHitAdjustmentHook> _hitAdjustment;
    private readonly HookList<IBlockAdjustmentHook> _blockAdjustment;
    private readonly HookList<ICancelWindowHook> _cancelWindows;
    private readonly ModuleCharacters? _characters;

    /// <summary>A lineup without characters, for unit tests: a module asking for one throws.</summary>
    /// <exception cref="ArgumentException">The lineup names an unregistered module or one module twice.</exception>
    public ModuleLineup(ModuleCatalog catalog, in ModuleSlots slots, CoreRequests requests)
        : this(catalog, in slots, requests, null, null)
    {
    }

    /// <exception cref="ArgumentException">
    /// The lineup names an unregistered module, one module twice or a character mechanic; a character names a module
    /// that isn't a character mechanic; two meter gauges serve one channel for a fighter; or a character doesn't name a
    /// move role a module needs.
    /// </exception>
    public ModuleLineup(
        ModuleCatalog catalog, in ModuleSlots slots, CoreRequests requests, CharacterData? player1, CharacterData? player2)
    {
        _requests = requests;
        IMechanicModule?[] modulesBySlot = ResolveModules(catalog, in slots);
        for (int slot = 0; slot < ModuleStates.LineupSlots; slot++)
        {
            _configs[slot] = slots[slot].Config;
        }

        if (player1 is not null && player2 is not null)
        {
            AddMechanic(catalog, modulesBySlot, 0, player1);
            AddMechanic(catalog, modulesBySlot, 1, player2);
            _characters = new(modulesBySlot, player1, player2);
        }

        _afterInput = new(modulesBySlot);
        _beforeStateMachines = new(modulesBySlot);
        _afterStateMachines = new(modulesBySlot);
        _onHit = new(modulesBySlot);
        _onBlock = new(modulesBySlot);
        _onThrow = new(modulesBySlot);
        _endOfFrame = new(modulesBySlot);
        _airGuard = new(modulesBySlot);
        _meters = new MeterAccess(modulesBySlot, _configs, requests, _characters);
        _hitAdjustment = new(modulesBySlot);
        _blockAdjustment = new(modulesBySlot);
        _cancelWindows = new(modulesBySlot);
    }

    public void RunAfterInput(ref MatchState state)
    {
        for (int i = 0; i < _afterInput.Count; i++)
        {
            _afterInput.HookAt(i).AfterInput(ContextFor(ref state, _afterInput.SlotAt(i)));
        }
    }

    public void RunBeforeStateMachines(ref MatchState state)
    {
        for (int i = 0; i < _beforeStateMachines.Count; i++)
        {
            _beforeStateMachines.HookAt(i).BeforeStateMachines(ContextFor(ref state, _beforeStateMachines.SlotAt(i)));
        }
    }

    public void RunAfterStateMachines(ref MatchState state)
    {
        for (int i = 0; i < _afterStateMachines.Count; i++)
        {
            _afterStateMachines.HookAt(i).AfterStateMachines(ContextFor(ref state, _afterStateMachines.SlotAt(i)));
        }
    }

    public void RunOnHit(ref MatchState state, in FighterContact contact)
    {
        for (int i = 0; i < _onHit.Count; i++)
        {
            _onHit.HookAt(i).OnHit(ContextFor(ref state, _onHit.SlotAt(i)), in contact);
        }
    }

    public void RunOnBlock(ref MatchState state, in FighterContact contact)
    {
        for (int i = 0; i < _onBlock.Count; i++)
        {
            _onBlock.HookAt(i).OnBlock(ContextFor(ref state, _onBlock.SlotAt(i)), in contact);
        }
    }

    public void RunOnThrow(ref MatchState state, in FighterContact contact)
    {
        for (int i = 0; i < _onThrow.Count; i++)
        {
            _onThrow.HookAt(i).OnThrow(ContextFor(ref state, _onThrow.SlotAt(i)), in contact);
        }
    }

    public void RunEndOfFrame(ref MatchState state)
    {
        for (int i = 0; i < _endOfFrame.Count; i++)
        {
            _endOfFrame.HookAt(i).EndOfFrame(ContextFor(ref state, _endOfFrame.SlotAt(i)));
        }
    }

    /// <summary>True when any enabled module lets the airborne defender block (<see cref="IAirGuardHook"/>).</summary>
    public bool AllowsAirGuard(ref MatchState state, in AirGuardCheck check)
    {
        for (int i = 0; i < _airGuard.Count; i++)
        {
            if (_airGuard.HookAt(i).CanAirGuard(ContextFor(ref state, _airGuard.SlotAt(i)), in check))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lets the enabled modules adjust a hit about to land; false when one makes it whiff (<see cref="IHitAdjustmentHook"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">A module set a negative damage, hitstun or hitstop.</exception>
    public bool AdjustHit(ref MatchState state, in FighterContact contact, ref HitData hit)
    {
        for (int i = 0; i < _hitAdjustment.Count; i++)
        {
            int slot = _hitAdjustment.SlotAt(i);
            if (!_hitAdjustment.HookAt(i).AdjustHit(ContextFor(ref state, slot), in contact, ref hit))
            {
                return false;
            }

            if (hit.Damage < 0 || hit.Hitstun < 0 || hit.Hitstop < 0)
            {
                throw new InvalidOperationException(
                    $"The module in slot {slot} adjusted a hit to {hit.Damage} damage, {hit.Hitstun} hitstun and " +
                    $"{hit.Hitstop} hitstop, which the core can't apply.");
            }
        }

        return true;
    }

    /// <summary>Lets the enabled modules adjust a block about to happen (<see cref="IBlockAdjustmentHook"/>).</summary>
    /// <exception cref="InvalidOperationException">A module set a negative chip, blockstun, hitstop or pushback.</exception>
    public void AdjustBlock(ref MatchState state, in FighterContact contact, ref HitData hit)
    {
        for (int i = 0; i < _blockAdjustment.Count; i++)
        {
            int slot = _blockAdjustment.SlotAt(i);
            _blockAdjustment.HookAt(i).AdjustBlock(ContextFor(ref state, slot), in contact, ref hit);
            if (hit.ChipDamage < 0 || hit.Blockstun < 0 || hit.Hitstop < 0 || hit.Pushback < 0)
            {
                throw new InvalidOperationException(
                    $"The module in slot {slot} adjusted a block to {hit.ChipDamage} chip, {hit.Blockstun} blockstun, " +
                    $"{hit.Hitstop} hitstop and {hit.Pushback} pushback, which the core can't apply.");
            }
        }
    }

    /// <summary>True when any enabled module lets the fighter take a data cancel (<see cref="ICancelWindowHook"/>).</summary>
    public bool AllowsCancel(ref MatchState state, in CancelCheck check)
    {
        for (int i = 0; i < _cancelWindows.Count; i++)
        {
            if (_cancelWindows.HookAt(i).CanCancel(ContextFor(ref state, _cancelWindows.SlotAt(i)), in check))
            {
                return true;
            }
        }

        return false;
    }

    private ModuleContext ContextFor(ref MatchState state, int slot) =>
        new(ref state, ref state.Modules[slot], in _configs[slot], _requests, slot, _meters, _characters);

    private void AddMechanic(ModuleCatalog catalog, IMechanicModule?[] modulesBySlot, int fighter, CharacterData character)
    {
        ModuleId id = character.Mechanic.Module;
        if (id.IsNone)
        {
            return;
        }

        if (!catalog.TryFind(id, out IMechanicModule? module) || !module!.IsCharacterMechanic)
        {
            throw new ArgumentException(
                $"Character '{character.Id}' names module {id} as its mechanic, which isn't a registered character mechanic.");
        }

        int slot = ModuleStates.MechanicSlot(fighter);
        modulesBySlot[slot] = module;
        _configs[slot] = character.Mechanic.Config;
    }

    private static IMechanicModule?[] ResolveModules(ModuleCatalog catalog, in ModuleSlots slots)
    {
        var modulesBySlot = new IMechanicModule?[ModuleStates.MaxModules];
        for (int slot = 0; slot < ModuleStates.LineupSlots; slot++)
        {
            ModuleId id = slots[slot].Module;
            if (id.IsNone)
            {
                continue;
            }

            if (!catalog.TryFind(id, out IMechanicModule? module))
            {
                throw new ArgumentException($"Module slot {slot} names unregistered module {id}.", nameof(slots));
            }

            if (Array.IndexOf(modulesBySlot, module) >= 0)
            {
                throw new ArgumentException($"Module {id} is in the lineup more than once.", nameof(slots));
            }

            if (module!.IsCharacterMechanic)
            {
                throw new ArgumentException(
                    $"Module {id} is a character mechanic; characters name it, the lineup can't.", nameof(slots));
            }

            modulesBySlot[slot] = module;
        }

        return modulesBySlot;
    }
}
