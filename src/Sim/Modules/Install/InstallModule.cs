using System.Collections.Generic;
using Sim.Characters;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.Install;

/// <summary>
/// An install (ADR-0023): a character mechanic that sets its owner's <c>install</c> flag when the character's
/// <c>install</c> move starts, and clears it again after <see cref="InstallConfig.DurationFrames"/>. Install-only moves
/// require the flag in their data; what the install costs is its move's own <c>cost</c>.
/// </summary>
/// <remarks>
/// Marking <c>install</c> <c>clearedWhenHit</c> in data/rules/flags.json makes a hit end it early; the timer then stops
/// too. Starting the install move again while it runs restarts the timer.
/// </remarks>
public sealed class InstallModule : IMechanicModule, IAfterInputHook, IAfterStateMachinesHook
{
    // Indexes in MoveRoles and Flags.
    private const int InstallRole = 0;
    private const int InstallFlag = 0;

    // The next free id after the character gauge (23, ADR-0023).
    public ModuleId Id => new(24);

    public string Name => "install";

    public bool IsCharacterMechanic => true;

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<InstallConfig>(
        new ModuleConfigField("durationFrames", 1, 99_999, "How long an install lasts, in frames."));

    /// <summary>The move that starts the install.</summary>
    public IReadOnlyList<string> MoveRoles { get; } = ["install"];

    /// <summary>Set while the install runs.</summary>
    public IReadOnlyList<string> Flags { get; } = ["install"];

    public void AfterInput(ModuleContext context)
    {
        ref InstallState install = ref context.GetState<InstallState>();
        if (install.FramesLeft == 0)
        {
            return;
        }

        int owner = context.Owner;
        int flag = context.FlagMask(InstallFlag);
        if ((context.State.Fighters[owner].Flags & flag) == 0)
        {
            // A hit took the flag away (clearedWhenHit), which ends the install.
            install.FramesLeft = 0;
            return;
        }

        if (--install.FramesLeft == 0)
        {
            context.RequestFlags(owner, new FlagsRequest { Clear = flag });
        }
    }

    public void AfterStateMachines(ModuleContext context)
    {
        int owner = context.Owner;
        ref readonly FighterState fighter = ref context.State.Fighters[owner];
        if (fighter.StartedMoveCategory != MoveCategory.None &&
            fighter.Move == context.RoleMove(owner, InstallRole))
        {
            context.GetState<InstallState>().FramesLeft = context.GetConfig<InstallConfig>().DurationFrames;
            context.RequestFlags(owner, new FlagsRequest { Set = context.FlagMask(InstallFlag) });
        }
    }
}
