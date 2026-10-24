using System;
using System.Collections.Generic;

namespace FightingGame.Settings;

/// <summary>
/// The player settings as they are now: changes apply at once through each <see cref="IPlayerSettingApplier"/>, and
/// are written to the file only when <see cref="Save"/> is called (on leaving the options menu).
/// </summary>
public sealed class CurrentPlayerSettings
{
    private readonly Action<PlayerSettings> _save;
    private readonly IReadOnlyList<IPlayerSettingApplier> _appliers;
    private PlayerSettings _saved;

    /// <param name="loaded">The settings saved last time; applied now.</param>
    /// <param name="save">Writes a changed copy for next time.</param>
    public CurrentPlayerSettings(
        PlayerSettings loaded, Action<PlayerSettings> save, IReadOnlyList<IPlayerSettingApplier> appliers)
    {
        _save = save;
        _appliers = appliers;
        _saved = loaded;
        Value = loaded;
        Apply();
    }

    public PlayerSettings Value { get; private set; }

    /// <summary>Applies at once; the input delay from the next online match.</summary>
    public void Change(PlayerSettings settings)
    {
        Value = settings;
        Apply();
    }

    /// <summary>Writes the settings when they differ from the file.</summary>
    public void Save()
    {
        if (Value != _saved)
        {
            _save(Value);
            _saved = Value;
        }
    }

    private void Apply()
    {
        foreach (IPlayerSettingApplier applier in _appliers)
        {
            applier.Apply(Value);
        }
    }
}
