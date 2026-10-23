using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Flow;

/// <summary>
/// The screen for each step of the <see cref="GameFlow"/>. A new screen is one <see cref="Add"/> line; until a step
/// has one, the flow skips it.
/// </summary>
public sealed class FlowScreens
{
    private readonly Dictionary<FlowStep, IMenuScreen> _screens = [];
    private readonly List<IMenuScreen> _path = [];

    public void Add(FlowStep step, IMenuScreen screen) => _screens.Add(step, screen);

    public bool Has(FlowStep step) => _screens.ContainsKey(step);

    /// <summary>The screens for the flow's path, bottom first, for <see cref="MenuStack.Show"/>.</summary>
    /// <remarks>Reuses one list, so it is only valid until the next call.</remarks>
    public IReadOnlyList<IMenuScreen> For(IReadOnlyList<FlowStep> path)
    {
        _path.Clear();
        foreach (FlowStep step in path)
        {
            _path.Add(_screens[step]);
        }

        return _path;
    }
}
