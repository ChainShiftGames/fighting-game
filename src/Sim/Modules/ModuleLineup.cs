using System;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>The match's enabled modules in hook order; the tick pipeline calls its hooks at fixed points.</summary>
/// <remarks>
/// Built once at match start from the <see cref="MatchRules"/> lineup, which is fixed for the match. Within a hook
/// point, modules run in slot order (the module order from data). Running hooks allocates nothing.
/// </remarks>
public sealed class ModuleLineup
{
    private readonly ModuleSlots _slots;
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

    /// <exception cref="ArgumentException">The lineup names an unregistered module or one module twice.</exception>
    public ModuleLineup(ModuleCatalog catalog, in ModuleSlots slots, CoreRequests requests)
    {
        _slots = slots;
        _requests = requests;
        IMechanicModule?[] modulesBySlot = ResolveModules(catalog, in slots);
        _afterInput = new(modulesBySlot);
        _beforeStateMachines = new(modulesBySlot);
        _afterStateMachines = new(modulesBySlot);
        _onHit = new(modulesBySlot);
        _onBlock = new(modulesBySlot);
        _onThrow = new(modulesBySlot);
        _endOfFrame = new(modulesBySlot);
        _airGuard = new(modulesBySlot);
        _meters = CreateMeterAccess(modulesBySlot, in slots, requests);
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

    private ModuleContext ContextFor(ref MatchState state, int slot) =>
        new(ref state, ref state.Modules[slot], in _slots[slot].Config, _requests, slot, _meters);

    // The first meter gauge in hook order keeps the meter; a second one would never be asked.
    private static MeterAccess CreateMeterAccess(
        IMechanicModule?[] modulesBySlot, in ModuleSlots slots, CoreRequests requests)
    {
        var gauges = new HookList<IMeterGauge>(modulesBySlot);
        if (gauges.Count == 0)
        {
            return new MeterAccess(null, 0, default, requests);
        }

        int slot = gauges.SlotAt(0);
        return new MeterAccess(gauges.HookAt(0), slot, in slots[slot].Config, requests);
    }

    private static IMechanicModule?[] ResolveModules(ModuleCatalog catalog, in ModuleSlots slots)
    {
        var modulesBySlot = new IMechanicModule?[ModuleStates.MaxModules];
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
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

            modulesBySlot[slot] = module;
        }

        return modulesBySlot;
    }
}
