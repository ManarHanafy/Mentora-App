using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class JournalTagConfiguration : IEntityTypeConfiguration<JournalTag>
{
    public void Configure(EntityTypeBuilder<JournalTag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Tag).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.JournalEntryId);

        builder.HasOne(x => x.JournalEntry)
            .WithMany(j => j.JournalTags)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
