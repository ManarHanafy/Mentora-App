using api.Contracts.Users;

namespace api.Services;

public interface IOnboardingScoringEngine
{
    OnboardingScoreResult ComputeScores(IReadOnlyList<OnboardingScoringAnswer> answers);
}

public record OnboardingScoringAnswer(string Parameter, IReadOnlyList<OnboardingScoringOption> Options);

public record OnboardingScoringOption(int? ScorePoints, IReadOnlyList<OnboardingScoringModifier> Modifiers);

public record OnboardingScoringModifier(string Parameter, int? Value);

public record OnboardingScoreResult(
    ParameterValues Parameters,
    Dictionary<string, int> BaseScores,
    Dictionary<string, int> Modifiers,
    int SlpModifier
);

public class OnboardingScoringEngine : IOnboardingScoringEngine
{
    private static readonly string[] ParameterKeys = ["ANX", "DEP", "STR", "SLP", "SOC", "CDT", "SAFE", "ENG"];

    public OnboardingScoreResult ComputeScores(IReadOnlyList<OnboardingScoringAnswer> answers)
    {
        var baseScores = ParameterKeys.ToDictionary(k => k, _ => 0, StringComparer.OrdinalIgnoreCase);
        var modifiers = ParameterKeys.ToDictionary(k => k, _ => 0, StringComparer.OrdinalIgnoreCase);
        var slpModifier = 0;

        foreach (var answer in answers)
        {
            if (string.Equals(answer.Parameter, "multi_parameter_context", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var option in answer.Options)
                {
                    foreach (var modifier in option.Modifiers)
                    {
                        if (modifier.Value is null)
                            continue;

                        if (modifiers.ContainsKey(modifier.Parameter))
                            modifiers[modifier.Parameter] += modifier.Value.Value;
                    }
                }
                continue;
            }

            if (string.Equals(answer.Parameter, "SLP_modifier", StringComparison.OrdinalIgnoreCase))
            {
                slpModifier += answer.Options.Sum(o => o.ScorePoints ?? 0);
                continue;
            }

            if (!baseScores.ContainsKey(answer.Parameter))
                continue;

            baseScores[answer.Parameter] += answer.Options.Sum(o => o.ScorePoints ?? 0);
        }

        var dep = baseScores["DEP"] + modifiers["DEP"];
        var anx = baseScores["ANX"] + modifiers["ANX"];
        var str = baseScores["STR"] + modifiers["STR"];
        var slp = baseScores["SLP"] + modifiers["SLP"] + slpModifier;
        var soc = baseScores["SOC"] + modifiers["SOC"];
        var cdt = baseScores["CDT"] + modifiers["CDT"];
        var eng = baseScores["ENG"];
        var safe = baseScores["SAFE"];

        var parameters = new ParameterValues(
            Clamp(anx),
            Clamp(dep),
            Clamp(str),
            Clamp(slp),
            Clamp(soc),
            Clamp(cdt),
            Clamp(safe),
            Clamp(eng));

        return new OnboardingScoreResult(parameters, baseScores, modifiers, slpModifier);
    }

    private static int Clamp(int value) => Math.Max(0, value);
}
