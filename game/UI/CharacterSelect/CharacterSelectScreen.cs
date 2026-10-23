using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Sim.Data.Validation;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// Character select: both players pick a character and a colour at once, each with their own devices
/// (<see cref="CharacterSelection"/>), then the picks go into the match setup and on to stage select. It starts on
/// each player's last pick this session (<see cref="PickMemory"/>). Drawn by <see cref="CharacterSelectView"/>.
/// </summary>
public sealed class CharacterSelectScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly CharacterSelection _selection;
    private readonly PickMemory _memory;
    private readonly Func<LaunchMode, IMenuPlayers> _playersFor;
    private readonly Func<IReadOnlyList<SelectableCharacter>> _load;
    private IMenuPlayers? _players;
    private MenuText _problem = MenuText.Empty;
    private bool _isOpen;

    /// <param name="playersFor">Whose devices are whose for a mode, read each time the screen opens.</param>
    /// <param name="load">The characters to pick from, loaded again each time the screen opens (data edits show up).</param>
    public CharacterSelectScreen(
        GameFlow flow,
        CharacterSelection selection,
        PickMemory memory,
        Func<LaunchMode, IMenuPlayers> playersFor,
        Func<IReadOnlyList<SelectableCharacter>> load)
    {
        _flow = flow;
        _selection = selection;
        _memory = memory;
        _playersFor = playersFor;
        _load = load;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_CHARACTER_SELECT");

    /// <summary>Why there is nothing to pick (the game data didn't load); empty otherwise.</summary>
    public IReadOnlyList<MenuText> Body => _problem.IsEmpty ? [] : [_problem];

    public IReadOnlyList<MenuItem> Items { get; } = [];

    public bool IsOverMatch => false;

    /// <summary>Each player's back and directions are their own, so every press comes with its device.</summary>
    public bool TakesEveryPress => true;

    public CharacterSelection Selection => _selection;

    /// <summary>Training: player 2 is the dummy, not a player without a controller.</summary>
    public bool IsTraining => _flow.Setup.Mode == LaunchMode.Training;

    /// <summary>Opens and closes with the flow, and moves on once both are ready; call once per displayed frame.</summary>
    public void Follow(bool isShown, long nowMilliseconds)
    {
        if (!isShown)
        {
            _isOpen = false;
            return;
        }

        if (!_isOpen)
        {
            _isOpen = true;
            Open();
        }

        if (_selection.Update(nowMilliseconds))
        {
            Finish();
        }
    }

    public void Press(MenuPress press)
    {
        int side = _players?.PlayerOf(press.Device) ?? -1;
        if (side is < 0 or >= CharacterSelection.SideCount)
        {
            return;
        }

        if (_selection.Press(side, press.Actions))
        {
            _flow.Back();
        }
    }

    // Every press reaches Press with its device (TakesEveryPress); back never comes here.
    public bool Back() => false;

    private void Open()
    {
        IMenuPlayers players = _players = _playersFor(_flow.Setup.Mode);
        IReadOnlyList<SelectableCharacter> characters;
        try
        {
            characters = _load();
            _problem = MenuText.Empty;
        }
        catch (Exception exception) when (exception is DataValidationException or IOException or InvalidDataException)
        {
            // Never patched up: the screen says why and only back works.
            characters = [];
            _problem = MenuText.Plain(exception.Message);
        }

        _selection.Begin(characters, players.CanPress, _memory);
    }

    private void Finish()
    {
        FighterPick player1 = _selection.Pick(0);
        FighterPick player2 = _selection.Pick(1);
        for (int side = 0; side < CharacterSelection.SideCount; side++)
        {
            if (_selection.Chooses(side))
            {
                _memory.Remember(side, _selection.Pick(side));
            }
        }

        _flow.Advance(_flow.Setup with { Player1 = player1, Player2 = player2 });
    }
}
