using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>One labelled row of the move timeline: frame ranges, 1-based and inclusive.</summary>
public sealed record TimelineRow(string Label, IReadOnlyList<TimelineSpan> Spans);
