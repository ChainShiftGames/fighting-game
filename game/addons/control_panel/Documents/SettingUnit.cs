namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>What a stored number means, so the panel can show it in human terms next to the raw value.</summary>
public enum SettingUnit
{
    None,

    /// <summary>Logic frames at 60 Hz; shown in seconds.</summary>
    Frames,

    /// <summary>Subpixels (1 px = 1000); shown in pixels.</summary>
    Subpixels,

    /// <summary>Subpixels per frame; shown in pixels per frame.</summary>
    SubpixelsPerFrame,

    /// <summary>Thousandths; shown as a percentage.</summary>
    Permille,
}
