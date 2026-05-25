using System.Reflection;
using Microsoft.EntityFrameworkCore;
using api.Entities;

namespace api.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User>                  Users                  { get; set; } = null!;
    public DbSet<JournalEntry>          JournalEntries         { get; set; } = null!;
    public DbSet<JournalTag>            JournalTags            { get; set; } = null!;
    public DbSet<MatchedItem>           MatchedItems           { get; set; } = null!;
    public DbSet<MatchedItemDetail>     MatchedItemDetails     { get; set; } = null!;
    public DbSet<JournalScore>          JournalScores          { get; set; } = null!;
    public DbSet<Exercise>              Exercises              { get; set; } = null!;
    public DbSet<SuggestedExercise>     SuggestedExercises     { get; set; } = null!;
    public DbSet<UserParameterSnapshot> UserParameterSnapshots { get; set; } = null!;
    public DbSet<Chat>                  Chats                  { get; set; } = null!;
    public DbSet<ChatMessage>           ChatMessages           { get; set; } = null!;
    public DbSet<ChatScoreSnapshot>     ChatScoreSnapshots     { get; set; } = null!;
    public DbSet<ChatScoreTag>          ChatScoreTags          { get; set; } = null!;
    public DbSet<MoodEntry>             MoodEntries            { get; set; } = null!;
    public DbSet<PasswordResetToken>    PasswordResetTokens    { get; set; } = null!;
    public DbSet<OnboardingQuestion>    OnboardingQuestions    { get; set; } = null!;
    public DbSet<OnboardingQuestionOption> OnboardingQuestionOptions { get; set; } = null!;
    public DbSet<OnboardingOptionMetricModifier> OnboardingOptionMetricModifiers { get; set; } = null!;
    public DbSet<UserOnboardingState>   UserOnboardingStates   { get; set; } = null!;
    public DbSet<UserOnboardingResponse> UserOnboardingResponses { get; set; } = null!;
    public DbSet<UserOnboardingResponseOption> UserOnboardingResponseOptions { get; set; } = null!;
    public DbSet<UserOnboardingResult>  UserOnboardingResults  { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
