using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>One value a <see cref="ChoiceSetting{T}"/> offers, with what the picker shows for it.</summary>
public sealed record SettingChoice<T>(T Value, MenuText Label);
