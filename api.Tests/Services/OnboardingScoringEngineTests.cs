using api.Services;
using FluentAssertions;

namespace api.Tests.Services;

public class OnboardingScoringEngineTests
{
    [Fact]
    public void ComputeScores_AppliesFormulasAndClampsToZero()
    {
        var engine = new OnboardingScoringEngine();
        var answers = new List<OnboardingScoringAnswer>
        {
            new("DEP", [new OnboardingScoringOption(4, [])]),
            new("ANX", [new OnboardingScoringOption(2, [])]),
            new("STR", [new OnboardingScoringOption(5, [])]),
            new("SLP", [new OnboardingScoringOption(0, [])]),
            new("SOC", [new OnboardingScoringOption(1, [])]),
            new("CDT", [new OnboardingScoringOption(4, [])]),
            new("ENG", [new OnboardingScoringOption(3, [])]),
            new("SAFE", [new OnboardingScoringOption(4, [])]),
            new("SLP_modifier", [new OnboardingScoringOption(-2, [])]),
            new("multi_parameter_context",
            [
                new OnboardingScoringOption(null,
                [
                    new OnboardingScoringModifier("DEP", 2),
                    new OnboardingScoringModifier("ANX", 1),
                    new OnboardingScoringModifier("STR", 2),
                    new OnboardingScoringModifier("SLP", -1),
                    new OnboardingScoringModifier("SOC", -1),
                    new OnboardingScoringModifier("CDT", 2),
                    new OnboardingScoringModifier("ENG", 5)
                ])
            ])
        };

        var result = engine.ComputeScores(answers);

        result.Parameters.Dep.Should().Be(6);
        result.Parameters.Anx.Should().Be(3);
        result.Parameters.Str.Should().Be(7);
        result.Parameters.Slp.Should().Be(0);
        result.Parameters.Soc.Should().Be(0);
        result.Parameters.Cdt.Should().Be(6);
        result.Parameters.Eng.Should().Be(3);
        result.Parameters.Safe.Should().Be(4);
    }
}
