using System;

namespace FightingGame.UI;

/// <summary>One button of a <see cref="MenuPanel"/>: its text, what choosing it does, and whether it can be chosen now.</summary>
public sealed record MenuEntry(string Label, Action Choose, bool Enabled = true);
