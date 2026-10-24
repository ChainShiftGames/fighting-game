using Godot;

namespace FightingGame.Settings;

/// <summary>Turns vsync on or off (ADR-0004 §3).</summary>
public sealed class VsyncApplier : IPlayerSettingApplier
{
    private bool? _applied;

    public void Apply(PlayerSettings settings)
    {
        if (_applied == settings.Vsync)
        {
            return;
        }

        _applied = settings.Vsync;
        DisplayServer.WindowSetVsyncMode(settings.Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
    }
}
