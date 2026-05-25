using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
        builder.Property(c => c.RiskLevel).IsRequired().HasMaxLength(50).HasDefaultValue("normal");
        builder.Property(c => c.IsEnded).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.LastActivityAt).IsRequired();
        builder.Property(c => c.Summary).HasColumnType("nvarchar(max)");
        builder.Property(c => c.UserMemory).HasColumnType("nvarchar(max)");

        builder.ToTable(t => t.HasCheckConstraint("CK_Chats_RiskLevel", "[RiskLevel] IN ('normal', 'elevated', 'crisis')"));

        builder.HasIndex(c => new { c.UserId, c.CreatedAt });
        builder.HasIndex(c => c.IsEnded);

        builder.HasOne(c => c.User)
            .WithMany(u => u.Chats)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
