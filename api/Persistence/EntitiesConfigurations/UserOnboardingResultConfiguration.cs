using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserOnboardingResultConfiguration : IEntityTypeConfiguration<UserOnboardingResult>
{
    public void Configure(EntityTypeBuilder<UserOnboardingResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.CompletedAt).IsRequired();

        builder.HasIndex(r => r.UserId).IsUnique();

        builder.HasOne(r => r.State)
            .WithOne(s => s.Result)
            .HasForeignKey<UserOnboardingResult>(r => r.UserOnboardingStateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
