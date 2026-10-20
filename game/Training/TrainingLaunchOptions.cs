using System;

namespace FightingGame.Training;

/// <summary>Starts training mode at once instead of showing the main menu, from the command line: <c>--training</c>.</summary>
public static class TrainingLaunchOptions
{
    public const string Usage = "--training";

    public static bool WantsTraining(string[] args) => Array.IndexOf(args, Usage) >= 0;
}
