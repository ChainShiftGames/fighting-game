namespace FightingGame.Input.Profiles;

/// <summary>
/// Which inputs a binding feeds: the match's input word, or the menus. The same control may be on both (a pad's A is
/// confirm in menus and an attack in matches); only two inputs of one layer sharing a control is a duplicate.
/// </summary>
public enum BindingLayer
{
    Game,
    Menu,
}
