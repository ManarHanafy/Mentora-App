using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class ChatScoreSnapshotConfiguration : IEntityTypeConfiguration<ChatScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<ChatScoreSnapshot> builder)
    {
        builder.HasKey(css => css.Id);
        builder.Property(css => css.Id).ValueGeneratedOnAdd();

        builder.Property(css => css.Anx).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Dep).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Str).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Slp).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Soc).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Cdt).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Safe).IsRequired().HasDefaultValue(0);
        builder.Property(css => css.Eng).IsRequired().HasDefaultValue(0);

        builder.HasIndex(css => css.ChatId);

        builder.HasOne(css => css.Chat)
            .WithMany(c => c.ScoreSnapshots)
            .HasForeignKey(css => css.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
