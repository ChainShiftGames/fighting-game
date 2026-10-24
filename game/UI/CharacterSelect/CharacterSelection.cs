using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// The rules of character select for both players at once: each moves their cursor, chooses a character, cycles its
/// colours and readies; back undoes one step. In a mirror match the player who readies second can't take the
/// first's colour: theirs shifts to the next free one. A player without a controller (the training dummy, or player 2
/// with no pad) plays the other player's character in the next free colour. Online, the other PC's player is a remote
/// side: it shows their pick once it arrives and counts as locked in (ADR-0042).
/// </summary>
/// <remarks>Plain C#: the screen hands it presses per player and draws it.</remarks>
public sealed class CharacterSelection
{
    /// <summary>Character select always picks for two players, whether one or both choose.</summary>
    public const int SideCount = 2;

    /// <summary>For <see cref="Begin"/>: both sides are on this PC.</summary>
    public const int NoRemoteSide = -1;

    private readonly SideSelection[] _sides = [new(), new()];
    private readonly CharacterSelectSettings _settings;
    private readonly Func<int, int> _randomBelow;
    private Func<int, bool> _canPress = _ => false;
    private long? _readySince;
    private int _remoteSide = NoRemoteSide;
    private FighterPick? _remotePick;

    /// <param name="randomBelow">A random number from 0 up to (not including) its argument: presentation randomness,
    /// never the logic's.</param>
    public CharacterSelection(CharacterSelectSettings settings, Func<int, int> randomBelow)
    {
        _settings = settings;
        _randomBelow = randomBelow;
        Grid = new CharacterGrid(0, settings.Columns);
    }

    public IReadOnlyList<SelectableCharacter> Characters { get; private set; } = [];

    public CharacterGrid Grid { get; private set; }

    /// <summary>Everyone who picks is ready, and the short "both ready" moment is running.</summary>
    public bool IsAllReady => _readySince is not null;

    public SideSelection Side(int side) => _sides[side];

    /// <summary>Whether <paramref name="side"/> chooses for themselves; the other plays a mirror.</summary>
    public bool Chooses(int side) => side != _remoteSide && _canPress(side);

    /// <summary>Whether <paramref name="side"/> is the other PC's player (online).</summary>
    public bool IsRemote(int side) => side == _remoteSide;

    /// <summary>Whether the remote side's pick has arrived; false when there is no remote side.</summary>
    public bool HasRemotePick => _remotePick is not null;

    /// <summary>The remote side's pick as it arrives (null until then); ignored without a remote side.</summary>
    public void ShowRemotePick(FighterPick? pick) => _remotePick = _remoteSide == NoRemoteSide ? null : pick;

    /// <summary>Starts over with every cursor on the character that player last played.</summary>
    /// <param name="canPress">Whether a player has a controller to choose with; asked every time, so a pad plugged in
    /// later joins.</param>
    /// <param name="remoteSide">The other PC's side online; <see cref="NoRemoteSide"/> when both are on this PC.</param>
    public void Begin(
        IReadOnlyList<SelectableCharacter> characters, Func<int, bool> canPress, PickMemory memory, int remoteSide = NoRemoteSide)
    {
        Characters = characters;
        Grid = new CharacterGrid(characters.Count, _settings.Columns);
        _canPress = canPress;
        _readySince = null;
        _remoteSide = remoteSide;
        _remotePick = null;
        for (int side = 0; side < SideCount; side++)
        {
            FighterPick last = memory.Of(side);
            int remembered = IndexOf(last.CharacterId);
            SideSelection selection = _sides[side];
            selection.RememberedCharacter = remembered;
            selection.RememberedColour = last.Colour;
            selection.Slot = Math.Max(remembered, 0);
            selection.Step = SelectStep.Character;
            selection.IsRandom = false;
        }
    }

    /// <summary>A press by <paramref name="side"/>; true when it was back with nothing left to undo (leave the screen).</summary>
    public bool Press(int side, MenuAction actions)
    {
        if (Characters.Count == 0)
        {
            return (actions & MenuAction.Back) != 0;
        }

        SideSelection selection = _sides[side];
        if ((actions & MenuAction.Back) != 0)
        {
            return Undo(side);
        }

        if ((actions & MenuAction.Confirm) != 0)
        {
            Confirm(side);
        }
        else if (selection.Step == SelectStep.Character)
        {
            selection.Slot = Grid.Move(selection.Slot, actions);
        }
        else if (selection.Step == SelectStep.Colour && (actions & (MenuAction.Left | MenuAction.Right)) != 0)
        {
            int step = (actions & MenuAction.Left) != 0 ? -1 : 1;
            selection.Colour = FreeColour(side, selection.Character, selection.Colour + step, step);
        }

        return false;
    }

    /// <summary>Call once per displayed frame; true once everyone has been ready for the "both ready" moment.</summary>
    public bool Update(long nowMilliseconds)
    {
        if (!EveryoneReady())
        {
            _readySince = null;
            return false;
        }

        _readySince ??= nowMilliseconds;
        return nowMilliseconds - _readySince.Value >= _settings.BothReadyMilliseconds;
    }

    /// <summary>What <paramref name="side"/> plays: their own pick, or the mirror when they don't choose.</summary>
    /// <remarks>Only once <see cref="Update"/> said everyone is ready.</remarks>
    public FighterPick Pick(int side)
    {
        (int character, int colour) = Shown(side);
        return new FighterPick(Characters[character].Id, (byte)colour);
    }

    /// <summary>
    /// The character and colour to show for <paramref name="side"/> now: the one under the cursor, the one chosen, or
    /// the mirror's. Character -1 is the random slot, or nothing to show yet.
    /// </summary>
    public (int Character, int Colour) Shown(int side)
    {
        SideSelection selection = _sides[side];
        if (Chooses(side))
        {
            if (selection.Step != SelectStep.Character)
            {
                return (selection.Character, selection.Colour);
            }

            if (Grid.IsRandom(selection.Slot))
            {
                return (-1, 0);
            }

            return (selection.Slot, FirstColour(selection));
        }

        if (IsRemote(side))
        {
            return RemoteShown();
        }

        int other = Other(side);
        if (!Chooses(other))
        {
            return (-1, 0);
        }

        (int character, int colour) = Shown(other);
        return character < 0 ? (-1, 0) : (character, FreeColour(side, character, colour + 1, 1));
    }

    /// <summary>Whether <paramref name="colour"/> of <paramref name="character"/> is locked in by the other player.</summary>
    public bool IsTaken(int side, int character, int colour)
    {
        int other = Other(side);
        if (IsRemote(other))
        {
            (int remoteCharacter, int remoteColour) = RemoteShown();
            return remoteCharacter >= 0 && remoteCharacter == character && remoteColour == colour;
        }

        SideSelection them = _sides[other];
        return Chooses(other) && them.Step == SelectStep.Ready && them.Character == character && them.Colour == colour;
    }

    // The remote pick on this PC's grid; a character this PC doesn't list shows as nothing (the match start says why).
    private (int Character, int Colour) RemoteShown()
    {
        int character = _remotePick is { } pick ? IndexOf(pick.CharacterId) : -1;
        return character < 0 ? (-1, 0) : (character, Wrap(_remotePick!.Value.Colour, Characters[character].Colours.Count));
    }

    private static int Other(int side) => 1 - side;

    private bool EveryoneReady()
    {
        bool anyone = false;
        for (int side = 0; side < SideCount; side++)
        {
            if (Chooses(side))
            {
                anyone = true;
                if (_sides[side].Step != SelectStep.Ready)
                {
                    return false;
                }
            }
        }

        return anyone && Characters.Count > 0;
    }

    private void Confirm(int side)
    {
        SideSelection selection = _sides[side];
        switch (selection.Step)
        {
            case SelectStep.Character when Grid.IsRandom(selection.Slot):
                selection.Character = _randomBelow(Characters.Count);
                int start = _randomBelow(Characters[selection.Character].Colours.Count);
                selection.Colour = FreeColour(side, selection.Character, start, 1);
                selection.IsRandom = true;
                Ready(side);
                break;
            case SelectStep.Character:
                selection.Character = selection.Slot;
                selection.IsRandom = false;
                selection.Colour = FreeColour(side, selection.Character, FirstColour(selection), 1);
                selection.Step = SelectStep.Colour;
                break;
            case SelectStep.Colour:
                Ready(side);
                break;
        }
    }

    private void Ready(int side)
    {
        SideSelection selection = _sides[side];
        selection.Step = SelectStep.Ready;

        // The other player still on this colour of this character moves off it: the second to ready gives way.
        int other = Other(side);
        SideSelection them = _sides[other];
        if (them.Step == SelectStep.Colour && them.Character == selection.Character && them.Colour == selection.Colour)
        {
            them.Colour = FreeColour(other, them.Character, them.Colour + 1, 1);
        }
    }

    private bool Undo(int side)
    {
        SideSelection selection = _sides[side];
        switch (selection.Step)
        {
            case SelectStep.Ready:
                selection.Step = selection.IsRandom ? SelectStep.Character : SelectStep.Colour;
                return false;
            case SelectStep.Colour:
                selection.Step = SelectStep.Character;
                return false;
            default:
                // Leaving would throw away the other player's choice: only once they are back on the grid too.
                int other = Other(side);
                return !Chooses(other) || _sides[other].Step == SelectStep.Character;
        }
    }

    // The first colour from start, going in direction, the other player hasn't locked in. A character with only one
    // colour is shared in a mirror: there is nothing to shift to.
    private int FreeColour(int side, int character, int start, int direction)
    {
        int count = Characters[character].Colours.Count;
        for (int i = 0; i < count; i++)
        {
            int colour = Wrap(start + (i * direction), count);
            if (!IsTaken(side, character, colour))
            {
                return colour;
            }
        }

        return Wrap(start, count);
    }

    // The colour last played on the character under the cursor, else its first; the file may have fewer colours now.
    private int FirstColour(SideSelection selection) => selection.Slot == selection.RememberedCharacter
        ? Wrap(selection.RememberedColour, Characters[selection.Slot].Colours.Count)
        : 0;

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    private int IndexOf(string? characterId)
    {
        for (int i = 0; i < Characters.Count; i++)
        {
            if (Characters[i].Id == characterId)
            {
                return i;
            }
        }

        return -1;
    }
}
