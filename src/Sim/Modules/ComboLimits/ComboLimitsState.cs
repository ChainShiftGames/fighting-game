using System.Runtime.CompilerServices;
using Sim.State;

namespace Sim.Modules.ComboLimits;

/// <summary>The combo limits module's slice of the match state: the combo each fighter is taking, P1's first.</summary>
[InlineArray(MatchState.PlayerCount)]
public struct ComboLimitsState : IModuleState
{
    private DefenderCombo _element;
}
