using System.Text.Json.Serialization;
using api.Contracts.Exercises;

namespace api.Contracts.Chats;

public record CreateChatResponse(int ChatId);

public record SendChatMessageRequest(string Message);

public record EndChatRequest(bool HasEnded = true);

public record ChatResponse(
    int ChatId,
    string Message,
    Dictionary<string, int> CurrentScores,
    Dictionary<string, int> Deltas,
    string RiskLevel,
    List<string> Tags,
    DateTime Timestamp
);

public record ChatHistoryResponse(
    int Id,
    DateTime CreatedAt,
    DateTime? EndedAt,
    bool IsEnded,
    int MessageCount,
    string RiskLevel,
    List<string> Tags,
    string? Summary
);

public record ChatDetailsResponse(
    int Id,
    DateTime CreatedAt,
    DateTime? EndedAt,
    bool IsEnded,
    string RiskLevel,
    string? Summary,
    int? TodayMood,
    List<ChatMessageResponse> Messages,
    ChatScoresResponse CurrentScores,
    List<string> AllTags
);

public record ChatMessageResponse(
    int Id,
    string Role,
    string Content,
    DateTime CreatedAt
);

public record ChatMessageDetailsResponse(
    int Id,
    int ChatId,
    string Role,
    string Content,
    DateTime CreatedAt
);

public record ChatSummariesResponse(
    int ChatId,
    string? Summary,
    string? UserMemory,
    DateTime? EndedAt
);

public record ChatSummarizeResult(
    [property: JsonPropertyName("updated_memory")] string UpdatedMemory,
    [property: JsonPropertyName("suggested_exercises")] List<SuggestedExerciseResponse> SuggestedExercises
);

public record ChatScoresResponse(
    int Anx, int Dep, int Str, int Slp,
    int Soc, int Cdt, int Safe, int Eng
);
