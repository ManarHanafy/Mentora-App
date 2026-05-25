using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class JournalScoreConfiguration : IEntityTypeConfiguration<JournalScore>
{
    public void Configure(EntityTypeBuilder<JournalScore> builder)
    {
        builder.ToTable("Scores");
        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).ValueGeneratedOnAdd();
        builder.Property(ps => ps.JournalEntryId).IsRequired();
        builder.HasIndex(ps => ps.JournalEntryId).IsUnique();
    }
}
