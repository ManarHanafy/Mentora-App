using api.Contracts.Journals;
using api.Contracts.AI;
using api.Contracts.Common;

namespace api.Services;

public interface IJournalService
{
    Task<JournalResponse> SubmitAsync(int userId, SubmitJournalRequest request, CancellationToken cancellationToken = default);
    Task<JournalResponse?> GetByIdAsync(int journalEntryId, CancellationToken cancellationToken = default);
    Task<PagedResponse<JournalSummaryResponse>> GetHistoryAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<JournalResponse?> UpdateAsync(int journalEntryId, UpdateJournalRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int journalEntryId, CancellationToken cancellationToken = default);
}

public interface IAIService
{
    Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default);
    Task<ChatAIResult> ChatAsync(
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
        CancellationToken cancellationToken = default);
    Task<ChatSummarizeResponse> SummarizeChatAsync(
        List<ChatMessage> messages,
        string? previousSummary,
        ChatSummarizeUserProfile userProfile,
        Dictionary<string, int> finalScores,
        CancellationToken cancellationToken = default);
}

public record AIServiceResult(string RawResponseJson, MentoraAnalyzeResponse Response);
