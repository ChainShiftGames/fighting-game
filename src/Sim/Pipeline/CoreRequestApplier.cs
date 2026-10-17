using System;
using Sim.Characters;
using Sim.Modules;
using Sim.State;

namespace Sim.Pipeline;

/// <summary>Validates the modules' core requests and applies them to the state in a fixed order (ADR-0011).</summary>
/// <remarks>
/// Order: cancels, time scales, hitstop overrides, move locks, freezes; within a kind, fighter by fighter in module slot order.
/// Conflicts: the highest-priority cancel wins (ties: earlier slot); for time scale and hitstop the latest slot
/// wins; move locks combine; the longest freeze wins (ties: earlier slot). An invalid request is a module bug and throws, naming the
/// slot, rather than being silently fixed. The request tables are cleared after applying.
/// </remarks>
public sealed class CoreRequestApplier
{
    private readonly CoreRequests _requests;

    public CoreRequestApplier(CoreRequests requests)
    {
        _requests = requests;
    }

    public void Apply(ref MatchState state)
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref FighterState fighterState = ref state.Fighters[fighter];
            ApplyCancels(ref fighterState.Cancel, fighter);
            ApplyTimeScales(ref fighterState.Timing, fighter);
            ApplyHitstopOverrides(ref fighterState.Timing, fighter);
            ApplyMoveLocks(ref fighterState.LockedMoveCategories, fighter);
        }

        ApplyFreezes(ref state.Freeze);
        _requests.Clear();
    }

    private void ApplyCancels(ref PendingCancel pending, int fighter)
    {
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            if (!_requests.Cancels.TryGet(CoreRequests.FighterKey(slot, fighter), out CancelRequest request))
            {
                continue;
            }

            if (request.TargetMove < 0)
            {
                throw Invalid(slot, $"a cancel into move {request.TargetMove}");
            }

            if (pending.IsPending == 0 || request.Priority > pending.Priority)
            {
                pending = new PendingCancel
                {
                    IsPending = 1,
                    TargetMove = request.TargetMove,
                    Priority = request.Priority,
                };
            }
        }
    }

    private void ApplyTimeScales(ref FighterTiming timing, int fighter)
    {
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            if (!_requests.TimeScales.TryGet(CoreRequests.FighterKey(slot, fighter), out TimeScaleRequest request))
            {
                continue;
            }

            // Only slowdown: a fighter can't get more than one frame of its own per match frame.
            bool isValid = request.Denominator > 0 && request.Numerator > 0 &&
                request.Numerator <= request.Denominator && request.Frames > 0;
            if (!isValid)
            {
                throw Invalid(
                    slot, $"time scale {request.Numerator}/{request.Denominator} for {request.Frames} frames");
            }

            timing.TimeScaleNumerator = request.Numerator;
            timing.TimeScaleDenominator = request.Denominator;
            timing.TimeScaleFramesLeft = request.Frames;
            timing.TimeScaleProgress = 0;
        }
    }

    private void ApplyHitstopOverrides(ref FighterTiming timing, int fighter)
    {
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            int key = CoreRequests.FighterKey(slot, fighter);
            if (!_requests.HitstopOverrides.TryGet(key, out HitstopOverrideRequest request))
            {
                continue;
            }

            if (request.Frames < 0)
            {
                throw Invalid(slot, $"a hitstop of {request.Frames} frames");
            }

            timing.HitstopFramesLeft = request.Frames;
        }
    }

    private void ApplyMoveLocks(ref MoveCategory locked, int fighter)
    {
        const MoveCategory Lockable = MoveCategory.Normal | MoveCategory.Special | MoveCategory.Super |
            MoveCategory.Throw;
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            if (!_requests.MoveLocks.TryGet(CoreRequests.FighterKey(slot, fighter), out MoveLockRequest request))
            {
                continue;
            }

            if ((request.Categories & ~Lockable) != 0)
            {
                throw Invalid(slot, $"a move lock on categories {request.Categories}");
            }

            locked |= request.Categories;
        }
    }

    private void ApplyFreezes(ref FreezeState freeze)
    {
        for (int slot = 0; slot < ModuleStates.MaxModules; slot++)
        {
            if (!_requests.Freezes.TryGet(slot, out FreezeRequest request))
            {
                continue;
            }

            int allFighters = (1 << MatchState.PlayerCount) - 1;
            if (request.Frames <= 0 || (request.ExemptFighters & ~allFighters) != 0)
            {
                throw Invalid(slot, $"a freeze of {request.Frames} frames exempting fighters {request.ExemptFighters}");
            }

            if (request.Frames > freeze.FramesLeft)
            {
                freeze = new FreezeState { FramesLeft = request.Frames, ExemptFighters = request.ExemptFighters };
            }
        }
    }

    private static InvalidOperationException Invalid(int slot, string what) =>
        new($"The module in slot {slot} requested {what}, which the core can't apply.");
}
