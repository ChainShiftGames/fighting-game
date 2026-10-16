using System;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Input;
using Xunit;

namespace Sim.Data.Tests.Input;

public sealed class ButtonSetLoaderTests
{
    private const string File = ButtonSetSchema.File;

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void RepositoryButtonSetIsTheAdr0010Layout()
    {
        ButtonSet buttons = Fixture.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot)).Buttons;

        string[] expectedIds = ["a", "b", "c", "d", "special", "dash"];
        Assert.Equal(expectedIds.Length, buttons.Count);
        for (int button = 0; button < expectedIds.Length; button++)
        {
            Assert.Equal(expectedIds[button], buttons.IdOf(button));
            Assert.Equal(1 << (InputWord.FirstButtonBit + button), buttons.MaskOf(button));
        }
    }

    [Fact]
    public void MovesResolveButtonsThroughTheLoadedSet()
    {
        _data.Replace(File, "\"id\": \"a\"", "\"id\": \"light\"");
        _data.Replace(Fixture.MoveFile("5a"), "\"button\": \"a\"", "\"button\": \"light\"");

        GameData data = Fixture.Load(_data);

        Assert.True(data.Buttons.TryFind("light", out _));
    }

    [Fact]
    public void MoveWithAButtonOutsideTheSetListsTheLoadedIds()
    {
        _data.Replace(File, "\"id\": \"a\"", "\"id\": \"light\"");

        AssertSingleError(Fixture.MoveFile("5a"), "input.button", "(the button set has: light, b, c, d, special, dash)");
    }

    [Fact]
    public void MissingFile()
    {
        _data.Remove(File);

        AssertSingleError(File, "", "file does not exist");
    }

    [Fact]
    public void RepeatedId()
    {
        _data.Replace(File, "\"id\": \"b\"", "\"id\": \"a\"");

        AssertSingleError(File, "buttons[1].id", "button 'a' is defined twice");
    }

    [Fact]
    public void RepeatedBit()
    {
        _data.Replace(File, "\"bit\": 5", "\"bit\": 4");

        AssertSingleError(File, "buttons[1].bit", "bit 4 is already used by button 'a'");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(16)]
    public void BitOutsideTheButtonBits(int bit)
    {
        _data.Replace(File, "\"bit\": 4", $"\"bit\": {bit}");

        AssertSingleError(File, "buttons[0].bit", "must be a whole number from 4 to 15");
    }

    [Fact]
    public void IdThatIsNotSnakeCase()
    {
        _data.Replace(File, "\"id\": \"special\"", "\"id\": \"Special\"");

        AssertSingleError(File, "buttons[4].id", "");
    }

    [Fact]
    public void EmptyButtonList()
    {
        _data.SetText(File, "{ \"schemaVersion\": 1, \"buttons\": [] }");

        AssertSingleError(File, "buttons", "needs at least one button");
    }

    [Fact]
    public void UnknownField()
    {
        _data.Replace(File, "\"bit\": 4", "\"bit\": 4, \"bti\": 4");

        AssertSingleError(File, "buttons[0].bti", "unknown field");
    }

    [Fact]
    public void NewerSchemaVersion()
    {
        _data.Replace(File, "\"schemaVersion\": 1", "\"schemaVersion\": 2");

        AssertSingleError(File, "schemaVersion", "this build reads schema version 1");
    }

    private void AssertSingleError(string file, string field, string reasonPart)
    {
        DataValidationException exception = Assert.Throws<DataValidationException>(() => Fixture.Load(_data));
        DataError error = Assert.Single(exception.Errors);
        Assert.Equal(file, error.File);
        Assert.Equal(field, error.Field);
        Assert.Contains(reasonPart, error.Reason, StringComparison.Ordinal);
    }
}
