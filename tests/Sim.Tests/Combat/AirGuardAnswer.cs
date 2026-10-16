using Sim.Modules;
using Sim.Rules;

namespace Sim.Tests.Combat;

/// <summary>A test module that answers every air guard question the same way and counts the questions.</summary>
internal sealed class AirGuardAnswer(int id, bool answer) : IMechanicModule, IAirGuardHook
{
    public ModuleId Id { get; } = new(id);

    public string Name { get; } = $"air_guard_answer_{id}";

    public int Questions { get; private set; }

    public bool CanAirGuard(ModuleContext context, in AirGuardCheck check)
    {
        Questions++;
        return answer;
    }
}
