using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;
using Xunit;
using static FightingGame.Tests.UI.CharacterSelect.TestCharacters;

namespace FightingGame.Tests.UI.CharacterSelect;

public sealed class CharacterSelectionTests
{
    private const long ReadyTime = 900;

    private readonly Queue<int> _random = new();
    private readonly PickMemory _memory = new();
    private readonly bool[] _canPress = [true, true];
    private readonly CharacterSelection _selection;

    public CharacterSelectionTests()
    {
        _selection = new CharacterSelection(new CharacterSelectSettings(Columns: 6, ReadyTime), _ => _random.Dequeue());
    }

    [Fact]
    public void BothPlayersPickAtOnceWithTheirOwnCursors()
    {
        Begin(Three());

        Press(0, MenuAction.Confirm, MenuAction.Right, MenuAction.Confirm);
        Press(1, MenuAction.Right, MenuAction.Confirm, MenuAction.Confirm);

        Assert.True(Done());
        Assert.Equal(new FighterPick("ryu", 1), _selection.Pick(0));
        Assert.Equal(new FighterPick("ken", 0), _selection.Pick(1));
    }

    [Fact]
    public void TheBothReadyMomentWaitsAndBackCancelsIt()
    {
        Begin(Three());
        Press(0, MenuAction.Confirm, MenuAction.Confirm);
        Press(1, MenuAction.Confirm, MenuAction.Confirm);

        Assert.False(_selection.Update(0));
        Assert.True(_selection.IsAllReady);
        Assert.False(_selection.Update(ReadyTime - 1));

        _selection.Press(1, MenuAction.Back);

        Assert.False(_selection.Update(ReadyTime));
        Assert.False(_selection.IsAllReady);
        Assert.Equal(SelectStep.Colour, _selection.Side(1).Step);
    }

    [Fact]
    public void InAMirrorTheSecondToReadyIsShiftedOffTheFirstsColour()
    {
        Begin(Three());
        _selection.Press(0, MenuAction.Confirm);
        _selection.Press(1, MenuAction.Confirm);
        Assert.Equal(0, _selection.Side(1).Colour);

        _selection.Press(0, MenuAction.Confirm);

        Assert.Equal(1, _selection.Side(1).Colour);
        Assert.True(_selection.IsTaken(1, character: 0, colour: 0));
    }

    [Fact]
    public void CyclingColoursSkipsTheOneTheOtherPlayerLockedIn()
    {
        Begin(Three());
        Press(0, MenuAction.Confirm, MenuAction.Right, MenuAction.Confirm);
        _selection.Press(1, MenuAction.Confirm);

        Assert.Equal(0, _selection.Side(1).Colour);
        _selection.Press(1, MenuAction.Right);
        Assert.Equal(2, _selection.Side(1).Colour);
        _selection.Press(1, MenuAction.Left);
        Assert.Equal(0, _selection.Side(1).Colour);
    }

    [Fact]
    public void ChoosingTheCharacterTheOtherReadiedStartsOnAFreeColour()
    {
        Begin(Three());
        Press(0, MenuAction.Confirm, MenuAction.Confirm);

        _selection.Press(1, MenuAction.Confirm);

        Assert.Equal(1, _selection.Side(1).Colour);
    }

    [Fact]
    public void ACharacterWithOneColourIsSharedInAMirror()
    {
        Begin([Named("solo", colours: 1)]);
        Press(0, MenuAction.Confirm, MenuAction.Confirm);
        Press(1, MenuAction.Confirm, MenuAction.Confirm);

        Assert.True(Done());
        Assert.Equal(_selection.Pick(0), _selection.Pick(1));
    }

    [Fact]
    public void RandomPicksACharacterAndColourAndReadiesAtOnce()
    {
        Begin(Three());
        _selection.Press(0, MenuAction.Left);
        _random.Enqueue(2);
        _random.Enqueue(1);

        _selection.Press(0, MenuAction.Confirm);

        Assert.Equal(SelectStep.Ready, _selection.Side(0).Step);
        Assert.Equal(new FighterPick("guile", 1), _selection.Pick(0));

        _selection.Press(0, MenuAction.Back);

        Assert.Equal(SelectStep.Character, _selection.Side(0).Step);
        Assert.True(_selection.Grid.IsRandom(_selection.Side(0).Slot));
    }

    [Fact]
    public void RandomAvoidsTheColourTheOtherPlayerLockedIn()
    {
        Begin(Three());
        Press(1, MenuAction.Confirm, MenuAction.Confirm);
        _selection.Press(0, MenuAction.Left);
        _random.Enqueue(0);
        _random.Enqueue(0);

        _selection.Press(0, MenuAction.Confirm);

        Assert.Equal(new FighterPick("ryu", 1), _selection.Pick(0));
    }

    [Fact]
    public void BackUnreadiesThenLeavesTheColourThenLeavesTheScreen()
    {
        Begin(Three());
        Press(0, MenuAction.Confirm, MenuAction.Confirm);

        Assert.False(_selection.Press(0, MenuAction.Back));
        Assert.Equal(SelectStep.Colour, _selection.Side(0).Step);
        Assert.False(_selection.Press(0, MenuAction.Back));
        Assert.Equal(SelectStep.Character, _selection.Side(0).Step);
        Assert.True(_selection.Press(0, MenuAction.Back));
    }

    [Fact]
    public void BackDoesNotLeaveWhileTheOtherPlayerHasChosen()
    {
        Begin(Three());
        _selection.Press(1, MenuAction.Confirm);

        Assert.False(_selection.Press(0, MenuAction.Back));

        _selection.Press(1, MenuAction.Back);
        Assert.True(_selection.Press(0, MenuAction.Back));
    }

    [Fact]
    public void APlayerWithoutAControllerMirrorsInTheNextFreeColour()
    {
        _canPress[1] = false;
        Begin(Three());
        Press(0, MenuAction.Right, MenuAction.Confirm, MenuAction.Right, MenuAction.Confirm);

        Assert.True(Done());
        Assert.Equal(new FighterPick("ken", 1), _selection.Pick(0));
        Assert.Equal(new FighterPick("ken", 2), _selection.Pick(1));
        Assert.False(_selection.Press(0, MenuAction.Back));
    }

    [Fact]
    public void ACursorStartsOnThatPlayersLastPickAndItsColour()
    {
        _memory.Remember(0, new FighterPick("guile", 2));
        _memory.Remember(1, new FighterPick("gone", 1));

        Begin(Three());

        Assert.Equal(2, _selection.Side(0).Slot);
        Assert.Equal(0, _selection.Side(1).Slot);
        Assert.Equal((2, 2), _selection.Shown(0));

        _selection.Press(0, MenuAction.Confirm);
        Assert.Equal(2, _selection.Side(0).Colour);
    }

    [Fact]
    public void BeginStartsOverFromTheGrid()
    {
        Begin(Three());
        Press(0, MenuAction.Confirm, MenuAction.Confirm);

        Begin(Three());

        Assert.Equal(SelectStep.Character, _selection.Side(0).Step);
        Assert.False(_selection.Update(0));
    }

    [Fact]
    public void WithoutCharactersOnlyBackWorks()
    {
        Begin([]);

        Assert.False(_selection.Press(0, MenuAction.Confirm));
        Assert.True(_selection.Press(0, MenuAction.Back));
        Assert.False(_selection.Update(0));
    }

    private void Begin(IReadOnlyList<SelectableCharacter> characters) =>
        _selection.Begin(characters, side => _canPress[side], _memory);

    private void Press(int side, params MenuAction[] presses)
    {
        foreach (MenuAction press in presses)
        {
            _selection.Press(side, press);
        }
    }

    private bool Done() => !_selection.Update(0) && _selection.Update(ReadyTime);
}
