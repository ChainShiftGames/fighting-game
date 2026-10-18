using System;

namespace FightingGame.VfxLab.Panels;

/// <summary>What a variant cell asks the Lab to do: each takes the variant's id, except <paramref name="FeedbackChanged"/>.</summary>
/// <param name="Solo">Show it alone, or go back to the grid when it already is.</param>
public sealed record LabCellActions(Action<string> Select, Action<string> Fork, Action<string> Solo, Action FeedbackChanged);
