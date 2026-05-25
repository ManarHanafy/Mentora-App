using api.Contracts.AI;
using api.Entities;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Tests.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task CreateChatAsync_CreatesChatAndInitialSnapshot()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 1);
        var service = new ChatService(db, new StubAiService(), NullLogger<ChatService>.Instance);

        var result = await service.CreateChatAsync(1, CancellationToken.None);

        result.Should().BeGreaterThan(0);
        (await db.Chats.CountAsync()).Should().Be(1);
        (await db.ChatScoreSnapshots.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SendMessageAsync_PersistsMessagesScoresAndTags()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 2);
        var service = new ChatService(db, new StubAiService(), NullLogger<ChatService>.Instance);
        var created = await service.CreateChatAsync(2, CancellationToken.None);

        var response = await service.SendMessageAsync(2, created, "I feel anxious today", CancellationToken.None);

        response.RiskLevel.Should().Be("elevated");
        response.CurrentScores["ANX"].Should().Be(3);
        (await db.ChatMessages.CountAsync()).Should().Be(2);
        (await db.ChatScoreSnapshots.CountAsync()).Should().Be(2);
        (await db.ChatScoreTags.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task EndInactiveChatsAsync_EndsOnlyInactiveChats()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 3);

        db.Chats.AddRange(
            new Chat { UserId = 3, LastActivityAt = DateTime.UtcNow.AddMinutes(-50), IsEnded = false, RiskLevel = "normal" },
            new Chat { UserId = 3, LastActivityAt = DateTime.UtcNow, IsEnded = false, RiskLevel = "normal" });
        await db.SaveChangesAsync();

        var service = new ChatService(db, new StubAiService(), NullLogger<ChatService>.Instance);
        var ended = await service.EndInactiveChatsAsync(30, CancellationToken.None);

        ended.Should().Be(1);
        (await db.Chats.CountAsync(c => c.IsEnded)).Should().Be(1);
    }

    [Fact]
    public async Task SummarizeChatAsync_SavesSummary()
    {
        await using var db = CreateDbContext();
        SeedUser(db, 4);
        var chat = new Chat { UserId = 4, LastActivityAt = DateTime.UtcNow, IsEnded = true, RiskLevel = "normal" };
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        db.ChatMessages.Add(new ChatMessage { ChatId = chat.Id, Role = "user", Content = "help me" });
        await db.SaveChangesAsync();

        var service = new ChatService(db, new StubAiService(), NullLogger<ChatService>.Instance);
        var success = await service.SummarizeChatAsync(chat.Id, CancellationToken.None);

        success.Should().BeTrue();
        var stored = await db.Chats.FindAsync(chat.Id);
        stored!.Summary.Should().Be("summary text");
        stored.UserMemory.Should().Be("summary text");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedUser(ApplicationDbContext db, int userId)
    {
        db.Users.Add(new User
        {
            Id = userId,
            Username = $"user{userId}",
            Email = $"user{userId}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });
        db.SaveChanges();
    }

    private sealed class StubAiService : IAIService
    {
        public Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ChatAIResult> ChatAsync(
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
            var newScores = new Dictionary<string, int>
            {
                ["ANX"] = 3, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            };
            var deltas = new Dictionary<string, int>
            {
                ["ANX"] = 3, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            };

            return Task.FromResult(new ChatAIResult(
                "mock response",
                newScores,
                deltas,
                "elevated",
                ["work_anxiety", "stress"],
                null));
        }

        public Task<string> SummarizeChatAsync(
            List<ChatMessage> messages,
            string? previousSummary,
            ChatSummarizeUserProfile userProfile,
            Dictionary<string, int> finalScores,
            CancellationToken cancellationToken = default)
            => Task.FromResult("summary text");
    }
}
