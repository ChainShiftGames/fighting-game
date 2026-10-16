using Scenarios.Tests.Expectations;

namespace Scenarios.Tests.Runner;

/// <summary>An expectation and the frames it must hold on (every frame of the range).</summary>
public sealed record FrameExpectation(FrameRange Frames, IExpectation Expectation, ScenarioLocation Location);
