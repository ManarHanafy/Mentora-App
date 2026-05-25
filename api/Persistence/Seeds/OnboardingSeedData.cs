namespace api.Persistence.Seeds;

public static class OnboardingSeedData
{
    public const string DefaultLocale = "en";

    public static IReadOnlyList<OnboardingQuestionSeed> Questions { get; } =
    [
        new(
            QuestionId: 1,
            Category: "Mood & Interest",
            Parameter: "DEP",
            QuestionText: "Over the past 2 weeks, how often have you felt down, hopeless, or lost interest in things you usually enjoy?",
            InputControlType: "single_select",
            ResponseOptions:
            [
                new(1, "Not at all", ScorePoints: 0),
                new(2, "Several days", ScorePoints: 2),
                new(3, "More than half the days", ScorePoints: 4),
                new(4, "Nearly every day", ScorePoints: 7)
            ]
        ),
        new(
            QuestionId: 2,
            Category: "Anxiety & Worry",
            Parameter: "ANX",
            QuestionText: "How often do you feel nervous, anxious, or unable to control your worrying?",
            InputControlType: "single_select",
            ResponseOptions:
            [
                new(1, "Rarely or never", ScorePoints: 0),
                new(2, "Sometimes (a few days a week)", ScorePoints: 2),
                new(3, "Often (most days)", ScorePoints: 5),
                new(4, "Almost always", ScorePoints: 8)
            ]
        ),
        new(
            QuestionId: 3,
            Category: "Stress & Pressure",
            Parameter: "STR",
            QuestionText: "In the last month, how often have you felt overwhelmed or unable to handle the demands in your life?",
            InputControlType: "single_select",
            ResponseOptions:
            [
                new(1, "Almost never", ScorePoints: 0),
                new(2, "Sometimes", ScorePoints: 2),
                new(3, "Fairly often", ScorePoints: 5),
                new(4, "Very often", ScorePoints: 8)
            ]
        ),
        new(
            QuestionId: 4,
            Category: "Sleep Quality",
            Parameter: "SLP",
            QuestionText: "How would you describe your sleep over the past 2 weeks?",
            InputControlType: "single_select",
            ScoringNote: "SLP metric is inverted — higher values mean healthier sleep outcomes",
            ResponseOptions:
            [
                new(1, "I sleep well and wake up feeling rested", ScorePoints: 6),
                new(2, "I sometimes have trouble falling or staying asleep", ScorePoints: 4),
                new(3, "I often struggle with sleep and feel tired during the day", ScorePoints: 2),
                new(4, "I rarely sleep well and feel exhausted most of the time", ScorePoints: 0)
            ]
        ),
        new(
            QuestionId: 5,
            Category: "Social Connection",
            Parameter: "SOC",
            QuestionText: "How connected do you feel to the people in your life (friends, family, colleagues)?",
            InputControlType: "single_select",
            ScoringNote: "SOC metric is inverted — higher values mean healthier social connection",
            ResponseOptions:
            [
                new(1, "Very connected — I have meaningful relationships", ScorePoints: 6),
                new(2, "Somewhat connected — I have people but feel distant sometimes", ScorePoints: 3),
                new(3, "Mostly disconnected — I feel alone even around others", ScorePoints: 1),
                new(4, "Very lonely — I have no one I feel close to", ScorePoints: 0)
            ]
        ),
        new(
            QuestionId: 6,
            Category: "Cognitive Distortions",
            Parameter: "CDT",
            QuestionText: "How often do you catch yourself catastrophizing (expecting the worst) or thinking in all-or-nothing terms like 'I always fail' or 'nothing ever works out for me'?",
            InputControlType: "single_select",
            ResponseOptions:
            [
                new(1, "Rarely — I can usually keep things in perspective", ScorePoints: 0),
                new(2, "Sometimes — I notice these thoughts occasionally", ScorePoints: 2),
                new(3, "Often — these thoughts come up most days", ScorePoints: 4),
                new(4, "Very often — I struggle to think differently", ScorePoints: 7)
            ]
        ),
        new(
            QuestionId: 7,
            Category: "Healthy Habits & Engagement",
            Parameter: "ENG",
            QuestionText: "Are you currently practicing any healthy coping habits? (e.g. exercise, meditation, journaling, breathing exercises)",
            InputControlType: "single_select",
            ScoringNote: "ENG metric is inverted — higher values mean healthier habit engagement",
            ResponseOptions:
            [
                new(1, "Yes, regularly — I have consistent healthy routines", ScorePoints: 6),
                new(2, "Sometimes — I do them occasionally but not consistently", ScorePoints: 3),
                new(3, "Rarely — I know I should but struggle to keep up", ScorePoints: 1),
                new(4, "Not at all — I haven't been doing any", ScorePoints: 0)
            ]
        ),
        new(
            QuestionId: 8,
            Category: "Physical Energy & Fatigue",
            Parameter: "SLP_modifier",
            QuestionText: "How would you describe your energy levels throughout the day?",
            InputControlType: "single_select",
            ScoringNote: "This score directly modifies the primary SLP score from question 4",
            ResponseOptions:
            [
                new(1, "High energy — I feel alert and active most of the day", ScorePoints: 1),
                new(2, "Moderate — I have some dips but manage okay", ScorePoints: 0),
                new(3, "Low — I feel tired most of the day", ScorePoints: -1),
                new(4, "Very low — I feel exhausted even after sleeping", ScorePoints: -2)
            ]
        ),
        new(
            QuestionId: 9,
            Category: "Main Life Challenge",
            Parameter: "multi_parameter_context",
            QuestionText: "What's been weighing on you the most lately? (choose up to 2)",
            InputControlType: "multi_select",
            MaxAllowedSelections: 2,
            ResponseOptions:
            [
                new(1, "Work, study, or career pressure", MetricModifiers: new Dictionary<string, string> { ["STR"] = "2" }),
                new(2, "Relationships, loneliness, or social stress", MetricModifiers: new Dictionary<string, string> { ["SOC"] = "-1" }),
                new(3, "Sleep problems or constant fatigue", MetricModifiers: new Dictionary<string, string> { ["SLP"] = "-1" }),
                new(4, "Low mood, lack of motivation, or sadness", MetricModifiers: new Dictionary<string, string> { ["DEP"] = "2" }),
                new(5, "Anxiety, overthinking, or constant worry", MetricModifiers: new Dictionary<string, string> { ["ANX"] = "2" }),
                new(6, "Negative thinking patterns or self-criticism", MetricModifiers: new Dictionary<string, string> { ["CDT"] = "2" }),
                new(7, "I just want to build better habits", MetricModifiers: new Dictionary<string, string> { ["ENG"] = "context_note" }),
                new(8, "I'm going through a tough time and need support", MetricModifiers: new Dictionary<string, string> { ["DEP"] = "1", ["ANX"] = "1" })
            ]
        ),
        new(
            QuestionId: 10,
            Category: "Safety Check",
            Parameter: "SAFE",
            QuestionText: "In the past month, have you had any thoughts of harming yourself or feeling like you'd be better off not being here?",
            InputControlType: "single_select",
            IsSensitiveQuestion: true,
            PreQuestionDisclaimer: "This question helps us make sure you're safe. Your answer is private and helps us support you better.",
            ConditionalActions: new Dictionary<int, string>
            {
                [1] = "continue_normally",
                [2] = "flag_elevated_monitoring_show_supportive_message",
                [3] = "immediately_show_crisis_resources_before_continuing",
                [4] = "immediately_show_crisis_resources_before_continuing"
            },
            ResponseOptions:
            [
                new(1, "No, not at all", ScorePoints: 0),
                new(2, "I've had fleeting thoughts but they pass quickly", ScorePoints: 2),
                new(3, "I've had these thoughts more than once", ScorePoints: 4),
                new(4, "Yes, I've been having these thoughts often", ScorePoints: 6)
            ]
        )
    ];

    public record OnboardingQuestionSeed(
        int QuestionId,
        string Category,
        string Parameter,
        string QuestionText,
        string InputControlType,
        List<OnboardingOptionSeed> ResponseOptions,
        string? ScoringNote = null,
        int? MaxAllowedSelections = null,
        bool IsSensitiveQuestion = false,
        string? PreQuestionDisclaimer = null,
        Dictionary<int, string>? ConditionalActions = null
    );

    public record OnboardingOptionSeed(
        int OptionId,
        string OptionText,
        int? ScorePoints = null,
        Dictionary<string, string>? MetricModifiers = null
    );
}
