using System.Collections.Generic;

namespace FightingGame.Settings;

/// <summary>Every <see cref="IPlayerSettingApplier"/>: the one place a setting that acts on the engine is registered.</summary>
/// <remarks>The input delay has none: online match startups read it from <see cref="CurrentPlayerSettings"/>.</remarks>
public static class PlayerSettingAppliers
{
    public static IReadOnlyList<IPlayerSettingApplier> Create() =>
    [
        new ScreenApplier(),
        new VsyncApplier(),
        new BusVolumeApplier(AudioBus.Master, settings => settings.MasterVolume),
        new BusVolumeApplier(AudioBus.Music, settings => settings.MusicVolume),
        new BusVolumeApplier(AudioBus.Effects, settings => settings.EffectsVolume),
        new BusVolumeApplier(AudioBus.Voice, settings => settings.VoiceVolume),
        new LanguageApplier(),
    ];
}
