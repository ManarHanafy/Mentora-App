using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class OnboardingQuestionOptionConfiguration : IEntityTypeConfiguration<OnboardingQuestionOption>
{
    public void Configure(EntityTypeBuilder<OnboardingQuestionOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.OptionId).IsRequired();
        builder.Property(o => o.OptionText).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(o => o.ScorePoints);
        builder.Property(o => o.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(o => o.DisplayOrder).IsRequired();

        builder.HasIndex(o => new { o.OnboardingQuestionId, o.OptionId }).IsUnique();
        builder.HasIndex(o => o.IsActive);

        builder.HasMany(o => o.MetricModifiers)
            .WithOne(m => m.Option)
            .HasForeignKey(m => m.OnboardingQuestionOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
