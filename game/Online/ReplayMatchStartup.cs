using System.IO;
using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.Feel;
using Godot;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Data;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Plays a replay file back: the match it recorded, rebuilt from its header, run on its inputs (ADR-0018).</summary>
public sealed class ReplayMatchStartup
{
    private readonly DeviceRoster _roster;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;

    /// <param name="stepping">Pause and frame step (developer setting).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="effectToggles">Switches each kind of event effect (developer settings).</param>
    public ReplayMatchStartup(
        DeviceRoster roster,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles)
    {
        _roster = roster;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
    }

    /// <param name="path">The replay file: a full path or a <c>user://</c> path.</param>
    /// <exception cref="ReplayRejectedException">This build can't play it; the message says why.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    public StartedMatch Create(GameData data, string path)
    {
        Replay replay = ReplayFolder.Load(ProjectSettings.GlobalizePath(path), data);
        MatchLineup lineup = MatchLineup.FromReplay(data, replay.Header);
        // Nobody holds a controller while watching, so nothing rumbles.
        var effects = new MatchEffects(_effectToggles, JoypadRumbleMotor.None(_roster), EffectSettingsChoice.Load());
        var driver = new ReplayMatchDriver<MatchState>(
            effects.Observe(new ComposedMatchSimulation(lineup.Simulation)), replay.Inputs);
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Simulation.Rules, lineup.Screen, _hitboxOverlay, effects);

        MatchLoop<MatchState> loop = _loops.Create(driver, players: [], _stepping);
        string description = $"replay {Path.GetFileName(path)}: {lineup.Description}, {replay.Inputs.Count} frames";
        return new StartedMatch(
            loop, driver, view, () => description, connection: null,
            isOver: () => driver.State.Round.Phase == RoundPhase.MatchOver);
    }
}
