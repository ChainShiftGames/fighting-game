using Godot;

namespace FightingGame.Settings;

/// <summary>Switches the translation locale the menus are shown in.</summary>
public sealed class LanguageApplier : IPlayerSettingApplier
{
    private string? _applied;

    public void Apply(PlayerSettings settings)
    {
        if (_applied == settings.Language)
        {
            return;
        }

        _applied = settings.Language;
        TranslationServer.SetLocale(settings.Language);
    }
}
