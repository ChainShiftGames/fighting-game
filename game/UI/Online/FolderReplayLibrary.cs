using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.Online;
using Netcode.Replays;
using Sim.Characters;
using Sim.Data;

namespace FightingGame.UI.Online;

/// <summary>The replays in this PC's <see cref="ReplayFolder"/>, each read against this build's game data.</summary>
public sealed class FolderReplayLibrary : IReplayLibrary
{
    private readonly ReplayFolder _folder;
    private readonly Func<GameData> _loadData;

    /// <param name="loadData">This build's game data: replays recorded with other data are unsupported.</param>
    public FolderReplayLibrary(ReplayFolder folder, Func<GameData> loadData)
    {
        _folder = folder;
        _loadData = loadData;
    }

    public IReadOnlyList<ReplayEntry> List()
    {
        GameData data = _loadData();
        var entries = new List<ReplayEntry>();
        foreach (string file in _folder.Files())
        {
            entries.Add(Read(file, data));
        }

        return entries;
    }

    public void Delete(ReplayEntry entry) => _folder.Delete(entry.Path);

    // The whole file is read: only a replay this build can play back to its end is listed as playable.
    private static ReplayEntry Read(string file, GameData data)
    {
        DateTime? savedAt = ReplayFolder.SavedAt(file);
        try
        {
            ReplayHeader header = ReplayFolder.Load(file, data).Header;
            return new ReplayEntry(
                file,
                savedAt,
                NameOf(data, header.Player1.CharacterId),
                header.Player1.Colour,
                NameOf(data, header.Player2.CharacterId),
                header.Player2.Colour,
                Rejection: null);
        }
        catch (ReplayRejectedException rejected)
        {
            return Unplayable(file, savedAt, rejected.Reason);
        }
        catch (IOException)
        {
            return Unplayable(file, savedAt, ReplayRejection.Damaged);
        }
    }

    private static ReplayEntry Unplayable(string file, DateTime? savedAt, ReplayRejection reason) =>
        new(file, savedAt, string.Empty, 0, string.Empty, 0, reason);

    private static string NameOf(GameData data, string id)
    {
        foreach (CharacterData character in data.Characters)
        {
            if (character.Id == id)
            {
                return character.DisplayName;
            }
        }

        return id;
    }
}
