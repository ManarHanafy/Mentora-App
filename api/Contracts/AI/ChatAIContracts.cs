using System.Text.Json.Serialization;

namespace api.Contracts.AI;

public record ChatRequestPayload(
    [property: JsonPropertyName("user_message")] string UserMessage,
    [property: JsonPropertyName("conversation_ended")] bool ConversationEnded,
    [property: JsonPropertyName("chat_history")] List<ChatHistoryItem> ChatHistory,
    [property: JsonPropertyName("current_scores")] Dictionary<string, int> CurrentScores,
    [property: JsonPropertyName("recent_journals")] List<JournalItem>? RecentJournals,
    [property: JsonPropertyName("today_mood")] int TodayMood,
    [property: JsonPropertyName("suggested_exercises")] List<MentoraSuggestedExercise>? SuggestedExercises,
    [property: JsonPropertyName("user_memory")] string? UserMemory,
    [property: JsonPropertyName("user_profile")] UserProfileInfo UserProfile
);

public record ChatHistoryItem(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public record JournalItem(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("text")] string Text
);

public record UserProfileInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("preferred_language")] string? PreferredLanguage,
    [property: JsonPropertyName("gender")] string Gender
);

public record ChatAIResponse(
    [property: JsonPropertyName("response")] string Response,
    [property: JsonPropertyName("new_scores")] Dictionary<string, int> NewScores,
    [property: JsonPropertyName("deltas")] Dictionary<string, int> Deltas,
    [property: JsonPropertyName("risk_level")] string RiskLevel,
    [property: JsonPropertyName("tags")] List<string> Tags,
    [property: JsonPropertyName("suggested_exercises")] List<MentoraSuggestedExercise>? SuggestedExercises
);

public record ChatSummarizeRequest(
    [property: JsonPropertyName("conversation")] List<ChatHistoryItem> Conversation,
    [property: JsonPropertyName("existing_memory")] string? ExistingMemory,
    [property: JsonPropertyName("user_profile")] ChatSummarizeUserProfile UserProfile,
    [property: JsonPropertyName("final_scores")] Dictionary<string, int> FinalScores
);

public record ChatSummarizeUserProfile(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("gender")] string Gender
);

public record ChatSummarizeResponse(
    [property: JsonPropertyName("updated_memory")] string UpdatedMemory,
    [property: JsonPropertyName("suggested_exercises")] List<MentoraSuggestedExercise> SuggestedExercises
);

public record ChatAIResult(
    string Response,
    Dictionary<string, int> NewScores,
    Dictionary<string, int> Deltas,
    string RiskLevel,
    List<string> Tags,
    List<MentoraSuggestedExercise>? SuggestedExercises
);
