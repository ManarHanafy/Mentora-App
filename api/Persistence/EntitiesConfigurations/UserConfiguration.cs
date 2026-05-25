using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();
        builder.Property(u => u.Username).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.EmailVerified).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.EmailVerifiedAt);
        builder.Property(u => u.EmailOtpHash).HasMaxLength(512);
        builder.Property(u => u.EmailOtpExpiresAt);
        builder.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.LastLogin);
        builder.Property(u => u.PasswordChangedAt);
        builder.Property(u => u.FailedLoginCount).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LockoutUntil);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasOne(u => u.ParameterSnapshot)
               .WithOne(s => s.User)
               .HasForeignKey<UserParameterSnapshot>(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(u => u.RefreshTokens, rt =>
        {
            rt.WithOwner().HasForeignKey("UserId");
            rt.ToTable("User_RefreshTokens");
            rt.Property(r => r.Token).IsRequired().HasMaxLength(512);
            rt.Property(r => r.ExpiresOn).IsRequired();
            rt.Property(r => r.CreatedOn).IsRequired();
            rt.Property(r => r.RevokedOn);
        });
    }
}
