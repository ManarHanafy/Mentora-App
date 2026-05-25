using api.Contracts.AI;
using api.Contracts.Chats;
using api.Contracts.Exercises;
using api.Contracts.Common;

namespace api.Services;

public class ChatService(
    ApplicationDbContext db,
    IAIService aiService,
    ILogger<ChatService> logger) : IChatService
{
    private static readonly string[] RequiredParams = ["ANX", "DEP", "STR", "SLP", "SOC", "CDT", "SAFE", "ENG"];

    public async Task<int> CreateChatAsync(int userId, CancellationToken cancellationToken = default)
    {
        var exists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"User {userId} not found.");

        var chat = new Chat
        {
            UserId = userId,
            LastActivityAt = DateTime.UtcNow,
            IsEnded = false,
            RiskLevel = "normal",
            ScoreSnapshots =
            [
                new ChatScoreSnapshot()
            ]
        };

        db.Chats.Add(chat);
        await db.SaveChangesAsync(cancellationToken);

        return chat.Id;
    }

    public async Task<ChatResponse> SendMessageAsync(int userId, int chatId, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.");

        var chat = await db.Chats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            throw new KeyNotFoundException($"Chat {chatId} not found.");

        if (chat.IsEnded)
        {
            chat.IsEnded = false;
            chat.EndedAt = null;
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var parameterSnapshot = await db.UserParameterSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var currentScores = parameterSnapshot is null
            ? CreateZeroScores()
            : parameterSnapshot.ToParametersDictionary()
                .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        var recentMessages = await db.ChatMessages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        var recentJournals = await db.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayMood = await db.MoodEntries
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date == today)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (int?)m.Mood)
            .FirstOrDefaultAsync(cancellationToken);
        var suggestedExercises = await db.SuggestedExercises
            .Where(se => se.UserId == userId)
            .OrderByDescending(se => se.Id)
            .Take(5)
            .Select(se => new MentoraSuggestedExercise(se.ExerciseCode, se.Parameter, se.Score, se.ScoreRange))
            .ToListAsync(cancellationToken);

        var userDisplayName = BuildDisplayName(user);
        var aiResult = await aiService.ChatAsync(
            message.Trim(),
            recentMessages,
            currentScores,
            recentJournals,
            todayMood ?? chat.TodayMood ?? 3,
            chat.UserMemory,
            userDisplayName,
            null,
            "unspecified",
            suggestedExercises.Count == 0 ? null : suggestedExercises,
            cancellationToken);

        ValidateChatResult(aiResult);

        var now = DateTime.UtcNow;
        db.ChatMessages.AddRange(
            new ChatMessage
            {
                ChatId = chat.Id,
                Role = "user",
                Content = message.Trim(),
                CreatedAt = now
            },
            new ChatMessage
            {
                ChatId = chat.Id,
                Role = "assistant",
                Content = aiResult.Response,
                CreatedAt = now
            });

        var normalizedScores = NormalizeScores(aiResult.NewScores);
        db.ChatScoreSnapshots.Add(new ChatScoreSnapshot
        {
            ChatId = chat.Id,
            Anx = normalizedScores["ANX"],
            Dep = normalizedScores["DEP"],
            Str = normalizedScores["STR"],
            Slp = normalizedScores["SLP"],
            Soc = normalizedScores["SOC"],
            Cdt = normalizedScores["CDT"],
            Safe = normalizedScores["SAFE"],
            Eng = normalizedScores["ENG"],
            CreatedAt = now
        });

        var normalizedTags = aiResult.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var tag in normalizedTags)
        {
            db.ChatScoreTags.Add(new ChatScoreTag
            {
                ChatId = chat.Id,
                Tag = tag,
                CreatedAt = now
            });
        }

        chat.RiskLevel = aiResult.RiskLevel;
        chat.LastActivityAt = now;

        if (aiResult.RiskLevel == "crisis")
            logger.LogWarning("Crisis risk detected for chat {ChatId} user {UserId}", chat.Id, userId);

        await db.SaveChangesAsync(cancellationToken);

        return new ChatResponse(
            chat.Id,
            aiResult.Response,
            normalizedScores,
            NormalizeScores(aiResult.Deltas),
            aiResult.RiskLevel,
            normalizedTags,
            now);
    }

    public async Task<ChatMessageDetailsResponse?> GetMessageByIdAsync(int chatId, int messageId, int userId, CancellationToken cancellationToken = default)
    {
        var message = await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.Id == messageId && m.ChatId == chatId && m.Chat!.UserId == userId)
            .Select(m => new ChatMessageDetailsResponse(
                m.Id,
                m.ChatId,
                m.Role,
                m.Content,
                m.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return message;
    }

    public async Task<ChatDetailsResponse?> GetChatByIdAsync(int chatId, int userId, CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats
            .AsNoTracking()
            .Include(c => c.Messages)
            .Include(c => c.ScoreSnapshots)
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return null;

        return MapChatDetails(chat);
    }

    public async Task<PagedResponse<ChatHistoryResponse>> GetUserChatsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = db.Chats
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(c => new ChatHistoryResponse(
                c.Id,
                c.CreatedAt,
                c.EndedAt,
                c.IsEnded,
                c.Messages.Count,
                c.RiskLevel,
                c.Tags.Select(t => t.Tag).Distinct().ToList(),
                c.Summary))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);
        return new PagedResponse<ChatHistoryResponse>(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages,
            safePageNumber < totalPages,
            safePageNumber > 1);
    }

    public async Task<ChatSummariesResponse?> GetChatSummaryAsync(int chatId, int userId, CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats
            .AsNoTracking()
            .Where(c => c.Id == chatId && c.UserId == userId)
            .Select(c => new ChatSummariesResponse(
                c.Id,
                c.Summary,
                c.UserMemory,
                c.EndedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return chat;
    }

    public async Task<bool> EndChatAsync(int chatId, int userId, CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);
        if (chat is null)
            return false;

        if (!chat.IsEnded)
        {
            chat.IsEnded = true;
            chat.EndedAt = DateTime.UtcNow;
            chat.LastActivityAt = DateTime.UtcNow;
        }

        var (userProfile, finalScores) = await GetSummaryContextAsync(userId, cancellationToken);
        await UpdateChatSummaryAsync(chat, userId, userProfile, finalScores, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> EndInactiveChatsAsync(int inactivityMinutes, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-inactivityMinutes);
        var staleChats = await db.Chats
            .Include(c => c.Messages)
            .Where(c => !c.IsEnded && c.LastActivityAt <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var chat in staleChats)
        {
            chat.IsEnded = true;
            chat.EndedAt = DateTime.UtcNow;
            chat.LastActivityAt = DateTime.UtcNow;
            var (userProfile, finalScores) = await GetSummaryContextAsync(chat.UserId, cancellationToken);
            await UpdateChatSummaryAsync(chat, chat.UserId, userProfile, finalScores, cancellationToken);
        }

        if (staleChats.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return staleChats.Count;
    }

    public async Task<ChatSummarizeResult?> SummarizeChatAsync(int chatId, int userId, CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId, cancellationToken);

        if (chat is null)
            return null;

        var (userProfile, finalScores) = await GetSummaryContextAsync(chat.UserId, cancellationToken);
        var result = await UpdateChatSummaryAsync(chat, chat.UserId, userProfile, finalScores, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<ChatSummarizeResult> UpdateChatSummaryAsync(
        Chat chat,
        int userId,
        ChatSummarizeUserProfile userProfile,
        Dictionary<string, int> finalScores,
        CancellationToken cancellationToken)
    {
        var messages = chat.Messages.OrderBy(m => m.CreatedAt).ToList();
        var summary = messages.Count == 0
            ? new ChatSummarizeResponse("No conversation to summarize.", new List<MentoraSuggestedExercise>())
            : await aiService.SummarizeChatAsync(messages, chat.UserMemory, userProfile, finalScores, cancellationToken);

        chat.Summary = summary.UpdatedMemory;
        chat.UserMemory = summary.UpdatedMemory;

        var existingExercises = await db.SuggestedExercises
            .Where(se => se.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existingExercises.Count > 0)
            db.SuggestedExercises.RemoveRange(existingExercises);

        var mappedExercises = summary.SuggestedExercises
            .Select(ex => new SuggestedExercise
            {
                UserId = userId,
                JournalEntryId = null,
                ExerciseCode = ex.Id.Trim(),
                Parameter = ex.Parameter.Trim().ToUpperInvariant(),
                Score = ex.Score,
                ScoreRange = ex.ScoreRange.Trim()
            })
            .ToList();

        if (mappedExercises.Count > 0)
            db.SuggestedExercises.AddRange(mappedExercises);

        return new ChatSummarizeResult(
            summary.UpdatedMemory,
            mappedExercises.Select(ex => new SuggestedExerciseResponse(
                ex.ExerciseCode,
                ex.Parameter,
                ex.Score,
                ex.ScoreRange)).ToList());
    }

    private async Task<(ChatSummarizeUserProfile profile, Dictionary<string, int> finalScores)> GetSummaryContextAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var snapshot = await db.UserParameterSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var profile = new ChatSummarizeUserProfile(
            user is null ? "Unknown" : BuildDisplayName(user),
            "Unknown");

        var scores = snapshot is null
            ? CreateZeroScores()
            : snapshot.ToParametersDictionary()
                .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        return (profile, scores);
    }

    private static ChatDetailsResponse MapChatDetails(Chat chat)
    {
        var latest = chat.ScoreSnapshots.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
        var current = latest is null ? CreateZeroScores() : latest.ToScoreDictionary();

        return new ChatDetailsResponse(
            chat.Id,
            chat.CreatedAt,
            chat.EndedAt,
            chat.IsEnded,
            chat.RiskLevel,
            chat.Summary,
            chat.TodayMood,
            chat.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageResponse(m.Id, m.Role, m.Content, m.CreatedAt))
                .ToList(),
            new ChatScoresResponse(
                current["ANX"], current["DEP"], current["STR"], current["SLP"],
                current["SOC"], current["CDT"], current["SAFE"], current["ENG"]),
            chat.Tags.Select(t => t.Tag).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static Dictionary<string, int> GetCurrentScoresFromSnapshot(ChatScoreSnapshot? snapshot) =>
        snapshot is null ? CreateZeroScores() : snapshot.ToScoreDictionary();

    private static Dictionary<string, int> CreateZeroScores() =>
        RequiredParams.ToDictionary(k => k, _ => 0, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, int> NormalizeScores(Dictionary<string, int> values)
    {
        var normalized = values.ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        var missing = RequiredParams.Where(p => !normalized.ContainsKey(p)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"AI response missing required score keys: {string.Join(", ", missing)}");
        return RequiredParams.ToDictionary(k => k, k => normalized[k], StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateChatResult(ChatAIResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Response))
            throw new InvalidOperationException("AI chat response is empty.");
        if (result.RiskLevel is not ("normal" or "elevated" or "crisis"))
            throw new InvalidOperationException("AI chat response contains invalid risk level.");
        _ = NormalizeScores(result.NewScores);
        _ = NormalizeScores(result.Deltas);
    }

    private static string BuildDisplayName(User user)
    {
        var firstLast = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(firstLast) ? user.Username : firstLast;
    }
}
