using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>The layouts the edited device can load ("Pad: classic", "Leverless"…); loading one replaces its layout.</summary>
public sealed class PresetScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;

    public PresetScreen(ControlsMenu menu)
    {
        _menu = menu;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_CONTROLS_PRESETS_TITLE");

    public IReadOnlyList<MenuText> Body =>
        [MenuText.Key("MENU_CONTROLS_DEVICE_TITLE", _menu.Device.Name), MenuText.Key("MENU_CONTROLS_PRESETS_INFO")];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            BindingProfile current = _menu.Profile;
            var items = new List<MenuItem>();
            foreach (ControlPreset preset in _menu.Presets)
            {
                items.Add(new ButtonItem(MenuText.Key(preset.NameKey), () => Load(preset))
                {
                    Description = preset.Profile.SameAs(current) ? MenuText.Key("MENU_CONTROLS_PRESET_CURRENT") : MenuText.Empty,
                });
            }

            items.Add(new ButtonItem(MenuText.Key("MENU_CONTROLS_CANCEL"), () => _menu.Close(this)));
            return items;
        }
    }

    public bool IsOverMatch => _menu.IsOverMatch;

    public bool Back()
    {
        _menu.Close(this);
        return false;
    }

    public void Press(MenuPress press)
    {
    }

    private void Load(ControlPreset preset)
    {
        _menu.Change(preset.Profile);
        _menu.Close(this);
    }
}
