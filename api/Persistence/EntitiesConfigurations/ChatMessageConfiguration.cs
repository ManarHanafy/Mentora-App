using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(cm => cm.Id);
        builder.Property(cm => cm.Id).ValueGeneratedOnAdd();
        builder.Property(cm => cm.Role).IsRequired().HasMaxLength(50);
        builder.Property(cm => cm.Content).IsRequired().HasColumnType("nvarchar(max)");

        builder.ToTable(t => t.HasCheckConstraint("CK_ChatMessages_Role", "[Role] IN ('user', 'assistant')"));

        builder.HasIndex(cm => new { cm.ChatId, cm.CreatedAt });

        builder.HasOne(cm => cm.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(cm => cm.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
