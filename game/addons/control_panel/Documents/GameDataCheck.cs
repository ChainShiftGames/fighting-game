using System;
using System.Collections.Generic;
using System.Text;
using FightingGame.Debug;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// Loads all of <c>data/</c> with the changed files laid over it, through the same Sim.Data validation the game runs
/// (ARCHITECTURE.md §7), so the panel only saves data the game accepts.
/// </summary>
public sealed class GameDataCheck : IPanelFileCheck
{
    private readonly string _dataRoot;

    public GameDataCheck(string dataRoot)
    {
        _dataRoot = dataRoot;
    }

    public IReadOnlyList<string> Check(IReadOnlyDictionary<PanelFile, string> changed)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach ((PanelFile file, string text) in changed)
        {
            if (file.Area == PanelFileArea.Data)
            {
                files[file.Path] = Encoding.UTF8.GetBytes(text);
            }
        }

        if (files.Count == 0)
        {
            return [];
        }

        try
        {
            new GameDataLoader(new OverlayDataSource(new DirectoryDataSource(_dataRoot), files)).Load();
            return [];
        }
        catch (DataValidationException exception)
        {
            var problems = new List<string>();
            foreach (DataError error in exception.Errors)
            {
                problems.Add($"data/{error}");
            }

            return problems;
        }
    }
}
