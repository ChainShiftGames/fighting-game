using System.Collections.Generic;

namespace FightingGame.UI.Online;

/// <summary>The saved replays the replay browser lists and deletes.</summary>
public interface IReplayLibrary
{
    /// <summary>Every saved replay, newest first.</summary>
    /// <exception cref="System.IO.IOException">The folder can't be read.</exception>
    IReadOnlyList<ReplayEntry> List();

    /// <exception cref="System.IO.IOException">The file can't be deleted.</exception>
    void Delete(ReplayEntry entry);
}
