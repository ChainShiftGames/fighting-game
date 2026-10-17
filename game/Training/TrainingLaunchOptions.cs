using System;

namespace FightingGame.Training;

/// <summary>Starts training mode instead of a local match from the command line, until there is a main menu: <c>--training</c>.</summary>
public static class TrainingLaunchOptions
{
    public const string Usage = "--training";

    public static bool WantsTraining(string[] args) => Array.IndexOf(args, Usage) >= 0;
}
