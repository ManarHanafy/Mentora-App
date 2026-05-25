using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedOnAdd();
        builder.Property(j => j.JournalText).IsRequired();
        builder.Property(j => j.AiResponseJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(j => j.RiskLevel).IsRequired().HasMaxLength(50);

        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => j.CreatedAt);

        builder.HasOne(j => j.User)
               .WithMany(u => u.JournalEntries)
               .HasForeignKey(j => j.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Score)
            .WithOne(s => s.JournalEntry)
            .HasForeignKey<JournalScore>(s => s.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
