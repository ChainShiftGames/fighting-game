using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sim.Characters;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>What one module sees in a hook: the match read-only, its own state slice, its config and core requests.</summary>
/// <remarks>
/// The match state is read-only, so a module can't write core state. It must not read another module's slice either
/// (ADR-0011); modules interact only through interfaces or events the core defines, such as the meter of the
/// lineup's <see cref="IMeterGauge"/>s (<see cref="MeterOf"/>, <see cref="TrySpendMeter"/>). Characters are read-only
/// data (<see cref="CharacterOf"/>, <see cref="RoleMove"/>, ADR-0023).
/// </remarks>
public readonly ref struct ModuleContext
{
    private readonly ref readonly MatchState _state;
    private readonly ref ModuleStateSlice _slice;
    private readonly ref readonly ModuleConfig _config;
    private readonly CoreRequests _requests;
    private readonly int _slot;

    // Writable only so the meter gauge can update its own slice when another module spends; null without one.
    private readonly ref MatchState _meterState;
    private readonly MeterAccess? _meters;
    private readonly ModuleCharacters? _characters;

    /// <summary>A context without meter or character access: <see cref="MeterOf"/> is 0 and spending fails.</summary>
    public ModuleContext(
        ref readonly MatchState state,
        ref ModuleStateSlice slice,
        ref readonly ModuleConfig config,
        CoreRequests requests,
        int slot)
    {
        _state = ref state;
        _slice = ref slice;
        _config = ref config;
        _requests = requests;
        _slot = slot;
        _meterState = ref Unsafe.NullRef<MatchState>();
        _meters = null;
        _characters = null;
    }

    internal ModuleContext(
        ref MatchState state,
        ref ModuleStateSlice slice,
        ref readonly ModuleConfig config,
        CoreRequests requests,
        int slot,
        MeterAccess meters,
        ModuleCharacters? characters)
    {
        _state = ref state;
        _slice = ref slice;
        _config = ref config;
        _requests = requests;
        _slot = slot;
        _meterState = ref state;
        _meters = meters;
        _characters = characters;
    }

    public ref readonly MatchState State => ref _state;

    /// <summary>This module's state slice viewed as its own state struct; throws if the struct doesn't fit.</summary>
    public ref TState GetState<TState>()
        where TState : unmanaged, IModuleState
    {
        Span<int> slice = _slice;
        return ref MemoryMarshal.AsRef<TState>(MemoryMarshal.AsBytes(slice));
    }

    /// <summary>This module's settings from <see cref="MatchRules"/> viewed as its own config struct.</summary>
    public ref readonly TConfig GetConfig<TConfig>()
        where TConfig : unmanaged, IModuleConfig => ref ModuleConfigView.As<TConfig>(in _config);

    /// <summary>The fighter's meter on <paramref name="channel"/> from the gauge serving it; 0 without one.</summary>
    public int MeterOf(int fighter, int channel = IMeterGauge.MainChannel) =>
        _meters is null ? 0 : _meters.MeterOf(ref _meterState, fighter, channel);

    /// <summary>Spends the fighter's meter on <paramref name="channel"/> now if it has <paramref name="amount"/>; false without enough or a gauge.</summary>
    public bool TrySpendMeter(int fighter, int amount, int channel = IMeterGauge.MainChannel) =>
        _meters is not null && _meters.TrySpend(ref _meterState, fighter, amount, channel);

    /// <summary>The fighter's character, read-only (ADR-0023).</summary>
    /// <exception cref="InvalidOperationException">The lineup was built without characters (unit tests only).</exception>
    public CharacterData CharacterOf(int fighter) => Characters.CharacterOf(fighter);

    /// <summary>The move the fighter's character names for this module's role at <paramref name="role"/> (<see cref="IMechanicModule.MoveRoles"/>).</summary>
    /// <exception cref="InvalidOperationException">The lineup was built without characters (unit tests only).</exception>
    public int RoleMove(int fighter, int role) => Characters.RoleMove(_slot, fighter, role);

    private ModuleCharacters Characters =>
        _characters ?? throw new InvalidOperationException("This module lineup was built without characters.");

    public void RequestCancel(int fighter, in CancelRequest request) =>
        _requests.Cancels.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestTimeScale(int fighter, in TimeScaleRequest request) =>
        _requests.TimeScales.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestHitstopOverride(int fighter, in HitstopOverrideRequest request) =>
        _requests.HitstopOverrides.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestMoveLock(int fighter, in MoveLockRequest request) =>
        _requests.MoveLocks.Set(CoreRequests.FighterKey(_slot, fighter), request);
    public void RequestGravityScale(int fighter, in GravityScaleRequest request) =>
        _requests.GravityScales.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestStunOverride(int fighter, in StunOverrideRequest request) =>
        _requests.StunOverrides.Set(CoreRequests.FighterKey(_slot, fighter), request);

    /// <summary>Tells presentation something happened, as a module event belonging to <paramref name="fighter"/>.</summary>
    public void RequestEvent(int fighter, in ModuleEventRequest request) =>
        _requests.Events.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestFreeze(in FreezeRequest request) => _requests.Freezes.Set(_slot, request);

    /// <summary>Spawns an entity for <paramref name="owner"/> at the end of the frame (<see cref="CoreRequests.Spawns"/>).</summary>
    public void RequestSpawn(int owner, in SpawnRequest request) =>
        _requests.Spawns.Set(CoreRequests.FighterKey(_slot, owner), request);
}
