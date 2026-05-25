using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserOnboardingStateConfiguration : IEntityTypeConfiguration<UserOnboardingState>
{
    public void Configure(EntityTypeBuilder<UserOnboardingState> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.IsCompleted).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.RawResponsesJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(s => s.UserId).IsUnique();

        builder.HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<UserOnboardingState>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Responses)
            .WithOne(r => r.State)
            .HasForeignKey(r => r.UserOnboardingStateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Result)
            .WithOne(r => r.State)
            .HasForeignKey<UserOnboardingResult>(r => r.UserOnboardingStateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
