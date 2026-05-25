using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class ChatScoreTagConfiguration : IEntityTypeConfiguration<ChatScoreTag>
{
    public void Configure(EntityTypeBuilder<ChatScoreTag> builder)
    {
        builder.HasKey(cst => cst.Id);
        builder.Property(cst => cst.Id).ValueGeneratedOnAdd();
        builder.Property(cst => cst.Tag).IsRequired().HasMaxLength(100);

        builder.HasIndex(cst => cst.ChatId);

        builder.HasOne(cst => cst.Chat)
            .WithMany(c => c.Tags)
            .HasForeignKey(cst => cst.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
