using System.Text.Json;
using api.Contracts.AI;

namespace api.Services;

/// <summary>
/// Stub AI service — produces keyword-driven results for development/testing.
/// To use real AI: implement IAIService, register it in DependencyInjection.cs — nothing else changes.
/// </summary>
public class MockAIService : IAIService
{
    private static readonly Random _rng = new();

    private static readonly (string kw, string param, string itemId, string reason)[] _hints =
    [
        ("anxious",    "anx",  "ANX1", "User expresses anxiety"),
        ("anxiety",    "anx",  "ANX1", "User mentions anxiety"),
        ("worry",      "anx",  "ANX2", "User expresses worry"),
        ("panic",      "anx",  "ANX3", "User describes panic"),
        ("sad",        "dep",  "DEP1", "User expresses sadness"),
        ("hopeless",   "dep",  "DEP2", "User expresses hopelessness"),
        ("pointless",  "dep",  "DEP3", "User reports loss of meaning"),
        ("depress",    "dep",  "DEP4", "User mentions depression"),
        ("stress",     "str",  "STR1", "User reports stress"),
        ("overwhelm",  "str",  "STR2", "User feels overwhelmed"),
        ("pressure",   "str",  "STR3", "User mentions pressure"),
        ("sleep",      "slp",  "SLP1", "User mentions sleep issues"),
        ("tired",      "slp",  "SLP2", "User reports fatigue"),
        ("exhausted",  "slp",  "SLP3", "User reports exhaustion"),
        ("alone",      "soc",  "SOC1", "User reports being alone"),
        ("isolat",     "soc",  "SOC2", "User describes isolation"),
        ("lonely",     "soc",  "SOC3", "User expresses loneliness"),
        ("worst",      "cdt",  "CDT1", "Catastrophic language detected"),
        ("never",      "cdt",  "CDT2", "Absolute negative thinking detected"),
        ("catastroph", "cdt",  "CDT3", "Catastrophic thinking detected"),
        ("hurt",       "safe", "SAF1", "Possible safety concern"),
        ("harm",       "safe", "SAF2", "Possible self-harm language"),
        ("happy",      "eng",  "ENG1", "Positive engagement detected"),
        ("enjoy",      "eng",  "ENG2", "User reports enjoyment"),
        ("motivat",    "eng",  "ENG3", "Positive motivation detected"),
        ("excit",      "eng",  "ENG4", "User expresses excitement"),
    ];

    // Mock exercise codes mirroring the AI API format (e.g. EX_ANX_01)
    private static readonly Dictionary<string, (string exerciseCode, string scoreRange)[]> _exerciseMap = new()
    {
        { "anx",  [("EX_ANX_01", "1-9"),  ("EX_ANX_02", "10-15"), ("EX_ANX_01", "16-20")] },
        { "dep",  [("EX_DEP_01", "1-9"),  ("EX_DEP_02", "10-15"), ("EX_DEP_01", "16-20")] },
        { "str",  [("EX_STR_01", "1-9"),  ("EX_STR_02", "10-15"), ("EX_STR_01", "16-20")] },
        { "slp",  [("EX_SLP_01", "1-7"),  ("EX_SLP_01", "8-20")]                          },
        { "soc",  [("EX_SOC_01", "1-9"),  ("EX_SOC_01", "10-20")]                         },
        { "cdt",  [("EX_CDT_01", "1-9"),  ("EX_CDT_01", "10-20")]                         },
        { "safe", [("EX_SAFE_01", "1-3"), ("EX_SAFE_01", "4-20")]                          },
        { "eng",  [("EX_ENG_01", "0-7"),  ("EX_ENG_01", "0-4")]                            },
    };

    public Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default)
    {
        var text          = journalText.ToLowerInvariant();
        var matchedGroups = new Dictionary<string, MentoraMatchedGroup>(StringComparer.OrdinalIgnoreCase);
        var deltas        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var newScores     = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var suggested     = new List<MentoraSuggestedExercise>();
        var tags          = new List<string>();
        var matchedParams = new HashSet<string>();

        foreach (var (kw, param, itemId, reason) in _hints)
        {
            int idx = text.IndexOf(kw, StringComparison.Ordinal);
            if (idx < 0) continue;

            int    start   = Math.Max(0, idx - 10);
            int    end     = Math.Min(journalText.Length, idx + kw.Length + 25);
            string excerpt = journalText[start..end].Trim();

            if (!matchedGroups.TryGetValue(param, out var group))
            {
                group = new MentoraMatchedGroup(param.ToUpperInvariant(), new List<MentoraMatchedItem>(), reason);
                matchedGroups[param] = group;
            }

            group.Items.Add(new MentoraMatchedItem(itemId, _rng.Next(1, 4), excerpt));

            matchedParams.Add(param);
        }

        var allParams = new[] { "anx", "dep", "str", "slp", "soc", "cdt", "safe", "eng" };
        foreach (var param in allParams)
        {
            int current  = currentScores.TryGetValue(param, out var v) ? v : 0;
            int delta    = matchedParams.Contains(param) ? _rng.Next(1, 4) : _rng.Next(-1, 1);
            int newScore = Math.Clamp(current + delta, 0, 20);

            deltas[param.ToUpperInvariant()]    = newScore - current;
            newScores[param.ToUpperInvariant()] = newScore;
        }

        int safe = newScores.GetValueOrDefault("SAFE");
        int dep  = newScores.GetValueOrDefault("DEP");
        int anx  = newScores.GetValueOrDefault("ANX");

        var riskLevel = (safe, dep, anx) switch
        {
            ( >= 5, _,  _ ) => "crisis",
            ( >= 3, _,  _ ) => "high",
            ( _,  >= 15, _) => "high",
            ( _,  >= 10, _) => "elevated",
            ( _,  _, >= 15) => "elevated",
            _               => "normal"
        };

        if (matchedParams.Contains("anx"))  tags.Add("anxiety");
        if (matchedParams.Contains("dep"))  tags.Add("depression");
        if (matchedParams.Contains("str"))  tags.Add("stress");
        if (matchedParams.Contains("slp"))  tags.Add("sleep_issues");
        if (matchedParams.Contains("soc"))  tags.Add("social_isolation");
        if (matchedParams.Contains("cdt"))  tags.Add("catastrophic_thinking");
        if (matchedParams.Contains("safe")) tags.Add("safety_concern");
        if (matchedParams.Contains("eng"))  tags.Add("positive_engagement");
        if (!tags.Any())                    tags.Add("general");

        var addedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in matchedParams)
        {
            if (!_exerciseMap.TryGetValue(param, out var options)) continue;
            int score = newScores.GetValueOrDefault(param.ToUpperInvariant());

            var band = options.FirstOrDefault(o =>
            {
                var parts = o.scoreRange.Split('-');
                return parts.Length == 2
                    && int.TryParse(parts[0], out int lo)
                    && int.TryParse(parts[1], out int hi)
                    && score >= lo && score <= hi;
            });

            if (band == default) band = options[0];

            if (addedCodes.Add(band.exerciseCode))
            {
                suggested.Add(new MentoraSuggestedExercise(
                    band.exerciseCode,
                    param.ToUpperInvariant(),
                    score,
                    band.scoreRange));
            }
        }

        var response = new MentoraAnalyzeResponse(
            matchedGroups.Values.ToList(),
            deltas,
            newScores,
            tags,
            riskLevel,
            suggested);

        return Task.FromResult(new AIServiceResult(JsonSerializer.Serialize(response), response));
    }

    public async Task<ChatAIResult> ChatAsync(
        string userMessage,
        List<ChatMessage> chatHistory,
        Dictionary<string, int> currentScores,
        List<JournalEntry>? recentJournals,
        int todayMood,
        string? userMemory,
        string userName,
        string? preferredLanguage,
        string gender,
        List<MentoraSuggestedExercise>? suggestedExercises = null,
        CancellationToken cancellationToken = default)
    {
        var analysis = (await AnalyseAsync(userMessage, currentScores, cancellationToken)).Response;
        return new ChatAIResult(
            $"Thanks {userName}, I hear you. {userMessage}",
            analysis.NewScores,
            analysis.Deltas,
            analysis.RiskLevel is "high" ? "elevated" : analysis.RiskLevel,
            analysis.Tags,
            analysis.SuggestedExercises);
    }

    public Task<ChatSummarizeResponse> SummarizeChatAsync(
        List<ChatMessage> messages,
        string? previousSummary,
        ChatSummarizeUserProfile userProfile,
        Dictionary<string, int> finalScores,
        CancellationToken cancellationToken = default)
    {
        var combined = string.Join(" ", messages
            .OrderBy(m => m.CreatedAt)
            .Where(m => m.Role == "user")
            .Select(m => m.Content.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .TakeLast(3));

        var summary = string.IsNullOrWhiteSpace(combined)
            ? "No summary available."
            : $"Summary: {combined}";

        return Task.FromResult(new ChatSummarizeResponse(summary, new List<MentoraSuggestedExercise>()));
    }
}
