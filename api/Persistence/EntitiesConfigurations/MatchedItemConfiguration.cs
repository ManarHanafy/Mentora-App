using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class MatchedItemConfiguration : IEntityTypeConfiguration<MatchedItem>
{
    public void Configure(EntityTypeBuilder<MatchedItem> builder)
    {
        builder.HasKey(mi => mi.Id);
        builder.Property(mi => mi.Id).ValueGeneratedOnAdd();
        builder.Property(mi => mi.Parameter).IsRequired().HasMaxLength(10);
        builder.Property(mi => mi.Reason).IsRequired();
        builder.HasIndex(mi => mi.JournalEntryId);

        builder.HasOne(mi => mi.JournalEntry)
               .WithMany(j => j.MatchedItems)
               .HasForeignKey(mi => mi.JournalEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
