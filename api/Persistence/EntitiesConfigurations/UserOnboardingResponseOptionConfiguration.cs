using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserOnboardingResponseOptionConfiguration : IEntityTypeConfiguration<UserOnboardingResponseOption>
{
    public void Configure(EntityTypeBuilder<UserOnboardingResponseOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.OptionId).IsRequired();
        builder.Property(o => o.OptionTextSnapshot).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(o => o.MetricModifiersSnapshotJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(o => new { o.UserOnboardingResponseId, o.OnboardingQuestionOptionId }).IsUnique();

        builder.HasOne(o => o.QuestionOption)
            .WithMany()
            .HasForeignKey(o => o.OnboardingQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
