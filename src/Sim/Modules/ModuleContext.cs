using System;
using System.Runtime.InteropServices;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules;

/// <summary>What one module sees in a hook: the match read-only, its own state slice, its config and core requests.</summary>
/// <remarks>
/// The match state is read-only, so a module can't write core state. It must not read another module's slice either
/// (ADR-0011); modules interact only through interfaces or events the core defines.
/// </remarks>
public readonly ref struct ModuleContext
{
    private readonly ref readonly MatchState _state;
    private readonly ref ModuleStateSlice _slice;
    private readonly ref readonly ModuleConfig _config;
    private readonly CoreRequests _requests;
    private readonly int _slot;

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
        where TConfig : unmanaged, IModuleConfig
    {
        ReadOnlySpan<int> config = _config;
        return ref MemoryMarshal.AsRef<TConfig>(MemoryMarshal.AsBytes(config));
    }

    public void RequestCancel(int fighter, in CancelRequest request) =>
        _requests.Cancels.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestTimeScale(int fighter, in TimeScaleRequest request) =>
        _requests.TimeScales.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestHitstopOverride(int fighter, in HitstopOverrideRequest request) =>
        _requests.HitstopOverrides.Set(CoreRequests.FighterKey(_slot, fighter), request);

    public void RequestFreeze(in FreezeRequest request) => _requests.Freezes.Set(_slot, request);
}
