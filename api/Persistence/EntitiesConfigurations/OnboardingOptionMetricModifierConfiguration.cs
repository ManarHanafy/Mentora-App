using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class OnboardingOptionMetricModifierConfiguration : IEntityTypeConfiguration<OnboardingOptionMetricModifier>
{
    public void Configure(EntityTypeBuilder<OnboardingOptionMetricModifier> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Parameter).IsRequired().HasMaxLength(50);
        builder.Property(m => m.ModifierValue);
        builder.Property(m => m.ModifierValueText).HasMaxLength(100);

        builder.HasIndex(m => new { m.OnboardingQuestionOptionId, m.Parameter }).IsUnique();
    }
}
