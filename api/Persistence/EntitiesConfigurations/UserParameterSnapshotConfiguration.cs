using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserParameterSnapshotConfiguration : IEntityTypeConfiguration<UserParameterSnapshot>
{
    public void Configure(EntityTypeBuilder<UserParameterSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasOne(s => s.LatestJournalEntry)
               .WithMany()
               .HasForeignKey(s => s.LatestJournalEntryId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
