using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Flow;

/// <summary>
/// The player's path through the game, from boot to a match and back: which screen is next. Screens tell it what the
/// player chose; it fills in the <see cref="Setup"/>, starts and stops matches through <see cref="IMatchStarter"/>,
/// and follows matches that start or end without a menu (the command line, the lobby, a friend's invite). Steps
/// without a screen yet are skipped, so the path works at every stage of the menus.
/// </summary>
/// <remarks>It only decides where the player is; each screen does its own work.</remarks>
public sealed class GameFlow
{
    // The screens between the main menu and the match, in order, for each mode.
    private static readonly FlowStep[] _localRoute =
        [FlowStep.SideSelect, FlowStep.CharacterSelect, FlowStep.StageSelect, FlowStep.Match];

    private static readonly FlowStep[] _onlineRoute = [FlowStep.Online, FlowStep.Match];
    private static readonly FlowStep[] _launchedRoute = [FlowStep.Match];

    private readonly IMatchStarter _matches;
    private readonly Func<FlowStep, bool> _hasScreen;
    private readonly List<FlowStep> _path = [];

    /// <param name="hasScreen">Whether a step has its screen yet; the title, main menu and results always do.</param>
    public GameFlow(IMatchStarter matches, Func<FlowStep, bool> hasScreen)
    {
        _matches = matches;
        _hasScreen = hasScreen;
        if (matches.Mode != LaunchMode.MainMenu)
        {
            // Launched from the command line: the match starts without the menus, which open after it.
            Setup = MatchSetup.For(matches.Mode);
        }

        matches.OnlineJoined += JoinOnline;
        GoTo(FlowStep.Title);
    }

    public FlowStep Step { get; private set; }

    /// <summary>What was picked for the match; also the last mode played, where the main menu's cursor starts.</summary>
    public MatchSetup Setup { get; private set; } = MatchSetup.For(LaunchMode.Versus);

    /// <summary>The device that pressed a button on the title; null before that.</summary>
    public MenuDevice? LeadDevice { get; private set; }

    /// <summary>The screens to show now, bottom first: the menus behind the current one, so back finds them as left.</summary>
    /// <remarks>Empty during a match (its pause menu is the match's own).</remarks>
    public IReadOnlyList<FlowStep> Path => _path;

    /// <summary>Whether the results can go back to character select (once it exists) for this mode.</summary>
    public bool CanReselect => IsOnRoute(FlowStep.CharacterSelect) && Has(FlowStep.CharacterSelect);

    private FlowStep Home => Setup.Mode == LaunchMode.Online ? FlowStep.Online : FlowStep.MainMenu;

    private FlowStep[] Route => Setup.Mode switch
    {
        LaunchMode.Versus or LaunchMode.Training => _localRoute,
        LaunchMode.Online => _onlineRoute,
        _ => _launchedRoute,
    };

    public bool Has(FlowStep step) => step is FlowStep.Match || _hasScreen(step);

    /// <summary>Any button on the title: on to the main menu.</summary>
    public void PressOnTitle(MenuDevice device)
    {
        if (Step != FlowStep.Title)
        {
            return;
        }

        LeadDevice = device;
        GoTo(FlowStep.MainMenu);
    }

    /// <summary>A mode picked on the main menu: versus, training or online.</summary>
    public void Choose(LaunchMode mode)
    {
        if (Step != FlowStep.MainMenu)
        {
            return;
        }

        if (mode is not (LaunchMode.Versus or LaunchMode.Training or LaunchMode.Online))
        {
            throw new ArgumentException($"The main menu doesn't offer {mode}.", nameof(mode));
        }

        if (mode == LaunchMode.Online && (!Has(FlowStep.Online) || !_matches.OpenOnline()))
        {
            return;
        }

        Setup = MatchSetup.For(mode);
        Enter(NextOnRoute(-1));
    }

    /// <summary>A screen over the main menu (options, replays, the quit prompt); nothing when it has no screen yet.</summary>
    public void Open(FlowStep step)
    {
        if (step is not (FlowStep.Options or FlowStep.Replays or FlowStep.QuitPrompt))
        {
            throw new ArgumentException($"{step} doesn't open from the main menu.", nameof(step));
        }

        if (Step == FlowStep.MainMenu && Has(step))
        {
            GoTo(step);
        }
    }

    /// <summary>A screen before the match is done (sides, characters, stage picked): on to the next one.</summary>
    /// <param name="setup">The setup with what that screen picked.</param>
    public void Advance(MatchSetup setup)
    {
        int index = Array.IndexOf(Route, Step);
        if (index < 0 || Step == FlowStep.Match || setup.Mode != Setup.Mode)
        {
            return;
        }

        Setup = setup;
        Enter(NextOnRoute(index));
    }

    /// <summary>Back: one screen towards the main menu, and from there to the title. Matches and results ignore it.</summary>
    public void Back()
    {
        switch (Step)
        {
            case FlowStep.MainMenu:
                GoTo(FlowStep.Title);
                break;
            case FlowStep.QuitPrompt or FlowStep.Options or FlowStep.Replays:
                GoTo(FlowStep.MainMenu);
                break;
            case FlowStep.Title or FlowStep.Match or FlowStep.Results:
                break;
            default:
                if (Step == FlowStep.Online)
                {
                    _matches.Stop();
                }

                GoTo(PreviousOnRoute(Array.IndexOf(Route, Step)));
                break;
        }
    }

    /// <summary>After a finished match on this PC: the same setup again.</summary>
    public void Rematch()
    {
        if (Step != FlowStep.Results)
        {
            return;
        }

        _matches.Rematch();
        GoTo(FlowStep.Match);
    }

    /// <summary>After a finished match: back to character select with the same mode.</summary>
    public void Reselect()
    {
        if (Step != FlowStep.Results || !CanReselect)
        {
            return;
        }

        _matches.Stop();
        GoTo(FlowStep.CharacterSelect);
    }

    /// <summary>Leaves a match being played (the pause menu): back where its mode was picked.</summary>
    /// <remarks>Online that is the online screens, out of the lobby; on this PC, the main menu.</remarks>
    public void LeaveMatch()
    {
        if (Step != FlowStep.Match)
        {
            return;
        }

        if (Home == FlowStep.Online)
        {
            _matches.OpenOnline();
        }
        else
        {
            _matches.Stop();
        }

        GoTo(Home);
    }

    /// <summary>Straight to the main menu, ending any match and leaving the lobby.</summary>
    public void ToMainMenu()
    {
        _matches.Stop();
        GoTo(FlowStep.MainMenu);
    }

    /// <summary>Follows the match on screen; call once per displayed frame.</summary>
    /// <remarks>
    /// Matches also start and end without a menu: the lobby starts one when both players are ready, a rematch or saved
    /// data starts one over, the opponent leaves, a match fails to start.
    /// </remarks>
    public void Follow(MatchPhase phase)
    {
        switch (phase)
        {
            case MatchPhase.Playing when Step != FlowStep.Match:
                GoTo(FlowStep.Match);
                break;
            case MatchPhase.Over when Step == FlowStep.Match:
                // Online, the lobby offers the rematch over the finished match.
                GoTo(Home == FlowStep.Online ? FlowStep.Online : FlowStep.Results);
                break;
            case MatchPhase.None when Step is FlowStep.Match or FlowStep.Results:
                GoTo(Home);
                break;
        }
    }

    // A friend's invite accepted in Steam: the launcher has already left whatever was running.
    private void JoinOnline()
    {
        Setup = MatchSetup.For(LaunchMode.Online);
        GoTo(FlowStep.Online);
    }

    private void Enter(FlowStep step)
    {
        if (step == FlowStep.Match && Setup.Mode is LaunchMode.Versus or LaunchMode.Training)
        {
            _matches.Start(Setup);
        }

        GoTo(step);
    }

    private bool IsOnRoute(FlowStep step) => Array.IndexOf(Route, step) >= 0;

    private FlowStep NextOnRoute(int index)
    {
        FlowStep[] route = Route;
        int next = index + 1;
        while (!Has(route[next]))
        {
            next++;
        }

        return route[next];
    }

    private FlowStep PreviousOnRoute(int index)
    {
        FlowStep[] route = Route;
        for (int i = index - 1; i >= 0; i--)
        {
            if (Has(route[i]))
            {
                return route[i];
            }
        }

        return FlowStep.MainMenu;
    }

    private void GoTo(FlowStep step)
    {
        Step = step;
        _path.Clear();
        switch (step)
        {
            case FlowStep.Title or FlowStep.MainMenu or FlowStep.Results:
                _path.Add(step);
                break;
            case FlowStep.QuitPrompt or FlowStep.Options or FlowStep.Replays:
                _path.Add(FlowStep.MainMenu);
                _path.Add(step);
                break;
            case FlowStep.Match:
                break;
            default:
                _path.Add(FlowStep.MainMenu);
                AddRouteUpTo(step);
                break;
        }
    }

    private void AddRouteUpTo(FlowStep step)
    {
        foreach (FlowStep onRoute in Route)
        {
            if (Has(onRoute))
            {
                _path.Add(onRoute);
            }

            if (onRoute == step)
            {
                return;
            }
        }
    }
}
