using System.Text.Json;
using api.Contracts.AI;
using api.Contracts.Journals;
using api.Entities;
using api.Infrastructure.Caching;
using api.Persistence;
using api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Tests.Services;

public class JournalServiceTests
{
    [Fact]
    public async Task SubmitAsync_EnglishInput_PersistsAndReturnsExactAiPayload()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 1);

        var response = BuildResponse(["worry_loop"], "elevated", "I feel anxious.");
        var service = CreateJournalService(db, new StubAiService(response));

        var result = await service.SubmitAsync(1, new SubmitJournalRequest("I feel anxious."), CancellationToken.None);

        result.Should().BeEquivalentTo(ToJournalResponse(response));
        db.SuggestedExercises.Count().Should().Be(2);
        db.SuggestedExercises.Select(x => x.ExerciseCode).Should().ContainInOrder("EX_ANX_01", "EX_ANX_01");
        (await db.JournalScores.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SubmitAsync_ArabicInput_PersistsAndReturnsExactAiPayload()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 2);

        var response = BuildResponse(["low_mood"], "normal", "أنا متعب وحزين");
        var service = CreateJournalService(db, new StubAiService(response));

        var result = await service.SubmitAsync(2, new SubmitJournalRequest("أنا متعب وحزين"), CancellationToken.None);

        result.RiskLevel.Should().Be("normal");
        result.Tags.Should().ContainSingle().Which.Should().Be("low_mood");
        (await db.JournalEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MultipleJournalEntries_AreIndependent()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 3);

        var first = BuildResponse(["tag1"], "normal", "entry1");
        var second = BuildResponse(["tag2"], "elevated", "entry2");
        var ai = new SequenceAiService(first, second);
        var service = CreateJournalService(db, ai);

        await service.SubmitAsync(3, new SubmitJournalRequest("entry1"), CancellationToken.None);
        await service.SubmitAsync(3, new SubmitJournalRequest("entry2"), CancellationToken.None);

        var entries = await db.JournalEntries.OrderBy(e => e.Id).ToListAsync();
        entries.Should().HaveCount(2);
        entries[0].RiskLevel.Should().Be("normal");
        entries[1].RiskLevel.Should().Be("elevated");
        entries[0].AiResponseJson.Should().NotBe(entries[1].AiResponseJson);
    }

    [Fact]
    public async Task DeleteFlows_WorkForExerciseAndJournal()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 4);

        var response = BuildResponse(["tag1"], "normal", "entry");
        var journalService = CreateJournalService(db, new StubAiService(response));
        var exerciseService = new ExerciseService(db);
        var created = await journalService.SubmitAsync(4, new SubmitJournalRequest("entry"), CancellationToken.None);

        var journalId = await db.JournalEntries.Select(j => j.Id).SingleAsync();
        var deleteByJournalCount = await exerciseService.DeleteByJournalAsync(4, journalId, CancellationToken.None);
        deleteByJournalCount.Should().Be(2);

        await journalService.DeleteAsync(journalId, CancellationToken.None);

        (await db.JournalEntries.CountAsync()).Should().Be(0);
        (await db.SuggestedExercises.CountAsync()).Should().Be(0);
        (await db.JournalScores.CountAsync()).Should().Be(0);
        (await db.MatchedItems.CountAsync()).Should().Be(0);
        (await db.JournalTags.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_NullAiCollections_DoesNotThrowAndReturnsSafeDefaults()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 5);

        var response = new MentoraAnalyzeResponse(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var service = CreateJournalService(db, new StubAiService(response));

        Func<Task> act = () => service.SubmitAsync(5, new SubmitJournalRequest("test entry"), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SubmitAsync_MissingSnapshot_RecreatesSnapshotAndSucceeds()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 7,
            Username = "user7",
            Email = "user7@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });
        await db.SaveChangesAsync();

        var response = new MentoraAnalyzeResponse(
            [],
            new Dictionary<string, int>
            {
                ["ANX"] = 0, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            },
            new Dictionary<string, int>
            {
                ["ANX"] = 0, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            },
            ["tag1"],
            "normal",
            []);
        var service = CreateJournalService(db, new StubAiService(response));

        var result = await service.SubmitAsync(7, new SubmitJournalRequest("entry"), CancellationToken.None);

        result.RiskLevel.Should().Be("normal");
        var snapshot = await db.UserParameterSnapshots.FirstOrDefaultAsync(s => s.UserId == 7);
        snapshot.Should().NotBeNull();
        snapshot!.ToParametersDictionary().Values.Should().OnlyContain(v => v == 0);
        snapshot.LatestJournalEntryId.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenStoredJsonIsMissing()
    {
        await using var db = CreateDbContext();
        SeedUserWithSnapshot(db, 6);

        var entry = new JournalEntry
        {
            UserId = 6,
            JournalText = "fallback",
            RiskLevel = "elevated",
            AiResponseJson = string.Empty
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();

        db.JournalTags.Add(new JournalTag { JournalEntryId = entry.Id, Tag = "low_mood" });
        db.JournalScores.Add(new JournalScore { JournalEntryId = entry.Id, Anx = 4, Dep = 0, Str = 0, Slp = 0, Soc = 0, Cdt = 0, Safe = 0, Eng = 0 });
        db.MatchedItems.Add(new MatchedItem
        {
            JournalEntryId = entry.Id,
            Parameter = "ANX",
            Reason = "anxiety signals"
        });
        await db.SaveChangesAsync();
        var matched = await db.MatchedItems.SingleAsync(mi => mi.JournalEntryId == entry.Id);
        db.MatchedItemDetails.Add(new MatchedItemDetail
        {
            MatchedItemId = matched.Id,
            ItemId = "ANX1",
            Intensity = 2,
            MatchText = "anxious"
        });
        db.SuggestedExercises.Add(new SuggestedExercise
        {
            JournalEntryId = entry.Id,
            ExerciseCode = "EX_ANX_01",
            Parameter = "ANX",
            Score = 4,
            ScoreRange = "1-5"
        });
        await db.SaveChangesAsync();

        var service = CreateJournalService(db, new StubAiService(BuildResponse(["x"], "normal", "x")));
        Func<Task> act = async () => await service.GetByIdAsync(entry.Id, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static JournalResponse ToJournalResponse(MentoraAnalyzeResponse response) =>
        new(
            response.MatchedItems.Select(g => new api.Contracts.AI.MatchedItemResponse(
                g.Parameter,
                g.Items.Select(i => new api.Contracts.AI.MatchedItemEntryResponse(i.Id, i.Intensity03, i.MatchText)).ToList(),
                g.Reason)).ToList(),
            response.Deltas,
            response.NewScores,
            response.Tags,
            response.RiskLevel,
            response.SuggestedExercises.Select(e => new api.Contracts.Exercises.SuggestedExerciseResponse(e.Id, e.Parameter, e.Score, e.ScoreRange)).ToList());

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedUserWithSnapshot(ApplicationDbContext db, int userId)
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
        db.UserParameterSnapshots.Add(new UserParameterSnapshot
        {
            UserId = userId,
            Anx = 0,
            Dep = 0,
            Str = 0,
            Slp = 0,
            Soc = 0,
            Cdt = 0,
            Safe = 0,
            Eng = 0
        });
        db.SaveChanges();
    }

    private static MentoraAnalyzeResponse BuildResponse(List<string> tags, string risk, string text) =>
        new(
            [
                new MentoraMatchedGroup(
                    "ANX",
                    [
                        new MentoraMatchedItem("ANX1", 2, text)
                    ],
                    "anxiety signals")
            ],
            new Dictionary<string, int>
            {
                ["ANX"] = 2, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            },
            new Dictionary<string, int>
            {
                ["ANX"] = 2, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
                ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
            },
            tags,
            risk,
            [
                new MentoraSuggestedExercise("EX_ANX_01", "ANX", 2, "1-5"),
                new MentoraSuggestedExercise("EX_ANX_01", "ANX", 2, "1-5")
            ]);

    private static Dictionary<string, int> ValidCurrentScores() =>
        new()
        {
            ["ANX"] = 0, ["DEP"] = 0, ["STR"] = 0, ["SLP"] = 0,
            ["SOC"] = 0, ["CDT"] = 0, ["SAFE"] = 0, ["ENG"] = 0
        };

    private sealed class StubAiService(MentoraAnalyzeResponse response) : IAIService
    {
        public Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default)
            => Task.FromResult(new AIServiceResult(JsonSerializer.Serialize(response), response));

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
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SummarizeChatAsync(
            List<ChatMessage> messages,
            string? previousSummary,
            ChatSummarizeUserProfile userProfile,
            Dictionary<string, int> finalScores,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class SequenceAiService(params MentoraAnalyzeResponse[] responses) : IAIService
    {
        private int _index;

        public Task<AIServiceResult> AnalyseAsync(string journalText, Dictionary<string, int> currentScores, CancellationToken cancellationToken = default)
        {
            var response = responses[Math.Min(_index++, responses.Length - 1)];
            return Task.FromResult(new AIServiceResult(JsonSerializer.Serialize(response), response));
        }

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
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SummarizeChatAsync(
            List<ChatMessage> messages,
            string? previousSummary,
            ChatSummarizeUserProfile userProfile,
            Dictionary<string, int> finalScores,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
