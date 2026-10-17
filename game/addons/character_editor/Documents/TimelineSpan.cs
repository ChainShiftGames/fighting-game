namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>Frames <paramref name="FirstFrame"/> to <paramref name="LastFrame"/> (1-based, inclusive) of one kind.</summary>
public readonly record struct TimelineSpan(int FirstFrame, int LastFrame, TimelineSpanKind Kind);
