using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class SuggestedExerciseConfiguration : IEntityTypeConfiguration<SuggestedExercise>
{
    public void Configure(EntityTypeBuilder<SuggestedExercise> builder)
    {
        builder.HasKey(se => se.Id);
        builder.Property(se => se.Id).ValueGeneratedOnAdd();
        builder.Property(se => se.UserId).IsRequired();
        builder.Property(se => se.JournalEntryId).IsRequired(false);
        builder.Property(se => se.ExerciseCode).IsRequired().HasMaxLength(50);
        builder.Property(se => se.Parameter).IsRequired().HasMaxLength(10);
        builder.Property(se => se.ScoreRange).IsRequired().HasMaxLength(20);
        builder.HasIndex(se => new { se.UserId, se.ExerciseCode }).IsUnique();
        builder.HasIndex(se => se.JournalEntryId);

        builder.HasOne(se => se.User)
               .WithMany()
               .HasForeignKey(se => se.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(se => se.JournalEntry)
               .WithMany(j => j.SuggestedExercises)
               .HasForeignKey(se => se.JournalEntryId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
