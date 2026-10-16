using Sim.Modules;

namespace Sim.Tests.Modules;

/// <summary>State slice of <see cref="CountingModule"/>: one counter per hook plus the last contact seen.</summary>
internal struct CountingState : IModuleState
{
    public int AfterInput;
    public int BeforeStateMachines;
    public int AfterStateMachines;
    public int OnHit;
    public int OnBlock;
    public int OnThrow;
    public int EndOfFrame;
    public int LastAttacker;
    public int LastDefender;
    public int ConfigSeen;
}
