using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class MatchedItemDetailConfiguration : IEntityTypeConfiguration<MatchedItemDetail>
{
    public void Configure(EntityTypeBuilder<MatchedItemDetail> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ItemId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MatchText).IsRequired();
        builder.Property(x => x.MatchedItemId).IsRequired();
        builder.HasIndex(x => x.MatchedItemId);

        builder.HasOne(x => x.MatchedItem)
            .WithMany(mi => mi.Details)
            .HasForeignKey(x => x.MatchedItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
