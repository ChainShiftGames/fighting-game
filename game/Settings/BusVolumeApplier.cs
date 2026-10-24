using System;
using Godot;

namespace FightingGame.Settings;

/// <summary>Sets one audio bus's volume from its percent setting; 0 mutes it.</summary>
public sealed class BusVolumeApplier : IPlayerSettingApplier
{
    private readonly string _bus;
    private readonly Func<PlayerSettings, int> _volume;
    private int? _applied;

    /// <param name="bus">One of <see cref="AudioBus"/>.</param>
    public BusVolumeApplier(string bus, Func<PlayerSettings, int> volume)
    {
        _bus = bus;
        _volume = volume;
    }

    public void Apply(PlayerSettings settings)
    {
        int volume = _volume(settings);
        if (_applied == volume)
        {
            return;
        }

        _applied = volume;
        int index = AudioServer.GetBusIndex(_bus);
        if (index < 0)
        {
            GD.PushWarning($"No audio bus \"{_bus}\" in the bus layout; its volume setting does nothing.");
            return;
        }

        AudioServer.SetBusMute(index, volume == 0);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(volume / (float)PlayerSettingLimits.MaxVolume));
    }
}
