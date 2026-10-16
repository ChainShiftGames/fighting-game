using System.Collections.Generic;

namespace Scenarios.Tests.Runner;

/// <summary>A parsed scenario file: setup, inputs per frame and expectations, ready to run.</summary>
public sealed class Scenario
{
    public Scenario(string file, ScenarioSetup setup, ScenarioInputs inputs, IReadOnlyList<FrameExpectation> expectations)
    {
        File = file;
        Setup = setup;
        Inputs = inputs;
        Expectations = expectations;

        int lastFrame = inputs.LastFrame;
        foreach (FrameExpectation expectation in expectations)
        {
            lastFrame = System.Math.Max(lastFrame, expectation.Frames.Last);
        }

        LastFrame = lastFrame;
    }

    public string File { get; }

    public ScenarioSetup Setup { get; }

    public ScenarioInputs Inputs { get; }

    /// <summary>In file order, which is also the order they are checked within a frame.</summary>
    public IReadOnlyList<FrameExpectation> Expectations { get; }

    /// <summary>The run ends after this frame: the last one with an input or an expectation.</summary>
    public int LastFrame { get; }
}
