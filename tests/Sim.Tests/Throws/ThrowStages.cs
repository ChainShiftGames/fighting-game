using System;
using System.Runtime.InteropServices;
using Sim.Characters;
using Sim.Combat;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;
using Sim.Throws;

namespace Sim.Tests.Throws;

/// <summary>The throw stages in tick order for two <see cref="ThrowCharacter"/>s, with a counting module.</summary>
internal sealed class ThrowStages
{
    public const int ModuleSlot = 0;
    public const int TechWindowFrames = 5;
    public const int ButtonBuffer = 2;

    private readonly ThrowDetectionStage _detection;
    private readonly ThrowHoldStage _holds;
    private readonly ThrowResolutionStage _resolution;

    public ThrowStages(ThrowPriority throwVsStrike = ThrowPriority.Throw)
    {
        CharacterData character = ThrowCharacter.Create();
        MatchRules rules = default;
        rules.ThrowTechWindowFrames = TechWindowFrames;
        rules.ThrowVsStrike = throwVsStrike;
        rules.Modules[ModuleSlot].Module = new ModuleId(1);
        var lineup = new ModuleLineup(new ModuleCatalog([new CountingModule(1)]), in rules.Modules, new CoreRequests());
        var contacts = new ThrowContacts();
        var tech = new ThrowTechInput(character, ButtonBuffer);
        _detection = new ThrowDetectionStage(character, character, contacts);
        _holds = new ThrowHoldStage(character, character, tech, tech, lineup);
        _resolution = new ThrowResolutionStage(character, character, contacts, Strikes, in rules);
    }

    /// <summary>The strike contacts the throw resolution reads; tests record strikes here by hand.</summary>
    public HitContacts Strikes { get; } = new();

    /// <summary>What the counting module saw: hook calls and the last contact.</summary>
    public static CountingState Hooks(ref MatchState state)
    {
        Span<int> slice = state.Modules[ModuleSlot];
        return MemoryMarshal.Read<CountingState>(MemoryMarshal.AsBytes(slice));
    }

    /// <summary>Detection, then the hit resolution step's throw part (holds, then new grabs).</summary>
    public void Run(ref MatchState state) => Run(ref state, FrameActivity.Everyone);

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        _detection.Run(ref state, in activity);
        _holds.Run(ref state, in activity);
        _resolution.Run(ref state, in activity);
    }

    /// <summary>Only the holds, for frames after a grab (the thrower's move no longer grabs).</summary>
    public void RunHolds(ref MatchState state, in FrameActivity activity) => _holds.Run(ref state, in activity);
}
