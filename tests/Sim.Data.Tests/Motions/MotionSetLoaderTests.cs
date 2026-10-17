using System;
using System.IO;
using Sim.Data.Hashing;
using Sim.Data.Input;
using Sim.Data.Motions;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Input;
using Sim.Input.Motions;
using Sim.Rules;
using Xunit;

namespace Sim.Data.Tests.Motions;

public sealed class MotionSetLoaderTests
{
    private const string File = MotionSetSchema.File;
    private const int DashBit = 9;
    private const int SpecialBit = 8;

    private static readonly ButtonSetLookup _buttons =
        new(new ButtonSet(
            [new ButtonDefinition("a", 4), new ButtonDefinition("special", SpecialBit), new ButtonDefinition("dash", DashBit)]));

    private readonly InMemoryDataSource _data = new();

    public MotionSetLoaderTests()
    {
        // The repository file itself: the motions have no fixture copy, so the tests break exactly what ships.
        string text = System.IO.File.ReadAllText(Path.Combine(Fixture.RepositoryDataRoot, "input", "motions.json"));
        _data.SetText(File, text.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void RepositoryMotionsHaveTheAdr0002DefaultsAndEveryPhase1Motion()
    {
        MotionFile file = Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(12, file.MotionWindow);
        Assert.Equal(2, file.ButtonBuffer);
        foreach (int notation in new[] { 236, 214, 623, 41236, 63214, 22, 236236, 632146, 66, 44, 5, 2 })
        {
            Assert.True(file.Motions.TryFind(notation, out _), $"motions.json lacks {notation}");
        }
    }

    [Fact]
    public void RepositoryPriorityPutsLongerMotionsFirst()
    {
        MotionSet motions = Load(_data).Motions;

        Assert.True(IndexOf(motions, 623) < IndexOf(motions, 236));
        Assert.True(IndexOf(motions, 41236) < IndexOf(motions, 236));
        Assert.True(IndexOf(motions, 236236) < IndexOf(motions, 236));
        Assert.True(IndexOf(motions, 632146) < IndexOf(motions, 63214));
        Assert.True(IndexOf(motions, 2) < IndexOf(motions, 5));
    }

    [Fact]
    public void ReadsStepsAndOwnWindow()
    {
        MotionSet motions = Load(_data).Motions;

        int doubleDown = IndexOf(motions, 22);
        Assert.Equal([MotionStep.Of(2), MotionStep.Of(5), MotionStep.Of(2)], motions.StepsOf(doubleDown).ToArray());
        Assert.Equal(MotionDefinition.UsesMatchWindow, motions.WindowOf(doubleDown));
        Assert.Equal(MotionStep.Of(1, 2, 3), motions.StepsOf(IndexOf(motions, 2))[0]);
        Assert.Equal(1, motions.WindowOf(IndexOf(motions, 2)));
    }

    [Fact]
    public void AppliesTheTimingToTheMatchRules()
    {
        _data.Replace(File, "\"buttonBuffer\": 2", "\"buttonBuffer\": 4");
        MatchRules rules = default;

        Load(_data).ApplyTo(ref rules);

        Assert.Equal(12, rules.MotionWindow);
        Assert.Equal(4, rules.ButtonBuffer);
    }

    [Theory]
    [InlineData("\"steps\": \"2 3 6\"", "\"steps\": \"2 1 6\"")]
    [InlineData("{ \"motion\": \"623\"", "{ \"window\": 20, \"motion\": \"623\"")]
    [InlineData("\"motionWindow\": 12", "\"motionWindow\": 13")]
    [InlineData("\"buttonBuffer\": 2", "\"buttonBuffer\": 3")]
    [InlineData("\"shortcut\": \"5\"", "\"shortcut\": \"2\"")]
    public void EveryRuleIsInTheDataHash(string oldText, string newText)
    {
        DataHash before = HashOf(Load(_data));
        _data.Replace(File, oldText, newText);

        Assert.NotEqual(before, HashOf(Load(_data)));
    }

    [Fact]
    public void PriorityOrderIsInTheDataHash()
    {
        DataHash before = HashOf(Load(_data));
        const string DragonPunch = "{ \"motion\": \"623\", \"steps\": \"6 2 3\", \"shortcut\": \"6\" },\n";
        const string Fireball = "{ \"motion\": \"236\", \"steps\": \"2 3 6\", \"shortcut\": \"5\" },\n";
        _data.Replace(File, DragonPunch, "");
        _data.Replace(File, Fireball, Fireball + "    " + DragonPunch);

        Assert.NotEqual(before, HashOf(Load(_data)));
    }

    [Fact]
    public void RepositoryShortcutsAreGbvsStyle()
    {
        MotionSet motions = Load(_data).Motions;

        Assert.Equal(5, motions.ShortcutOf(IndexOf(motions, 236)));
        Assert.Equal(6, motions.ShortcutOf(IndexOf(motions, 623)));
        Assert.Equal(4, motions.ShortcutOf(IndexOf(motions, 214)));
        Assert.Equal(MotionDefinition.NoShortcut, motions.ShortcutOf(IndexOf(motions, 41236)));
    }

    [Theory]
    [InlineData("\"0\"")]
    [InlineData("\"56\"")]
    [InlineData("\"x\"")]
    public void ShortcutThatIsNotOneDirection(string shortcut)
    {
        _data.Replace(File, "\"shortcut\": \"5\"", $"\"shortcut\": {shortcut}");

        AssertSingleError("motions[6].shortcut", "must be one numpad direction from 1 to 9");
    }

    [Fact]
    public void ShortcutUsedByTwoMotions()
    {
        _data.Replace(File, "\"shortcut\": \"5\"", "\"shortcut\": \"6\"");

        AssertSingleError("motions[6].shortcut", "direction 6 is already the shortcut of motion 623");
    }

    [Fact]
    public void MissingFile()
    {
        _data.Remove(File);

        AssertSingleError("", "file does not exist");
    }

    [Fact]
    public void RepeatedMotion()
    {
        _data.Replace(File, "{ \"motion\": \"214\"", "{ \"motion\": \"236\"");

        AssertSingleError("motions[6].motion", "motion 236 is defined twice");
    }

    [Theory]
    [InlineData("\"motion\": \"0\"")]
    [InlineData("\"motion\": \"23a\"")]
    [InlineData("\"motion\": \"236236236\"")]
    public void NotationThatIsNotNumpad(string motion)
    {
        _data.Replace(File, "\"motion\": \"236\"", motion);

        AssertSingleError("motions[6].motion", "must be numpad notation");
    }

    [Theory]
    [InlineData("\"2 0 6\"")]
    [InlineData("\"2  6\"")]
    [InlineData("\"2 33 6\"")]
    [InlineData("\" 2 3 6\"")]
    public void StepsThatAreNotNumpadGroups(string steps)
    {
        _data.Replace(File, "\"steps\": \"2 3 6\"", $"\"steps\": {steps}");

        AssertSingleError("motions[6].steps", "must be steps of numpad digits");
    }

    [Fact]
    public void ConsecutiveStepsSharingADirection()
    {
        _data.Replace(File, "\"steps\": \"2 5 2\"", "\"steps\": \"2 2\"");

        AssertSingleError("motions[7].steps", "steps 1 and 2 share a direction");
    }

    [Fact]
    public void WindowLongerThanTheHistoryAllowsWithTheBuffer()
    {
        _data.Replace(File, "{ \"motion\": \"623\"", "{ \"window\": 63, \"motion\": \"623\"");

        AssertSingleError("motions[4].window", "63 plus buttonBuffer 2 is longer than the 64-frame input history");
    }

    [Fact]
    public void MotionWindowLongerThanTheHistoryAllowsWithTheBuffer()
    {
        _data.Replace(File, "\"motionWindow\": 12", "\"motionWindow\": 63");

        AssertSingleError("motionWindow", "63 plus buttonBuffer 2 is longer");
    }

    [Fact]
    public void ZeroMotionWindow()
    {
        _data.Replace(File, "\"motionWindow\": 12", "\"motionWindow\": 0");

        AssertSingleError("motionWindow", "must be a whole number from 1 to 64");
    }

    [Fact]
    public void ReadsTheMovementInputs()
    {
        MovementInputs movement = Load(_data).Movement;

        Assert.Equal(new MovementInputs(66, 44, 28, 1 << DashBit, 1 << SpecialBit), movement);
    }

    [Fact]
    public void AnUnknownSpecialButtonIsReported()
    {
        _data.Replace(File, "\"specialButton\": \"special\"", "\"specialButton\": \"skill\"");

        AssertSingleError("movementInputs.specialButton", "unknown button \"skill\"");
    }

    [Fact]
    public void AMovementMotionMustBeInTheList()
    {
        _data.Replace(File, "\"highJump\": \"28\"", "\"highJump\": \"88\"");

        AssertSingleError("movementInputs.highJump", "motion 88 isn't in the motions list");
    }

    [Fact]
    public void TheDashButtonMustBeInTheButtonSet()
    {
        _data.Replace(File, "\"dashButton\": \"dash\"", "\"dashButton\": \"run\"");

        AssertSingleError("movementInputs.dashButton", "unknown button \"run\"");
    }

    [Fact]
    public void TheDashButtonIsOptional()
    {
        _data.Replace(File, ", \"dashButton\": \"dash\"", string.Empty);

        Assert.Equal(0, Load(_data).Movement.DashButtonMask);
    }

    [Fact]
    public void EmptyMotionList()
    {
        _data.SetText(File, "{ \"schemaVersion\": 1, \"motionWindow\": 12, \"buttonBuffer\": 2, \"motions\": [], " +
            "\"movementInputs\": { \"dash\": \"66\", \"backdash\": \"44\", \"highJump\": \"28\" } }");

        DataValidationException exception = Assert.Throws<DataValidationException>(() => Load(_data));
        Assert.Contains(exception.Errors, error => error.Field == "motions" && error.Reason.Contains(
            "needs 1 to 64 motions", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownField()
    {
        _data.Replace(File, "{ \"motion\": \"623\"", "{ \"windw\": 3, \"motion\": \"623\"");

        AssertSingleError("motions[4].windw", "unknown field");
    }

    [Fact]
    public void NewerSchemaVersion()
    {
        _data.Replace(File, "\"schemaVersion\": 1", "\"schemaVersion\": 2");

        AssertSingleError("schemaVersion", "this build reads schema version 1");
    }

    private static MotionFile Load(IDataSource source)
    {
        DataErrors errors = new();
        MotionFile? file = MotionSetLoader.Load(source, _buttons, errors);
        errors.ThrowIfAny();
        return file!;
    }

    private static DataHash HashOf(MotionFile file)
    {
        using DataHashBuilder hash = new();
        MotionSetDataHash.Append(hash, file);
        return hash.Finish();
    }

    private static int IndexOf(MotionSet motions, int notation)
    {
        Assert.True(motions.TryFind(notation, out int motion));
        return motion;
    }

    private void AssertSingleError(string field, string reasonPart)
    {
        DataValidationException exception = Assert.Throws<DataValidationException>(() => Load(_data));
        DataError error = Assert.Single(exception.Errors);
        Assert.Equal(File, error.File);
        Assert.Equal(field, error.Field);
        Assert.Contains(reasonPart, error.Reason, StringComparison.Ordinal);
    }
}
