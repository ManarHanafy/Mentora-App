using System.Text;
using System.Text.Json;
using api.Contracts.AI;

namespace api.Services;

/// <summary>
/// HTTP implementation of <see cref="IAIService"/> that calls the Mentora AI API.
/// Endpoint: POST https://mentorra.pythonanywhere.com/analyze
/// </summary>
public class RealAIService(HttpClient httpClient, ILogger<RealAIService> logger) : IAIService
{
    private static readonly string[] AllParams = ["ANX", "DEP", "STR", "SLP", "SOC", "CDT", "SAFE", "ENG"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public async Task<AIServiceResult> AnalyseAsync(
        string journalText,
        Dictionary<string, int> currentScores,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(journalText))
            throw new InvalidOperationException("journal_text must not be empty.");

        var normalized = currentScores
            .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        var missing = AllParams.Where(p => !normalized.ContainsKey(p)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"current_scores is missing required keys: {string.Join(", ", missing)}");

        var orderedScores = AllParams.ToDictionary(p => p, p => normalized[p], StringComparer.OrdinalIgnoreCase);
        var payload = new
        {
            journal_text = journalText,
            current_scores = orderedScores
        };
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        logger.LogInformation("Mentora AI request sent. Endpoint={Endpoint} PayloadBytes={PayloadBytes}", "/analyze", Encoding.UTF8.GetByteCount(payloadJson));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/analyze")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("Mentora AI response received. Endpoint={Endpoint} StatusCode={StatusCode} PayloadBytes={PayloadBytes}", "/analyze", (int)response.StatusCode, Encoding.UTF8.GetByteCount(responseJson));

            response.EnsureSuccessStatusCode();

            var body = JsonSerializer.Deserialize<MentoraAnalyzeResponse>(responseJson, JsonOptions);
            if (body is null)
                throw new InvalidOperationException("Mentora API returned an empty response.");

            ValidateResponseContract(body, journalText);
            return new AIServiceResult(responseJson, body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mentora AI response deserialization failed.");
            throw new InvalidOperationException("AI analysis response is invalid JSON or has an unexpected schema.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Mentora AI API call failed: {Message}", ex.Message);
            throw new InvalidOperationException("AI analysis service is currently unavailable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Mentora AI API call timed out.");
            throw new InvalidOperationException("AI analysis service timed out.", ex);
        }
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
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new InvalidOperationException("user_message must not be empty.");

        var normalized = currentScores
            .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        var missing = AllParams.Where(p => !normalized.ContainsKey(p)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"current_scores is missing required keys: {string.Join(", ", missing)}");

        var orderedScores = AllParams.ToDictionary(p => p, p => normalized[p], StringComparer.OrdinalIgnoreCase);
        var payload = new ChatRequestPayload(
            userMessage.Trim(),
            false,
            chatHistory
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatHistoryItem(m.Role, m.Content))
                .ToList(),
            orderedScores,
            recentJournals?
                .OrderByDescending(j => j.CreatedAt)
                .Take(5)
                .Select(j => new JournalItem(j.CreatedAt.ToString("yyyy-MM-dd"), j.JournalText))
                .ToList(),
            todayMood,
            suggestedExercises,
            userMemory,
            new UserProfileInfo(userName, preferredLanguage, gender));

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        logger.LogInformation("Mentora chat request sent. Endpoint={Endpoint} PayloadBytes={PayloadBytes}", "/chat", Encoding.UTF8.GetByteCount(payloadJson));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/chat")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("Mentora chat response received. Endpoint={Endpoint} StatusCode={StatusCode} PayloadBytes={PayloadBytes}", "/chat", (int)response.StatusCode, Encoding.UTF8.GetByteCount(responseJson));

            response.EnsureSuccessStatusCode();

            var body = JsonSerializer.Deserialize<ChatAIResponse>(responseJson, JsonOptions)
                ?? throw new InvalidOperationException("Mentora chat API returned an empty response.");

            ValidateChatResponseContract(body);
            return new ChatAIResult(
                body.Response,
                body.NewScores,
                body.Deltas,
                body.RiskLevel,
                body.Tags,
                body.SuggestedExercises);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mentora chat response deserialization failed.");
            throw new InvalidOperationException("AI chat response is invalid JSON or has an unexpected schema.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Mentora chat API call failed: {Message}", ex.Message);
            throw new InvalidOperationException("AI chat service is currently unavailable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Mentora chat API call timed out.");
            throw new InvalidOperationException("AI chat service timed out.", ex);
        }
    }

    public async Task<ChatSummarizeResponse> SummarizeChatAsync(
        List<ChatMessage> messages,
        string? previousSummary,
        ChatSummarizeUserProfile userProfile,
        Dictionary<string, int> finalScores,
        CancellationToken cancellationToken = default)
    {
        var payload = new ChatSummarizeRequest(
            messages.OrderBy(m => m.CreatedAt)
                .Select(m => new ChatHistoryItem(m.Role, m.Content))
                .ToList(),
            previousSummary,
            userProfile,
            finalScores);

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        logger.LogInformation("Mentora summarize request sent. Endpoint={Endpoint} PayloadBytes={PayloadBytes}", "/summarize", Encoding.UTF8.GetByteCount(payloadJson));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/summarize")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation("Mentora summarize response received. Endpoint={Endpoint} StatusCode={StatusCode} PayloadBytes={PayloadBytes}", "/summarize", (int)response.StatusCode, Encoding.UTF8.GetByteCount(responseJson));

            response.EnsureSuccessStatusCode();

            var body = JsonSerializer.Deserialize<ChatSummarizeResponse>(responseJson, JsonOptions)
                ?? throw new InvalidOperationException("Mentora summarize API returned an empty response.");

            if (string.IsNullOrWhiteSpace(body.UpdatedMemory))
                throw new InvalidOperationException("Mentora summarize API returned empty summary.");

            if (body.SuggestedExercises is null)
                throw new InvalidOperationException("Mentora summarize API returned missing suggested_exercises.");

            return body;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mentora summarize response deserialization failed.");
            throw new InvalidOperationException("AI summarize response is invalid JSON or has an unexpected schema.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Mentora summarize API call failed: {Message}", ex.Message);
            throw new InvalidOperationException("AI summarize service is currently unavailable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Mentora summarize API call timed out.");
            throw new InvalidOperationException("AI summarize service timed out.", ex);
        }
    }

    private static void ValidateResponseContract(MentoraAnalyzeResponse response, string journalText)
    {
        if (response.MatchedItems is null || response.Deltas is null || response.NewScores is null ||
            response.Tags is null || response.SuggestedExercises is null || string.IsNullOrWhiteSpace(response.RiskLevel))
        {
            throw new InvalidOperationException("AI response is missing required fields.");
        }

        if (response.RiskLevel is not ("normal" or "elevated" or "crisis"))
            throw new InvalidOperationException("AI response contains invalid risk_level.");

        var deltas = response.Deltas.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newScores = response.NewScores.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (AllParams.Any(p => !deltas.Contains(p)) || AllParams.Any(p => !newScores.Contains(p)))
            throw new InvalidOperationException("AI response must include all 8 parameter keys in deltas and new_scores.");

        foreach (var group in response.MatchedItems)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.Parameter) || group.Items is null || string.IsNullOrWhiteSpace(group.Reason))
                throw new InvalidOperationException("AI response contains invalid matched_items group.");

            foreach (var item in group.Items)
            {
                if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.MatchText))
                    throw new InvalidOperationException("AI response contains invalid matched_items item.");

                if (item.Intensity03 is < 0 or > 3)
                    throw new InvalidOperationException("AI response contains out-of-range intensity_0_3.");

                //if (!journalText.Contains(item.MatchText, StringComparison.Ordinal))
                //    throw new InvalidOperationException("AI response match_text must be an exact substring of journal_text.");
            }
        }

        foreach (var ex in response.SuggestedExercises)
        {
            if (ex is null || string.IsNullOrWhiteSpace(ex.Id) || string.IsNullOrWhiteSpace(ex.Parameter) || string.IsNullOrWhiteSpace(ex.ScoreRange))
                throw new InvalidOperationException("AI response contains invalid suggested_exercises item.");
        }
    }

    private static void ValidateChatResponseContract(ChatAIResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Response))
            throw new InvalidOperationException("Chat response is empty.");

        if (response.NewScores is null || response.Deltas is null || response.Tags is null || string.IsNullOrWhiteSpace(response.RiskLevel))
            throw new InvalidOperationException("Chat response is missing required fields.");

        if (response.RiskLevel is not ("normal" or "elevated" or "crisis"))
            throw new InvalidOperationException("Chat response contains invalid risk_level.");

        var newScores = response.NewScores.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deltas = response.Deltas.Keys.Select(k => k.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (AllParams.Any(p => !newScores.Contains(p)) || AllParams.Any(p => !deltas.Contains(p)))
            throw new InvalidOperationException("Chat response must include all 8 parameter keys in deltas and new_scores.");
    }

}
