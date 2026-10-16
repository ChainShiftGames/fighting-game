using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Modules;

/// <summary>Implements every hook and only counts calls in its own slice, so it allocates nothing.</summary>
internal sealed class CountingModule(int id) : IMechanicModule, IAfterInputHook, IBeforeStateMachinesHook,
    IAfterStateMachinesHook, IOnHitHook, IOnBlockHook, IOnThrowHook, IEndOfFrameHook
{
    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"counting_{id}";

    public void AfterInput(ModuleContext context)
    {
        ref CountingState state = ref context.GetState<CountingState>();
        state.AfterInput++;
        state.ConfigSeen = context.GetConfig<TestConfig>().Value;
    }

    public void BeforeStateMachines(ModuleContext context) => context.GetState<CountingState>().BeforeStateMachines++;

    public void AfterStateMachines(ModuleContext context) => context.GetState<CountingState>().AfterStateMachines++;

    public void OnHit(ModuleContext context, in FighterContact contact)
    {
        ref CountingState state = ref RecordContact(context, contact);
        state.OnHit++;
    }

    public void OnBlock(ModuleContext context, in FighterContact contact)
    {
        ref CountingState state = ref RecordContact(context, contact);
        state.OnBlock++;
    }

    public void OnThrow(ModuleContext context, in FighterContact contact)
    {
        ref CountingState state = ref RecordContact(context, contact);
        state.OnThrow++;
    }

    public void EndOfFrame(ModuleContext context) => context.GetState<CountingState>().EndOfFrame++;

    private static ref CountingState RecordContact(ModuleContext context, in FighterContact contact)
    {
        ref CountingState state = ref context.GetState<CountingState>();
        state.LastAttacker = contact.Attacker;
        state.LastDefender = contact.Defender;
        return ref state;
    }
}
