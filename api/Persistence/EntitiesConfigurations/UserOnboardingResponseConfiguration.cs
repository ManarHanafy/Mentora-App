using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserOnboardingResponseConfiguration : IEntityTypeConfiguration<UserOnboardingResponse>
{
    public void Configure(EntityTypeBuilder<UserOnboardingResponse> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.QuestionId).IsRequired();
        builder.Property(r => r.LocaleSnapshot).IsRequired().HasMaxLength(10);
        builder.Property(r => r.CategorySnapshot).IsRequired().HasMaxLength(200);
        builder.Property(r => r.ParameterSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(r => r.QuestionTextSnapshot).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(r => r.InputControlTypeSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(r => r.ScoringNoteSnapshot).HasMaxLength(500);
        builder.Property(r => r.PreQuestionDisclaimerSnapshot).HasMaxLength(500);
        builder.Property(r => r.ConditionalActionsSnapshotJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(r => new { r.UserOnboardingStateId, r.OnboardingQuestionId }).IsUnique();
        builder.HasIndex(r => r.UserId);

        builder.HasOne(r => r.Question)
            .WithMany()
            .HasForeignKey(r => r.OnboardingQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.SelectedOptions)
            .WithOne(o => o.Response)
            .HasForeignKey(o => o.UserOnboardingResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
