# 🤖 AI CHATBOT MODULE - SENIOR IMPLEMENTATION PLAN (REVISED)
## For: T-Hunter14/APP Repository
**Date:** 2026-05-11  
**Status:** Ready for Copilot Agent Execution  
**AI API Contracts:** Integrated and Verified

---

## PHASE 0: CRITICAL CONTEXT

### 0.1 Existing Database & Modules ⚠️
- **Existing Tables:** Users, JournalEntries, JournalScores, JournalTags, MatchedItems, MatchedItemDetails, SuggestedExercises, UserParameterSnapshots, RefreshTokens
- **Existing Services:** JournalService, ExerciseService, UserService, AuthService, AccountService, StatisticsService
- **Existing AI Integration:** RealAIService calls `/analyze` endpoint for journal analysis
- **Existing Controllers:** JournalsController, ExercisesController, AuthController, UsersController, etc.
- **Database Pattern:** EF Core 10.0.3 with SQL Server, configuration-based entity mapping

### 0.2 What's Different About Chat Module
- **NOT replacing Journal module** - runs in parallel
- **New workflow:** Chat session creation → message exchange → auto-summarization
- **Stateful conversation tracking** - maintains message history per session
- **Different AI endpoints:** `/chat` and `/summarize` (not `/analyze`)
- **Score tracking pattern:** Same 8 parameters (ANX, DEP, STR, SLP, SOC, CDT, SAFE, ENG)
- **Risk detection:** Same crisis detection but continuous in chat context

### 0.3 Key Assumptions
- **No breaking changes** to Journal module
- **New tables only** - no modifications to existing tables except User navigation
- **Same JWT auth** as Journal endpoints
- **Same AI service pattern** as existing RealAIService
- **Background job** for auto-cleanup (new concept, same pattern as QueuedHostedService)

---

## PHASE 1: AI API CONTRACT SPECIFICATION

### 1.1 Chat Endpoint: `POST /chat`

**Full Request Payload Structure:**
```json
{
  "user_message": "String - current user input (Arabic, English, or Franco-Arabic supported)",
  "conversation_ended": "Boolean - false for ongoing, true when user indicates end",
  "chat_history": [
    {
      "role": "String - 'user' or 'assistant'",
      "content": "String - message text"
    }
  ],
  "current_scores": {
    "ANX": "Integer 0-7 - Anxiety level",
    "DEP": "Integer 0-7 - Depression level",
    "STR": "Integer 0-7 - Stress level",
    "SLP": "Integer 0-7 - Sleep/energy (higher is better)",
    "SOC": "Integer 0-7 - Social connection (higher is better)",
    "CDT": "Integer 0-7 - Cognitive distortions",
    "SAFE": "Integer 0-7 - Safety/self-harm risk",
    "ENG": "Integer 0-7 - Engagement with healthy habits (higher is better)"
  },
  "recent_journals": [
    {
      "date": "String ISO format - 2026-04-10",
      "text": "String - journal entry text"
    }
  ],
  "today_mood": "Integer 1-5 - Daily mood rating (1=worst, 5=best)",
  "suggested_exercises": [
    {
      "id": "String - exercise identifier",
      "parameter": "String - which parameter this targets",
      "score": "Integer - score range this applies to",
      "score_range": "String - e.g. '0–4' or '4–7'"
    }
  ],
  "user_memory": "String or null - long-term memory summary from previous conversations",
  "user_profile": {
    "name": "String - user's name",
    "preferred_language": "String or null - language preference",
    "gender": "String - user's gender"
  }
}
```

**Full Response Payload Structure:**
```json
{
  "response": "String - AI's conversational response text",
  "new_scores": {
    "ANX": "Integer - updated anxiety score",
    "DEP": "Integer - updated depression score",
    "STR": "Integer - updated stress score",
    "SLP": "Integer - updated sleep score",
    "SOC": "Integer - updated social score",
    "CDT": "Integer - updated cognitive distortion score",
    "SAFE": "Integer - updated safety score",
    "ENG": "Integer - updated engagement score"
  },
  "deltas": {
    "ANX": "Integer - change from current_scores.ANX to new_scores.ANX",
    "DEP": "Integer - change in depression",
    "STR": "Integer - change in stress",
    "SLP": "Integer - change in sleep",
    "SOC": "Integer - change in social",
    "CDT": "Integer - change in cognitive distortions",
    "SAFE": "Integer - change in safety",
    "ENG": "Integer - change in engagement"
  },
  "risk_level": "String - 'normal', 'elevated', or 'crisis'",
  "tags": [
    "String - emotional themes detected, e.g. 'work_anxiety', 'sleep_problems'"
  ],
  "suggested_exercises": [
    {
      "id": "String - exercise ID",
      "parameter": "String - parameter code",
      "score": "Integer - new score value",
      "score_range": "String - applicable range"
    }
  ] or null
}
```

**Key Behaviors:**
- Response includes new scores: **backend must save these**
- Deltas show score changes: **use for trend analysis**
- Risk level can be "crisis": **must trigger emergency protocol**
- Tags help categorize emotional state: **store for analytics**
- Exercises may be null: **handle gracefully**

### 1.2 Summarize Endpoint: `POST /summarize`

**Request Payload Structure:**
```json
{
  "messages": [
    {
      "role": "String - 'user' or 'assistant'",
      "content": "String - message text"
    }
  ],
  "previous_summary": "String or null - summary from previous chat if resuming"
}
```

**Response Payload Structure:**
```json
{
  "summary": "String - AI-generated summary of the conversation"
}
```

**Key Behaviors:**
- Called AFTER chat ends (conversation_ended = true)
- Pass all messages from the session
- Store summary in Chat.Summary field for record-keeping
- Used for future context if user resumes conversations

---

## PHASE 2: DATABASE SCHEMA DESIGN (REVISED FOR AI CONTRACTS)

### 2.1 New Table: `Chats`

**Purpose:** Store chat session metadata aligned with AI API contract

**Columns:**
```sql
CREATE TABLE [dbo].[Chats] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [UserId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [EndedAt] DATETIME2 NULL,
    [IsEnded] BIT NOT NULL DEFAULT 0,
    [LastActivityAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Summary] NVARCHAR(MAX) NULL,
    [RiskLevel] NVARCHAR(50) NOT NULL DEFAULT 'normal',
    [TodayMood] INT NULL,
    [UserMemory] NVARCHAR(MAX) NULL,
    
    CONSTRAINT [FK_Chats_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_RiskLevel] CHECK ([RiskLevel] IN ('normal', 'elevated', 'crisis'))
);

CREATE INDEX [IX_Chats_UserId_CreatedAt] ON [dbo].[Chats]([UserId], [CreatedAt] DESC);
CREATE INDEX [IX_Chats_IsEnded] ON [dbo].[Chats]([IsEnded]);
```

**Field Explanations:**
- `TodayMood` (INT): Daily mood rating sent to AI (1-5), nullable for flexibility
- `UserMemory` (NVARCHAR(MAX)): Long-term memory summary from AI, persisted for next chat context
- `RiskLevel`: Constraint prevents invalid values, matches AI response options

---

### 2.2 New Table: `ChatMessages`

**Purpose:** Store conversational messages in exact format for AI API reuse

**Columns:**
```sql
CREATE TABLE [dbo].[ChatMessages] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [ChatId] INT NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_ChatMessages_Chats] FOREIGN KEY ([ChatId]) 
        REFERENCES [dbo].[Chats]([Id]) ON DELETE CASCADE,
    CONSTRAINT [CK_Role] CHECK ([Role] IN ('user', 'assistant'))
);

CREATE INDEX [IX_ChatMessages_ChatId_CreatedAt] ON [dbo].[ChatMessages]([ChatId], [CreatedAt]);
```

**Field Explanations:**
- `Role` constraint ensures only valid values ('user', 'assistant')
- Messages stored exactly as received from user/AI for API replay
- Ordered by CreatedAt: SELECT last 20 messages for chat_history in API calls

---

### 2.3 New Table: `ChatScoreSnapshots`

**Purpose:** Track score state at each message boundary for deltas and trend analysis

**Columns:**
```sql
CREATE TABLE [dbo].[ChatScoreSnapshots] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [ChatId] INT NOT NULL,
    [Anx] INT NOT NULL DEFAULT 0,
    [Dep] INT NOT NULL DEFAULT 0,
    [Str] INT NOT NULL DEFAULT 0,
    [Slp] INT NOT NULL DEFAULT 0,
    [Soc] INT NOT NULL DEFAULT 0,
    [Cdt] INT NOT NULL DEFAULT 0,
    [Safe] INT NOT NULL DEFAULT 0,
    [Eng] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_ChatScoreSnapshots_Chats] FOREIGN KEY ([ChatId]) 
        REFERENCES [dbo].[Chats]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ChatScoreSnapshots_ChatId] ON [dbo].[ChatScoreSnapshots]([ChatId]);
```

**Field Explanations:**
- One snapshot per AI response (not per message)
- Stores `new_scores` from AI response
- Multiple snapshots per chat show score progression
- Used to calculate deltas for next API call: previous snapshot → current scores

---

### 2.4 New Table: `ChatScoreTags`

**Purpose:** Store emotional tags detected by AI for each message

**Columns:**
```sql
CREATE TABLE [dbo].[ChatScoreTags] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [ChatId] INT NOT NULL,
    [Tag] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_ChatScoreTags_Chats] FOREIGN KEY ([ChatId]) 
        REFERENCES [dbo].[Chats]([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ChatScoreTags_ChatId] ON [dbo].[ChatScoreTags]([ChatId]);
```

**Field Explanations:**
- Denormalized from AI response tags array
- Enables filtering chats by emotional theme
- Examples: 'work_anxiety', 'sleep_problems', 'relationship_conflict'

---

### 2.5 Modified Table: `Users` (Existing)

**Add Navigation Property:**
```csharp
public ICollection<Chat> Chats { get; set; } = new List<Chat>();
```

**No column changes required** - just relationship tracking

---

## PHASE 3: ENTITY MODELS

### 3.1 Entity: `Chat.cs`
**Location:** `api/Entities/Chat.cs`  
**Inherits:** `AuditableEntity`

**Properties:**
```csharp
public int Id { get; set; }
public int UserId { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? EndedAt { get; set; }
public bool IsEnded { get; set; } = false;
public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
public string? Summary { get; set; }
public string RiskLevel { get; set; } = "normal";
public int? TodayMood { get; set; }
public string? UserMemory { get; set; }

// Navigation properties
public User? User { get; set; }
public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
public ICollection<ChatScoreSnapshot> ScoreSnapshots { get; set; } = new List<ChatScoreSnapshot>();
public ICollection<ChatScoreTag> Tags { get; set; } = new List<ChatScoreTag>();
```

---

### 3.2 Entity: `ChatMessage.cs`
**Location:** `api/Entities/ChatMessage.cs`  
**Inherits:** `AuditableEntity`

**Properties:**
```csharp
public int Id { get; set; }
public int ChatId { get; set; }
public string Role { get; set; } = string.Empty; // "user" or "assistant"
public string Content { get; set; } = string.Empty;

// Navigation
public Chat? Chat { get; set; }
```

**Note:** Role values match AI API contract exactly ("user", "assistant")

---

### 3.3 Entity: `ChatScoreSnapshot.cs`
**Location:** `api/Entities/ChatScoreSnapshot.cs`  
**Inherits:** `AuditableEntity`

**Properties:**
```csharp
public int Id { get; set; }
public int ChatId { get; set; }
public int Anx { get; set; }
public int Dep { get; set; }
public int Str { get; set; }
public int Slp { get; set; }
public int Soc { get; set; }
public int Cdt { get; set; }
public int Safe { get; set; }
public int Eng { get; set; }

// Navigation
public Chat? Chat { get; set; }

// Helper method to build dict for next API call
public Dictionary<string, int> ToScoreDictionary()
{
    return new()
    {
        { "ANX", Anx },
        { "DEP", Dep },
        { "STR", Str },
        { "SLP", Slp },
        { "SOC", Soc },
        { "CDT", Cdt },
        { "SAFE", Safe },
        { "ENG", Eng }
    };
}
```

---

### 3.4 Entity: `ChatScoreTag.cs`
**Location:** `api/Entities/ChatScoreTag.cs`  
**Inherits:** `AuditableEntity`

**Properties:**
```csharp
public int Id { get; set; }
public int ChatId { get; set; }
public string Tag { get; set; } = string.Empty;

// Navigation
public Chat? Chat { get; set; }
```

---

### 3.5 Update Entity: `User.cs`

**Add Navigation Property after RefreshTokens:**
```csharp
public ICollection<Chat> Chats { get; set; } = new List<Chat>();
```

---

## PHASE 4: DATA TRANSFER OBJECTS (DTOs)

### 4.1 Chat Request/Response DTOs

**File:** `api/Contracts/ChatContracts.cs`

```csharp
namespace api.Contracts;

// ── REQUEST DTOs ───────────────────────────────────────────

public record CreateChatRequest;

public record SendChatMessageRequest(string Message);

public record EndChatRequest(bool HasEnded = true);

// ── RESPONSE DTOs ──────────────────────────────────────────

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

public record ChatScoresResponse(
    int Anx, int Dep, int Str, int Slp,
    int Soc, int Cdt, int Safe, int Eng
);
```

---

### 4.2 AI API Contract DTOs

**File:** `api/Contracts/AI/ChatAIContracts.cs`

```csharp
namespace api.Contracts.AI;

// ── AI REQUEST DTOs ────────────────────────────────────────

public record ChatRequestPayload(
    string user_message,
    bool conversation_ended,
    List<ChatHistoryItem> chat_history,
    Dictionary<string, int> current_scores,
    List<JournalItem>? recent_journals,
    int today_mood,
    string? user_memory,
    UserProfileInfo user_profile
);

public record ChatHistoryItem(
    string role,
    string content
);

public record JournalItem(
    string date,
    string text
);

public record UserProfileInfo(
    string name,
    string? preferred_language,
    string gender
);

// ── AI RESPONSE DTOs ───────────────────────────────────────

public record ChatAIResponse(
    string response,
    Dictionary<string, int> new_scores,
    Dictionary<string, int> deltas,
    string risk_level,
    List<string> tags,
    List<SuggestedExerciseItem>? suggested_exercises
);

public record SuggestedExerciseItem(
    string id,
    string parameter,
    int score,
    string score_range
);

// ── SUMMARIZATION DTOs ────────────────────────────────────

public record ChatSummarizeRequest(
    List<ChatHistoryItem> messages,
    string? previous_summary
);

public record ChatSummarizeResponse(
    string summary
);

// ── WRAPPER ────────────────────────────────────────────────

public record ChatAIResult(
    string Response,
    Dictionary<string, int> NewScores,
    Dictionary<string, int> Deltas,
    string RiskLevel,
    List<string> Tags,
    List<SuggestedExerciseItem>? SuggestedExercises
);
```

---

## PHASE 5: ENTITY CONFIGURATIONS (EF CORE)

### 5.1 Configuration: `ChatConfiguration.cs`
**File:** `api/Persistence/Configurations/ChatConfiguration.cs`

**Responsibilities:**
- Map Chat entity to [Chats] table
- Configure foreign key: UserId → Users.Id (cascade delete)
- Configure relationships: Messages (one-to-many), ScoreSnapshots (one-to-many), Tags (one-to-many)
- Configure indexes: (UserId, CreatedAt DESC), (IsEnded)
- Configure check constraints: RiskLevel IN ('normal', 'elevated', 'crisis')

---

### 5.2 Configuration: `ChatMessageConfiguration.cs`
**File:** `api/Persistence/Configurations/ChatMessageConfiguration.cs`

**Responsibilities:**
- Map ChatMessage entity to [ChatMessages] table
- Configure foreign key: ChatId → Chats.Id (cascade delete)
- Configure check constraint: Role IN ('user', 'assistant')
- Configure index: (ChatId, CreatedAt)

---

### 5.3 Configuration: `ChatScoreSnapshotConfiguration.cs`
**File:** `api/Persistence/Configurations/ChatScoreSnapshotConfiguration.cs`

**Responsibilities:**
- Map ChatScoreSnapshot entity to [ChatScoreSnapshots] table
- Configure foreign key: ChatId → Chats.Id (cascade delete)
- Configure index: (ChatId)
- Set all score fields as non-nullable with default 0

---

### 5.4 Configuration: `ChatScoreTagConfiguration.cs`
**File:** `api/Persistence/Configurations/ChatScoreTagConfiguration.cs`

**Responsibilities:**
- Map ChatScoreTag entity to [ChatScoreTags] table
- Configure foreign key: ChatId → Chats.Id (cascade delete)
- Configure index: (ChatId)

---

## PHASE 6: UPDATE DBCONTEXT

**File:** `api/Persistence/ApplicationDbContext.cs`

**Add DbSet Properties:**
```csharp
public DbSet<Chat> Chats { get; set; } = null!;
public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
public DbSet<ChatScoreSnapshot> ChatScoreSnapshots { get; set; } = null!;
public DbSet<ChatScoreTag> ChatScoreTags { get; set; } = null!;
```

**Location:** After existing DbSet properties (after `SuggestedExercises`, before closing brace)

---

## PHASE 7: SERVICE INTERFACE

**File:** `api/Services/IChatService.cs`

**Methods to Define:**

1. **CreateChatAsync**
   - Input: `int userId`, `int? todayMood`, `CancellationToken`
   - Output: `Task<ChatResponse>`
   - Behavior: Create new Chat, save initial scores (all zeros), return welcome message

2. **SendMessageAsync**
   - Input: `int userId`, `int chatId`, `string message`, `CancellationToken`
   - Output: `Task<ChatResponse>`
   - Behavior: 
     - Validate chat not ended
     - Get last 20 messages from chat
     - Get latest score snapshot for current_scores
     - Load recent journals (last 5 from user's journal entries if available)
     - Get user's name and basic profile info
     - Call `IAIService.ChatAsync()` with full payload
     - Save user message to ChatMessages
     - Save AI response to ChatMessages
     - Save new scores to ChatScoreSnapshots
     - Save tags to ChatScoreTags
     - Update Chat.RiskLevel, LastActivityAt, UserMemory (if provided in response)
     - Check for crisis risk level and trigger alert if needed
     - Return ChatResponse with AI message and scores

3. **GetChatByIdAsync**
   - Input: `int chatId`, `int userId`, `CancellationToken`
   - Output: `Task<ChatDetailsResponse?>`
   - Behavior: Load chat with authorization, return null if not found or unauthorized

4. **GetUserChatsAsync**
   - Input: `int userId`, `int pageNumber`, `int pageSize`, `CancellationToken`
   - Output: `Task<List<ChatHistoryResponse>>`
   - Behavior: Paginate user's chats, include tag and summary info

5. **EndChatAsync**
   - Input: `int chatId`, `int userId`, `CancellationToken`
   - Output: `Task<bool>`
   - Behavior: Mark chat as ended, queue summarization background job

6. **EndInactiveChatsAsync**
   - Input: `int inactivityMinutes`, `CancellationToken`
   - Output: `Task<int>` (count)
   - Behavior: Query inactive chats, mark as ended, return count

7. **SummarizeChatAsync**
   - Input: `int chatId`, `CancellationToken`
   - Output: `Task<bool>`
   - Behavior: 
     - Load chat messages (ordered chronologically)
     - Build ChatHistoryItem list
     - Get current Chat.UserMemory as previous_summary
     - Call `IAIService.SummarizeChatAsync(messages, previous_summary)`
     - Save response to Chat.Summary
     - Update Chat.UserMemory with summary (for next chat context)
     - Persist changes

---

## PHASE 8: SERVICE IMPLEMENTATION

**File:** `api/Services/ChatService.cs`

**Injected Dependencies:**
- `ApplicationDbContext db`
- `IAIService aiService`
- `IUserService userService` (to get user profile info)
- `ILogger<ChatService> logger`

**Key Implementation Details:**

### GetCurrentScoresForChat()
Helper method to retrieve latest score snapshot or default to zeros:
```csharp
private Dictionary<string, int> GetCurrentScoresForChat(Chat chat)
{
    var latestSnapshot = chat.ScoreSnapshots.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
    if (latestSnapshot is null)
        return new() { { "ANX", 0 }, { "DEP", 0 }, { "STR", 0 }, { "SLP", 0 },
                       { "SOC", 0 }, { "CDT", 0 }, { "SAFE", 0 }, { "ENG", 0 } };
    
    return latestSnapshot.ToScoreDictionary();
}
```

### BuildChatHistory()
Helper to get last 20 messages for AI context:
```csharp
private List<ChatHistoryItem> BuildChatHistory(List<ChatMessage> messages)
{
    return messages
        .OrderBy(m => m.CreatedAt)
        .TakeLast(20)
        .Select(m => new ChatHistoryItem(m.Role, m.Content))
        .ToList();
}
```

### SendMessageAsync() Full Flow:
1. Load chat with related data (Include messages, score snapshots)
2. Validate chat.IsEnded == false (throw if ended)
3. Get current_scores from latest snapshot
4. Build chat_history from last 20 messages
5. Load user's recent journals (SELECT TOP 5 FROM JournalEntries WHERE UserId = userId ORDER BY CreatedAt DESC)
6. Get user profile (name, gender, language preference)
7. Build ChatRequestPayload
8. Call aiService.ChatAsync()
9. Create and persist ChatMessage(role="user", content=message)
10. Create and persist ChatMessage(role="assistant", content=aiResponse.response)
11. Create and persist ChatScoreSnapshot with new scores
12. For each tag in aiResponse.tags: Create and persist ChatScoreTag
13. Update chat.RiskLevel = aiResponse.risk_level
14. Update chat.LastActivityAt = DateTime.UtcNow
15. If crisis detected: log warning and trigger alert (implement alert logic)
16. SaveChangesAsync()
17. Return ChatResponse with all data

---

## PHASE 9: AI SERVICE EXTENSIONS

### 9.1 Update Interface: `IAIService.cs`

**Add Two Methods:**

```csharp
public interface IAIService
{
    // Existing method
    Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default);

    // NEW METHODS FOR CHAT MODULE
    
    /// <summary>
    /// Call Mentora AI /chat endpoint for conversational response
    /// </summary>
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Call Mentora AI /summarize endpoint to generate chat summary
    /// </summary>
    Task<string> SummarizeChatAsync(
        List<ChatMessage> messages,
        string? previousSummary,
        CancellationToken cancellationToken = default);
}
```

---

### 9.2 Implement in `RealAIService.cs`

**Implementation: ChatAsync()**

```csharp
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
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(userMessage))
        throw new ArgumentException("User message cannot be empty.");

    // Build chat history for API
    var chatHistoryItems = chatHistory
        .OrderBy(m => m.CreatedAt)
        .Select(m => new ChatHistoryItem(m.Role, m.Content))
        .ToList();

    // Build recent journals for API (last 5)
    var recentJournalsItems = recentJournals?
        .OrderByDescending(j => j.CreatedAt)
        .Take(5)
        .Select(j => new JournalItem(j.CreatedAt.ToString("yyyy-MM-dd"), j.Content))
        .ToList();

    // Build user profile for API
    var userProfile = new UserProfileInfo(userName, preferredLanguage, gender);

    // Build request payload
    var payload = new ChatRequestPayload(
        userMessage,
        conversation_ended: false,
        chatHistoryItems,
        currentScores,
        recentJournalsItems,
        todayMood,
        userMemory,
        userProfile
    );

    // Serialize and log
    var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
    logger.LogInformation("Chat API request payload: {Payload}", SanitizeForLog(payloadJson));

    // Send request
    using var request = new HttpRequestMessage(HttpMethod.Post, "/chat")
    {
        Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
    };

    try
    {
        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Chat API response: {Response}", SanitizeForLog(responseJson));

        response.EnsureSuccessStatusCode();

        // Deserialize response
        var chatResponse = JsonSerializer.Deserialize<ChatAIResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Chat API returned null response.");

        // Validate response contract
        ValidateChatResponse(chatResponse);

        // Build result
        return new ChatAIResult(
            chatResponse.response,
            chatResponse.new_scores,
            chatResponse.deltas,
            chatResponse.risk_level,
            chatResponse.tags,
            chatResponse.suggested_exercises
        );
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "Chat API response deserialization failed");
        throw new InvalidOperationException("Chat service response is invalid JSON.", ex);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Chat API request failed");
        throw new InvalidOperationException("Chat service is currently unavailable.", ex);
    }
    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
    {
        logger.LogError(ex, "Chat API request timed out");
        throw new InvalidOperationException("Chat service timed out.", ex);
    }
}
```

**Helper: ValidateChatResponse()**

```csharp
private static void ValidateChatResponse(ChatAIResponse response)
{
    if (string.IsNullOrWhiteSpace(response.response))
        throw new InvalidOperationException("AI response is empty.");

    if (response.new_scores is null || response.deltas is null || response.tags is null)
        throw new InvalidOperationException("AI response is missing required fields.");

    if (!new[] { "normal", "elevated", "crisis" }.Contains(response.risk_level))
        throw new InvalidOperationException("Invalid risk level in AI response.");

    var requiredKeys = new[] { "ANX", "DEP", "STR", "SLP", "SOC", "CDT", "SAFE", "ENG" };
    if (requiredKeys.Any(k => !response.new_scores.ContainsKey(k)))
        throw new InvalidOperationException("AI response missing score parameters.");

    if (requiredKeys.Any(k => !response.deltas.ContainsKey(k)))
        throw new InvalidOperationException("AI response missing delta parameters.");
}
```

---

**Implementation: SummarizeChatAsync()**

```csharp
public async Task<string> SummarizeChatAsync(
    List<ChatMessage> messages,
    string? previousSummary,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Build message items
        var messageItems = messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatHistoryItem(m.Role, m.Content))
            .ToList();

        // Build request payload
        var payload = new ChatSummarizeRequest(messageItems, previousSummary);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        logger.LogInformation("Summarize API request for {MessageCount} messages", messageItems.Count);

        // Send request
        using var request = new HttpRequestMessage(HttpMethod.Post, "/summarize")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var summarizeResponse = JsonSerializer.Deserialize<ChatSummarizeResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Summarize API returned null.");

        return summarizeResponse.summary ?? "Unable to generate summary.";
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat summarization failed");
        return "Summarization temporarily unavailable.";
    }
}
```

---

## PHASE 10: API CONTROLLERS

**File:** `api/Controllers/ChatsController.cs`

**Route:** `[Route("api/[controller]")]` → `/api/chats`  
**Attributes:** `[ApiController]`, `[Authorize]`  
**Injected:** `IChatService chatService`, `IUserService userService`, `ApplicationDbContext db`

**Endpoints:**

### 1. POST /api/chats - Create Chat
```
Request: Empty body
Response: 201 Created
{
  "chatId": 1,
  "message": "Welcome! How can I help you today?",
  "currentScores": { ANX: 0, DEP: 0, ... },
  "deltas": { all: 0 },
  "riskLevel": "normal",
  "tags": [],
  "timestamp": "2026-05-11T..."
}
```

**Implementation:**
- Extract userId from JWT token
- Verify user exists
- Call chatService.CreateChatAsync(userId, todayMood: null)
- Return 201 with ChatResponse

---

### 2. POST /api/chats/{chatId}/messages - Send Message
```
Request:
{
  "message": "I'm feeling anxious"
}

Response: 200 OK
{
  "chatId": 1,
  "message": "AI response here...",
  "currentScores": { ANX: 5, DEP: 0, ... },
  "deltas": { ANX: 2, ... },
  "riskLevel": "normal",
  "tags": ["work_anxiety"],
  "timestamp": "2026-05-11T..."
}
```

**Implementation:**
- Extract userId from JWT
- Verify user owns chat
- Call chatService.SendMessageAsync(userId, chatId, message)
- Return 200 with ChatResponse
- Handle: 400 (empty message), 404 (chat not found), 503 (AI error)

---

### 3. GET /api/chats/{chatId} - Get Chat Details
```
Response: 200 OK
{
  "id": 1,
  "createdAt": "...",
  "endedAt": null,
  "isEnded": false,
  "riskLevel": "normal",
  "summary": null,
  "todayMood": 3,
  "messages": [
    { "id": 1, "role": "user", "content": "...", "createdAt": "..." },
    { "id": 2, "role": "assistant", "content": "...", "createdAt": "..." }
  ],
  "currentScores": { ANX: 5, ... },
  "allTags": ["work_anxiety", "sleep_problems"]
}
```

**Implementation:**
- Extract userId from JWT
- Call chatService.GetChatByIdAsync(chatId, userId)
- Return 200 or 404

---

### 4. GET /api/chats - Get Chat History
```
Query Parameters: pageNumber=1&pageSize=10

Response: 200 OK
[
  {
    "id": 1,
    "createdAt": "...",
    "endedAt": null,
    "isEnded": false,
    "messageCount": 12,
    "riskLevel": "elevated",
    "tags": ["anxiety"],
    "summary": null
  }
]
```

**Implementation:**
- Extract userId from JWT
- Validate pagination (pageSize 1-50)
- Call chatService.GetUserChatsAsync(userId, pageNumber, pageSize)
- Return 200

---

### 5. POST /api/chats/{chatId}/end - End Chat
```
Response: 200 OK
{
  "message": "Chat ended. Will be summarized shortly."
}
```

**Implementation:**
- Extract userId from JWT
- Verify ownership
- Call chatService.EndChatAsync(chatId, userId)
- Fire-and-forget background job for summarization
- Return 200 or 404

---

## PHASE 11: DEPENDENCY INJECTION

**File:** `api/DependencyInjection.cs`

**In AddApplicationServices() method, add:**
```csharp
services.AddScoped<IChatService, ChatService>();
```

**Location:** After `IExerciseService` registration, before JWT provider

---

## PHASE 12: BACKGROUND JOBS

### 12.1 Create Cleanup Job

**File:** `api/Infrastructure/BackgroundJobs/InactiveChatCleanupJob.cs`

```csharp
public class InactiveChatCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InactiveChatCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                
                // Auto-end chats inactive for 30 minutes
                var endedCount = await chatService.EndInactiveChatsAsync(30, stoppingToken);
                if (endedCount > 0)
                    logger.LogInformation("Ended {Count} inactive chats", endedCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in inactive chat cleanup job");
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 12.2 Register in Program.cs

**File:** `api/Program.cs`

**Add after existing hosted services:**
```csharp
builder.Services.AddHostedService<api.Infrastructure.BackgroundJobs.InactiveChatCleanupJob>();
```

---

## PHASE 13: DATABASE MIGRATION

### Create Migration:
```bash
dotnet ef migrations add AddChatModule --project api --startup-project api
```

### Apply Migration:
```bash
dotnet ef database update --project api --startup-project api
```

---

## PHASE 14: VALIDATORS (OPTIONAL BUT RECOMMENDED)

**File:** `api/Validators/ChatValidators.cs`

```csharp
public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty")
            .MaximumLength(5000).WithMessage("Message must be 5000 characters or less");
    }
}
```

---

## PHASE 15: ERROR HANDLING & RESPONSES

**Standard Error Responses:**

1. **401 Unauthorized** - Invalid/missing JWT
2. **403 Forbidden** - User doesn't own resource
3. **404 Not Found** - Chat/User not found
4. **400 Bad Request** - Invalid input (empty message, invalid pagination)
5. **503 Service Unavailable** - AI service down or timeout

---

## PHASE 16: LOGGING STRATEGY

**Log Points in ChatService:**
- Chat created: INFO
- Message saved: INFO
- Chat ended: INFO
- Crisis detected: WARNING
- Auto-ended inactive: INFO
- Summarization started: INFO
- Summarization failed: ERROR

**Log Points in RealAIService:**
- Request sent: INFORMATION (sanitized)
- Response received: INFORMATION (sanitized)
- Error: ERROR

---

## PHASE 17: SECURITY CONSIDERATIONS

1. **JWT Authorization:** All endpoints require token
2. **Ownership Verification:** Users can only access their own chats
3. **Input Validation:** Message length, pagination bounds
4. **Cascade Delete:** Chat deletion removes all messages and scores
5. **PII Protection:** Don't log message content in production

---

## PHASE 18: TESTING FILE

**File:** `api/chat-api.http`

```http
@baseUrl = https://localhost:5001/api
@token = <JWT_TOKEN_HERE>
@chatId = 1

### 1. Create Chat
POST {{baseUrl}}/chats
Authorization: Bearer {{token}}

### 2. Send Message
POST {{baseUrl}}/chats/{{chatId}}/messages
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "message": "I'm feeling anxious about work deadlines"
}

### 3. Get Chat Details
GET {{baseUrl}}/chats/{{chatId}}
Authorization: Bearer {{token}}

### 4. Get Chat History (Paginated)
GET {{baseUrl}}/chats?pageNumber=1&pageSize=10
Authorization: Bearer {{token}}

### 5. End Chat
POST {{baseUrl}}/chats/{{chatId}}/end
Authorization: Bearer {{token}}
```

---

## EXECUTION SEQUENCE FOR COPILOT AGENT

### STEP 1: Create Entities (4 files)
- [ ] Chat.cs
- [ ] ChatMessage.cs
- [ ] ChatScoreSnapshot.cs
- [ ] ChatScoreTag.cs

### STEP 2: Update User Entity (1 file)
- [ ] User.cs (add navigation)

### STEP 3: Create EF Configurations (4 files)
- [ ] ChatConfiguration.cs
- [ ] ChatMessageConfiguration.cs
- [ ] ChatScoreSnapshotConfiguration.cs
- [ ] ChatScoreTagConfiguration.cs

### STEP 4: Update DbContext (1 file)
- [ ] ApplicationDbContext.cs (add DbSets)

### STEP 5: Create DTOs (2 files)
- [ ] ChatContracts.cs
- [ ] ChatAIContracts.cs

### STEP 6: Create Service Interface (1 file)
- [ ] IChatService.cs

### STEP 7: Implement Service (1 file)
- [ ] ChatService.cs

### STEP 8: Extend AI Service (1 file)
- [ ] RealAIService.cs (add 2 methods)

### STEP 9: Update AI Service Interface (1 file)
- [ ] IAIService.cs (add 2 method signatures)

### STEP 10: Create Controller (1 file)
- [ ] ChatsController.cs

### STEP 11: Register DI (1 file)
- [ ] DependencyInjection.cs (add 1 line)

### STEP 12: Create Background Job (1 file)
- [ ] InactiveChatCleanupJob.cs

### STEP 13: Register Background Job (1 file)
- [ ] Program.cs (add 1 line)

### STEP 14: Create Migration
```bash
dotnet ef migrations add AddChatModule --project api --startup-project api
dotnet ef database update --project api --startup-project api
```

### STEP 15: Create Test File (1 file)
- [ ] chat-api.http

### OPTIONAL: Add Validators (1 file)
- [ ] ChatValidators.cs

---

## FINAL CHECKLIST

- [ ] All 18 new files created
- [ ] All 6 existing files updated
- [ ] Migration applies cleanly
- [ ] Application builds without errors
- [ ] All endpoints accessible in Swagger
- [ ] JWT authorization working
- [ ] User ownership verified
- [ ] Messages saved to database
- [ ] Scores tracked correctly
- [ ] Tags stored and retrieved
- [ ] Risk levels detected
- [ ] Pagination working (1-50)
- [ ] Background cleanup job running
- [ ] No breaking changes to Journal module
- [ ] All existing tests still pass

---

## SUMMARY

**Total New Files:** 18  
**Total Modified Files:** 6  
**Database Tables:** 4 new, 1 modified  
**New Endpoints:** 5  
**Breaking Changes:** 0  
**Estimated Implementation Time:** 4-6 hours (with Copilot)

**Key Differences from Journal Module:**
- ✅ Stateful conversation tracking (vs. one-time analysis)
- ✅ Multiple API calls per session (vs. single analysis)
- ✅ Background summarization (vs. immediate processing)
- ✅ Risk detection continuous (vs. per-entry)
- ✅ Score snapshots per message (vs. per journal entry)

---

**Status:** ✅ **READY FOR COPILOT AGENT EXECUTION**
