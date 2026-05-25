using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class OnboardingQuestionConfiguration : IEntityTypeConfiguration<OnboardingQuestion>
{
    public void Configure(EntityTypeBuilder<OnboardingQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedOnAdd();
        builder.Property(q => q.QuestionId).IsRequired();
        builder.Property(q => q.Locale).IsRequired().HasMaxLength(10);
        builder.Property(q => q.Category).IsRequired().HasMaxLength(200);
        builder.Property(q => q.Parameter).IsRequired().HasMaxLength(50);
        builder.Property(q => q.QuestionText).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(q => q.InputControlType).IsRequired().HasMaxLength(50);
        builder.Property(q => q.ScoringNote).HasMaxLength(500);
        builder.Property(q => q.PreQuestionDisclaimer).HasMaxLength(500);
        builder.Property(q => q.ConditionalActionsJson).HasColumnType("nvarchar(max)");
        builder.Property(q => q.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(q => q.DisplayOrder).IsRequired();

        builder.HasIndex(q => new { q.QuestionId, q.Locale }).IsUnique();
        builder.HasIndex(q => new { q.Locale, q.IsActive });

        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.OnboardingQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
