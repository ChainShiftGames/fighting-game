namespace FightingGame.Settings;

/// <summary>Puts one part of the <see cref="PlayerSettings"/> into effect (the window, an audio bus, the language…).</summary>
/// <remarks>Called with every change, so it skips the work when its part is as it last applied it.</remarks>
public interface IPlayerSettingApplier
{
    void Apply(PlayerSettings settings);
}
