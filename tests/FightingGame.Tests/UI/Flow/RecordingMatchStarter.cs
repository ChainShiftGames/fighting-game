using System;
using System.Collections.Generic;
using FightingGame.Online;

namespace FightingGame.Tests.UI.Flow;

/// <summary>A match starter that records what the flow asked of it, playing the modes as the launcher would.</summary>
internal sealed class RecordingMatchStarter : IMatchStarter
{
    public RecordingMatchStarter(LaunchMode launched = LaunchMode.MainMenu)
    {
        Mode = launched;
    }

    public event Action? OnlineJoined;

    public LaunchMode Mode { get; private set; }

    /// <summary>Whether the platform has online play.</summary>
    public bool HasOnline { get; set; } = true;

    public List<MatchSetup> Started { get; } = [];

    public int Rematches { get; private set; }

    public List<string> Watched { get; } = [];

    public int Stops { get; private set; }

    public void Start(MatchSetup setup)
    {
        Started.Add(setup);
        Mode = setup.Mode;
    }

    public void Watch(string replayPath)
    {
        Watched.Add(replayPath);
        Mode = LaunchMode.Replay;
    }

    public bool OpenOnline()
    {
        if (HasOnline)
        {
            Mode = LaunchMode.Online;
        }

        return HasOnline;
    }

    public void Rematch() => Rematches++;

    public void Stop()
    {
        Stops++;
        Mode = LaunchMode.MainMenu;
    }

    /// <summary>A friend's invite was accepted in Steam.</summary>
    public void AcceptInvite()
    {
        Mode = LaunchMode.Online;
        OnlineJoined?.Invoke();
    }
}
